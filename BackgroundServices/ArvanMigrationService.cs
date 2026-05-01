using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.EntityFrameworkCore;
using sport_app_backend.Data;

namespace sport_app_backend.BackgroundServices;

public class ArvanMigrationService
{
    private readonly ApplicationDbContext _context;
    private readonly AmazonS3Client _client;
    private readonly string _bucketName = "chaarset";

    public ArvanMigrationService(ApplicationDbContext context, IConfiguration config)
    {
        _context = context;

        var s3Config = new AmazonS3Config
        {
            ServiceURL = "https://s3.ir-thr-at1.arvanstorage.ir",
            ForcePathStyle = true,
            SignatureVersion = "4",
            AuthenticationRegion = "ir-thr-at1"
        };

        var credentials = new BasicAWSCredentials(
            "cc698a28-40d0-4708-af66-d01cdcc9b5d4",
            "0e6542fcee37bf003f85f1b3c606284e00029ddba283297d7fb65a50a93aebf1"
        );

        _client = new AmazonS3Client(credentials, s3Config);
    }

    public async Task MigrateExercises()
    {
        var exercises = await _context.Exercises.ToListAsync();
        using var http = new HttpClient();

        foreach (var exercise in exercises)
        {
            try
            {
                bool changed = false;

                if (!string.IsNullOrWhiteSpace(exercise.VideoLink))
                {
                    var newUrl = await UploadFromUrl(http, exercise.VideoLink, "videos");

                    if (newUrl != null)
                    {
                        exercise.VideoLink = newUrl;
                        changed = true;
                    }
                }

                if (!string.IsNullOrWhiteSpace(exercise.ImageLink))
                {
                    var newUrl = await UploadFromUrl(http, exercise.ImageLink, "images");

                    if (newUrl != null)
                    {
                        exercise.ImageLink = newUrl;
                        changed = true;
                    }
                }

                if (changed)
                {
                    await _context.SaveChangesAsync(); // ✅ ذخیره همان لحظه
                }

                Console.WriteLine($"✔ Migrated Exercise {exercise.Id}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error migrating exercise {exercise.Id}: {ex.Message}");
            }
        }
    }

    private string NormalizeUrl(string url)
    {
        if (string.IsNullOrEmpty(url))
            return url;

        url = url.Replace(
            "https://storage.c2.liara.space/charsets/",
            "http://storage.chaarset.ir/"
        );
        url = url.Replace(
            "https://charsets.storage.c2.liara.space",
            "http://storage.chaarset.ir/"
        );
        url = url.Replace(
            "https://charsetproject.storage.c2.liara.space/",
            "http://prev.chaarset.ir/"
        );
        url = url.Replace(
            "https://storage.c2.liara.space/charsetproject/",
            "http://prev.chaarset.ir/"
        );

        return url;
    }

    private async Task<string> UploadFromUrl(HttpClient http, string oldUrl, string folder)
    {
        oldUrl = NormalizeUrl(oldUrl);
        oldUrl = Uri.EscapeUriString(oldUrl);

        var uri = new Uri(oldUrl);
        var fileName = Path.GetFileName(uri.LocalPath);
        var extension = Path.GetExtension(fileName);
        var objectKey = $"{folder}/{fileName}"; // ✅ همان اسم قبلی

        // --- گرفتن Content-Length با HEAD ---
        var headReq = new HttpRequestMessage(HttpMethod.Head, oldUrl);
        var headRes = await http.SendAsync(headReq);
        headRes.EnsureSuccessStatusCode();

        var length = headRes.Content.Headers.ContentLength ?? 0;
        var contentType = GetContentType(extension);

        // --- حالا دانلود ---
        using var response = await http.GetAsync(oldUrl, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync();

        var putRequest = new PutObjectRequest
        {
            BucketName = _bucketName,
            Key = objectKey,
            InputStream = stream,
            ContentType = contentType,
            CannedACL = S3CannedACL.PublicRead
        };

        // فقط اگر طول مشخص بود اضافه کن
        if (length > 0)
        {
            putRequest.Headers.ContentLength = length;
        }

        await _client.PutObjectAsync(putRequest);

        return $"https://{_bucketName}.s3.ir-thr-at1.arvanstorage.ir/{objectKey}";
    }

    private string GetContentType(string extension)
    {
        return extension.ToLower() switch
        {
            ".mp4" => "video/mp4",
            ".jpg" => "image/jpeg",
            ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".webp" => "image/webp",
            _ => "application/octet-stream"
        };
    }
}