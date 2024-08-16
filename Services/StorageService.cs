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

    public async Task UploadFileAsync(string localPath, string objectName)
    {
        using var fileStream = System.IO.File.OpenRead(localPath);
        await _storageClient.UploadObjectAsync("bumble-bee-foundation.appspot.com", objectName, null, fileStream);
        Console.WriteLine($"Uploaded {objectName}.");
    }

    //--- Additional methods for storage operations to be added later.
}

