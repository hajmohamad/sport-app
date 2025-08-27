using Amazon.S3;
using Amazon.S3.Model;
using sport_app_backend.Interface;
using sport_app_backend.Models;


namespace sport_app_backend.Services;

public class LiaraStorage(IConfiguration config) :ILiaraStorage
{
    private readonly string _accessKey = config["Liara:accessKey"] ?? "string.Empty";
    private readonly string _secretKey = config["Liara:secretKey"] ?? "string.Empty";
    private readonly string _bucketName = config["Liara:BucketName"] ?? "string.Empty";
    private readonly string _endpoint = config["Liara:endPoint"] ?? "string.Empty";
    //
    // private readonly CascadeClassifier _faceCascade=new CascadeClassifier("haarcascade_frontalface_default.xml");
    // private readonly CascadeClassifier _profileFaceCascade =  new CascadeClassifier("haarcascade_profileface.xml");
    
    
    
    
    
    
    //  public byte[] BlurFacesInImage(byte[] imageData)
    // {
    //      using var image = Mat.FromImageData(imageData, ImreadModes.Color);
    //     if (image.Empty())
    //     {
    //         throw new Exception("Could not read image data.");
    //     }
    //
    //
    //     var frontalFaces = _faceCascade.DetectMultiScale(
    //         image,
    //         scaleFactor: 1.1,
    //         minNeighbors: 5,
    //         minSize: new Size(30, 30));
    //
    //     var profileFaces = _profileFaceCascade.DetectMultiScale(
    //         image,
    //         scaleFactor: 1.1,
    //         minNeighbors: 4,
    //         minSize: new Size(15, 15));
    //
    //
    //     var allFaces = profileFaces.Concat(frontalFaces).ToArray();
    //
    //    
    //     foreach (var faceRect in allFaces)
    //     {
    //         var faceRoi = new Mat(image, faceRect);
    //         Cv2.GaussianBlur(faceRoi, faceRoi, new Size(99, 99), 0);
    //     }
    //
    //     Cv2.ImEncode(".png", image, out var processedImage);
    //     return processedImage;
    // }
    // public async Task<ApiResponse> UploadBlurFaceImage(IFormFile image, string url)
    // {
    //     var config = new AmazonS3Config
    //     {
    //         ServiceURL = _endpoint,
    //         ForcePathStyle = true,
    //         SignatureVersion = "4"
    //     };
    //
    //     var credentials = new Amazon.Runtime.BasicAWSCredentials(
    //         _accessKey,
    //         _secretKey
    //     );
    //
    //     using var client = new AmazonS3Client(credentials, config);
    //
    //     var extension = Path.GetExtension(image.FileName);
    //     var objectKey = $"{Guid.NewGuid()}{extension}";
    //
    //     try
    //     {
    //         byte[] originalImageBytes;
    //         using (var initialStream = new MemoryStream())
    //         {
    //             await image.CopyToAsync(initialStream);
    //             originalImageBytes = initialStream.ToArray();
    //         }
    //
    //        
    //         var processedImageBytes = BlurFacesInImage(originalImageBytes);
    //
    //         using var memoryStream = new MemoryStream(processedImageBytes);
    //         
    //
    //         var request = new PutObjectRequest
    //         {
    //             BucketName = _bucketName,
    //             Key = objectKey,
    //             InputStream = memoryStream,
    //             ContentType = image.ContentType // اضافه برای بهتر بودن متادیتا
    //         };
    //
    //         await client.PutObjectAsync(request);
    //
    //         var fileUrl = $"{_endpoint}/{_bucketName}/{objectKey}";
    //         if (url.Length > 10)
    //         {
    //             await DeleteObjectAsync(client, url);
    //         }
    //
    //         return new ApiResponse()
    //         {
    //             Action = true,
    //             Message = "imageUploaded successfully",
    //             Result = fileUrl 
    //         };
    //
    //
    //     }
    //     catch (AmazonS3Exception e)
    //     {
    //
    //         return new ApiResponse()
    //         {
    //             Action = false,
    //             Message = $"Error uploading to S3: {e.Message}",
    //             
    //         };
    //     }
    //
    // }


    public async Task<ApiResponse> UploadImage(IFormFile image, string url)
    {
        var config = new AmazonS3Config
        {
            ServiceURL = _endpoint,
            ForcePathStyle = true,
            SignatureVersion = "4"
        };

        var credentials = new Amazon.Runtime.BasicAWSCredentials(
            _accessKey,
            _secretKey
        );

        using var client = new AmazonS3Client(credentials, config);

        var extension = Path.GetExtension(image.FileName);
        var objectKey = $"{Guid.NewGuid()}{extension}";

        try
        {
            using var memoryStream = new MemoryStream();
            await image.CopyToAsync(memoryStream).ConfigureAwait(false);
            memoryStream.Position = 0;

            var request = new PutObjectRequest
            {
                BucketName = _bucketName,
                Key = objectKey,
                InputStream = memoryStream,
                ContentType = image.ContentType // اضافه برای بهتر بودن متادیتا
            };

            await client.PutObjectAsync(request);

            var fileUrl = $"{_endpoint}/{_bucketName}/{objectKey}";
            if (url.Length > 10)
            {
                await DeleteObjectAsync(client, url);
            }

            return new ApiResponse()
            {
                Action = true,
                Message = "imageUploaded successfully",
                Result = fileUrl 
            };


        }
        catch (AmazonS3Exception e)
        {

            return new ApiResponse()
            {
                Action = false,
                Message = $"Error uploading to S3: {e.Message}",
                
            };
        }

    }
    public async Task<ApiResponse> RemovePhoto(string url)
    {
   
        var config = new AmazonS3Config
        {
            ServiceURL = _endpoint,
            ForcePathStyle = true,
            SignatureVersion = "4"
        };

        var credentials = new Amazon.Runtime.BasicAWSCredentials(
            _accessKey,
            _secretKey
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
        var segments = uri.AbsolutePath.TrimStart('/').Split('/', 2);

        if (segments.Length < 2)
            throw new ArgumentException("URL does not contain a valid bucket and object key");
        var bucketName = segments[0];
        var objectKey = segments[1];

        try
        {
            var deleteRequest = new DeleteObjectRequest
            {
                BucketName = bucketName,
                Key = objectKey
            };

            await client.DeleteObjectAsync(deleteRequest);
            Console.WriteLine($"File '{objectKey}' deleted successfully.");
        }
        catch (AmazonS3Exception e)
        {
            Console.WriteLine($"Error: {e.Message}");
        }
    }
}