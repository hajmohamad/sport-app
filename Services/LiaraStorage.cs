using Amazon.S3;
using Amazon.S3.Model;
using sport_app_backend.Interface;
using sport_app_backend.Models;


namespace sport_app_backend.Services;

public class LiaraStorage(IConfiguration config) :ILiaraStorage
{
    // private readonly string _accessKey = config["Liara:accessKey"] ?? "string.Empty";
    // private readonly string _secretKey = config["Liara:secretKey"] ?? "string.Empty";
    // private readonly string _bucketName = config["Liara:BucketName"] ?? "string.Empty";
    // private readonly string _endpoint = config["Liara:endPoint"] ?? "string.Empty";
    public async Task<ApiResponse> UploadImage(IFormFile image, string url, string folderName)
    {
        var config = new AmazonS3Config
        {
            ServiceURL = "https://s3.ir-thr-at1.arvanstorage.ir",
            ForcePathStyle = true,
            SignatureVersion = "4",
            AuthenticationRegion = "ir-thr-at1"
        };

        var credentials = new Amazon.Runtime.BasicAWSCredentials(
            "cc698a28-40d0-4708-af66-d01cdcc9b5d4","0e6542fcee37bf003f85f1b3c606284e00029ddba283297d7fb65a50a93aebf1"
        );

        using var client = new AmazonS3Client(credentials, config);

        var extension = Path.GetExtension(image.FileName);

        folderName = folderName?.Trim().TrimEnd('/');

        var objectKey = string.IsNullOrEmpty(folderName)
            ? $"{Guid.NewGuid()}{extension}"
            : $"{folderName}/{Guid.NewGuid()}{extension}";

        try
        {
            using var memoryStream = new MemoryStream();
            await image.CopyToAsync(memoryStream);
            memoryStream.Position = 0;

            var request = new PutObjectRequest
            {
                BucketName = "chaarset",
                Key = objectKey,
                InputStream = memoryStream,
                ContentType = image.ContentType,
                CannedACL = S3CannedACL.PublicRead // اگر میخوای فایل public باشد
            };

            await client.PutObjectAsync(request);

            var fileUrl = $"https://{"chaarset"}.s3.ir-thr-at1.arvanstorage.ir/{objectKey}";

            if (!string.IsNullOrEmpty(url) && url.Length > 10)
            {
                await DeleteObjectAsync(client, url);
            }

            return new ApiResponse
            {
                Action = true,
                Message = "Image uploaded successfully",
                Result = fileUrl
            };
        }
        catch (AmazonS3Exception e)
        {
            return new ApiResponse
            {
                Action = false,
                Message = $"Error uploading to Arvan S3: {e.Message}"
            };
        }
    }

    public async Task<ApiResponse> RemovePhoto(string url)
    {
   
        var config = new AmazonS3Config
        {
            ServiceURL = "https://s3.ir-thr-at1.arvanstorage.ir",
            ForcePathStyle = true,
            SignatureVersion = "4",
            AuthenticationRegion = "ir-thr-at1"
        };

        var credentials = new Amazon.Runtime.BasicAWSCredentials(
            "cc698a28-40d0-4708-af66-d01cdcc9b5d4","0e6542fcee37bf003f85f1b3c606284e00029ddba283297d7fb65a50a93aebf1"
        );

        using var client = new AmazonS3Client(credentials, config);

        
        try
        { 
            if (url.Length > 10)
            {
                await DeleteObjectAsync(client, url);
                return new ApiResponse()
                {
                    Action = true,
                    Message = "img remove successfully",
                
                };
            }

        }
        catch (AmazonS3Exception e)
        {

            return new ApiResponse()
            {
                Action = false,
                Message = $"Error uploading to S3: {e.Message}",
                
            };
        }
        return new ApiResponse()
        {
            Action = true,
            Message = "img remove successfully",
                
        };
    }
    private static async Task DeleteObjectAsync(IAmazonS3 client, string url)
    {
        var uri = new Uri(url);

        var hostParts = uri.Host.Split('.');
        var bucketName = hostParts[0];

        var objectKey = uri.AbsolutePath.TrimStart('/');

        try
        {
            var deleteRequest = new DeleteObjectRequest
            {
                BucketName = bucketName,
                Key = objectKey
            };

            await client.DeleteObjectAsync(deleteRequest);
            Console.WriteLine($"Deleted: {bucketName}/{objectKey}");
        }
        catch (AmazonS3Exception e)
        {
            Console.WriteLine($"Error deleting from S3: {e.Message}");
        }
    }
}