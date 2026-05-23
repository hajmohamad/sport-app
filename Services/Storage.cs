using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.AspNetCore.Http;
using sport_app_backend.Interface;
using sport_app_backend.Models;
using System.Net;

namespace sport_app_backend.Services;

public class Storage : ILiaraStorage
{
    private readonly string _accessKey;
    private readonly string _secretKey;
    private readonly string _bucketName;
    private readonly string _endpoint;

    public Storage(IConfiguration config)
    {
        _accessKey = Environment.GetEnvironmentVariable("Arvan_ACCESS_KEY") ?? config["Arvan:AccessKey"] ?? "";
        _secretKey = Environment.GetEnvironmentVariable("Arvan_SECRET_KEY") ?? config["Arvan:SecretKey"] ?? "";
        _bucketName = Environment.GetEnvironmentVariable("Arvan_BUCKET") ?? config["Arvan:BucketName"] ?? "";
        _endpoint = Environment.GetEnvironmentVariable("Arvan_ENDPOINT") ?? config["Arvan:EndPoint"] ?? "";

        if (string.IsNullOrWhiteSpace(_accessKey) ||
            string.IsNullOrWhiteSpace(_secretKey) ||
            string.IsNullOrWhiteSpace(_bucketName) ||
            string.IsNullOrWhiteSpace(_endpoint))
        {
            throw new InvalidOperationException("Arvan S3 configuration is missing (AccessKey/SecretKey/BucketName/EndPoint).");
        }
    }

    private AmazonS3Client CreateClient()
    {
        var s3Config = new AmazonS3Config
        {
            ServiceURL = _endpoint,      // e.g. https://s3.ir-thr-at1.arvanstorage.ir
            ForcePathStyle = true,      
            SignatureVersion = "4",
            AuthenticationRegion = "ir-thr-at1"
        };

        var credentials = new BasicAWSCredentials(_accessKey, _secretKey);
        return new AmazonS3Client(credentials, s3Config);
    }

    public async Task<ApiResponse> UploadImage(IFormFile image, string url, string? folderName)
    {
        if (image == null || image.Length == 0)
        {
            return new ApiResponse { Action = false, Message = "Invalid image file" };
        }

        using var client = CreateClient();

        // فولدر
        folderName = NormalizeFolder(folderName);

        // پسوند را درست و تضمینی بساز
        var extension = NormalizeExtension(Path.GetExtension(image.FileName));
        if (string.IsNullOrEmpty(extension))
        {
            extension = GuessExtensionFromContentType(image.ContentType) ?? ".webp"; // fallback
        }

        var objectKey = BuildObjectKey(folderName, extension);

        try
        {
            using var memoryStream = new MemoryStream();
            await image.CopyToAsync(memoryStream);
            memoryStream.Position = 0;

            var contentType = !string.IsNullOrWhiteSpace(image.ContentType)
                ? image.ContentType
                : GuessContentTypeFromExtension(extension) ?? "application/octet-stream";

            var request = new PutObjectRequest
            {
                BucketName = _bucketName,
                Key = objectKey,                 // اینجا پسوند حتماً هست
                InputStream = memoryStream,
                ContentType = contentType,
                CannedACL = S3CannedACL.PublicRead
            };

            await client.PutObjectAsync(request);

            // URL صحیح و پایدار بساز (Path-style چون ForcePathStyle=true)
            var fileUrl = BuildPublicUrl(objectKey);

            // اگر url قبلی وجود داشت، حذفش کن (اگر نبود ارور نده)
            if (IsValidUrlForDelete(url))
                await DeleteObjectAsync(client, url);

            return new ApiResponse
            {
                Action = true,
                Message = "Image uploaded successfully",
                Result = fileUrl // همین را در DB ذخیره کنید (شامل پسوند)
            };
        }
        catch (AmazonS3Exception e)
        {
            return new ApiResponse
            {
                Action = false,
                Message = $"Error uploading to S3: {e.Message}"
            };
        }
        catch (Exception e)
        {
            return new ApiResponse
            {
                Action = false,
                Message = $"Unexpected error: {e.Message}"
            };
        }
    }

