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

    public async Task<byte[]> DownloadFileAsync(string fileUrl)
    {
        try
        {
            using (var httpClient = new HttpClient())
            {
                Console.WriteLine($"Attempting to download file from: {fileUrl}");

                // Make a GET request to the file URL
                var response = await httpClient.GetAsync("https://storage.googleapis.com/"+fileUrl);
                response.EnsureSuccessStatusCode();

                // Read the file contents as a byte array
                var fileBytes = await response.Content.ReadAsByteArrayAsync();
                Console.WriteLine($"File downloaded successfully from: {fileUrl}");
                return fileBytes;
            }
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"HTTP error downloading file: {ex.Message}");
            throw;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"General error downloading file: {ex.Message}");
            throw;
        }
    }
    public async Task<byte[]> DownloadFileAsync_x(string fileUrl)
    {
        try
        {
            using (var httpClient = new HttpClient())
            {
                Console.WriteLine($"Attempting to download file from: {fileUrl}");

                // Make a GET request to the file URL
                var response = await httpClient.GetAsync("https://storage.googleapis.com/"+fileUrl);
                response.EnsureSuccessStatusCode();

                // Read the file contents as a byte array
                var fileBytes = await response.Content.ReadAsByteArrayAsync();
                Console.WriteLine($"File downloaded successfully from: {fileUrl}");
                return fileBytes;
            }
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"HTTP error downloading file: {ex.Message}");
            throw;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"General error downloading file: {ex.Message}");
            throw;
        }
    }

}

