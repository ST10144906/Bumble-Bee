using Google.Cloud.Firestore;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Firestore.V1;

public class FirestoreService
{
    private readonly FirestoreDb _firestoreDb;

    public FirestoreService()
    {
        var googleCredential = GoogleCredential.FromFile("Secrets/bumble-bee-foundation-firebase-adminsdk.json");

        var builder = new FirestoreClientBuilder
        {
            Credential = googleCredential
        };
        var firestoreClient = builder.Build();

        _firestoreDb = FirestoreDb.Create("bumble-bee-foundation", firestoreClient);
    }

    public async Task AddDocumentAsync(string collectionName, object data)
    {
        CollectionReference collection = _firestoreDb.Collection(collectionName);
        await collection.AddAsync(data);
    }

    public async Task<DocumentSnapshot> GetDocumentByEmailAsync(string collectionName, string email)
    {
        var collection = _firestoreDb.Collection(collectionName);
        var query = collection.WhereEqualTo("Email", email);
        var querySnapshot = await query.GetSnapshotAsync();

        return querySnapshot.Documents.FirstOrDefault();  // Return the first matching document
    }

    // Additional methods for Firestore operations to be added later
}