    public async Task<ApiResponse> RemovePhoto(string url)
    {
        using var client = CreateClient();

        if (!IsValidUrlForDelete(url))
        {
            return new ApiResponse { Action = false, Message = "Invalid image url" };
        }

        try
        {
            await DeleteObjectAsync(client, url);

            // حتی اگر فایل وجود نداشت هم این را success برگردان
            return new ApiResponse
            {
                Action = true,
                Message = "Image removed successfully"
            };
        }
        catch (AmazonS3Exception e)
        {
            return new ApiResponse
            {
                Action = false,
                Message = $"Error removing from S3: {e.Message}"
            };
        }
        catch (Exception e)
        {
            return new ApiResponse
            {
                Action = false,
                Message = $"Unexpected error: {e.Message}"
            };
        }
    }

    private async Task DeleteObjectAsync(IAmazonS3 client, string url)
    {
        var objectKey = ExtractObjectKeyFromUrl(url);

        if (string.IsNullOrWhiteSpace(objectKey))
            return;

        try
        {
            var deleteRequest = new DeleteObjectRequest
            {
                BucketName = _bucketName,
                Key = objectKey
            };

            await client.DeleteObjectAsync(deleteRequest);
        }
        catch (AmazonS3Exception e) when (
            e.StatusCode == HttpStatusCode.NotFound ||
            string.Equals(e.ErrorCode, "NoSuchKey", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(e.ErrorCode, "NotFound", StringComparison.OrdinalIgnoreCase))
        {
            // فایل وجود ندارد => مشکلی نیست
            return;
        }
    }

    // ---------------- Helpers ----------------

    private string NormalizeFolder(string? folderName)
    {
        folderName = folderName?.Trim();

        if (string.IsNullOrWhiteSpace(folderName))
            return "";

        folderName = folderName.Trim('/').Replace("\\", "/");
        return folderName;
    }

    private string BuildObjectKey(string folderName, string extension)
    {
        var fileName = $"{Guid.NewGuid():N}{extension}"; // پسوند تضمینی

        return string.IsNullOrEmpty(folderName)
            ? fileName
            : $"{folderName}/{fileName}";
    }

    private string BuildPublicUrl(string objectKey)
    {
        // Path-style URL: {endpoint}/{bucket}/{key}
        // با ForcePathStyle=true منطقی‌ترین و کم‌خطاترین حالت برای Arvan
        return $"{_endpoint.TrimEnd('/')}/{_bucketName}/{objectKey}";
    }

    private bool IsValidUrlForDelete(string? url)
        => !string.IsNullOrWhiteSpace(url) && url.Length > 10 && Uri.TryCreate(url, UriKind.Absolute, out _);

    private string ExtractObjectKeyFromUrl(string url)
    {
        var uri = new Uri(url);

        // AbsolutePath مثل: /bucket/folder/file.webp  یا /folder/file.webp
        var path = uri.AbsolutePath.TrimStart('/');

        // اگر path با bucket شروع شد، حذفش کن
        if (path.StartsWith(_bucketName + "/", StringComparison.OrdinalIgnoreCase))
            path = path.Substring(_bucketName.Length + 1);

        // اگر کاربر URL virtual-host داده باشد (bucket.endpoint/...) ممکن است path از اول key باشد
        return path;
    }

    private string NormalizeExtension(string ext)
    {
        if (string.IsNullOrWhiteSpace(ext))
            return "";

        ext = ext.Trim().ToLowerInvariant();
        if (!ext.StartsWith(".")) ext = "." + ext;
        return ext;
    }

    private string? GuessExtensionFromContentType(string? contentType)
    {
        contentType = contentType?.ToLowerInvariant();
        return contentType switch
        {
            "image/jpeg" => ".jpg",
            "image/jpg" => ".jpg",
            "image/png" => ".png",
            "image/webp" => ".webp",
            "image/gif" => ".gif",
            _ => null
        };
    }

    private string? GuessContentTypeFromExtension(string ext)
    {
        ext = NormalizeExtension(ext);
        return ext switch
        {
            ".jpg" => "image/jpeg",
            ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".webp" => "image/webp",
            ".gif" => "image/gif",
            _ => null
        };
    }
}
