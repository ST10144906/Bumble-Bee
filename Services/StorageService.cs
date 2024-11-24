using Google.Cloud.Storage.V1;
using Google.Apis.Auth.OAuth2;

public class StorageService
    {
        private readonly StorageClient _storageClient;

    public StorageService()
    {
        var googleCredential = GoogleCredential.FromFile("Secrets/bumble-bee-foundation-firebase-adminsdk.json");
        _storageClient = StorageClient.Create(googleCredential);
    }

    public async Task<string> UploadFileAsync(Stream fileStream, string objectName)
    {
        var bucketName = "bumble-bee-foundation.appspot.com";
        await _storageClient.UploadObjectAsync(bucketName, objectName, null, fileStream);
        Console.WriteLine($"Uploaded {objectName}.");

        string fileUrl = $"https://storage.googleapis.com/{bucketName}/{objectName}";
        return fileUrl;
    }

    public async Task<byte[]> DownloadFileAsync(string objectName)
 {
     var bucketName = "bumble-bee-foundation.appspot.com";

     using (var memoryStream = new MemoryStream())
     {
         await _storageClient.DownloadObjectAsync(bucketName, objectName, memoryStream);
         Console.WriteLine($"Downloaded {objectName}.");
         return memoryStream.ToArray();
     }
 }

}

