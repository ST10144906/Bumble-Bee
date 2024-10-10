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

    
    public async Task<DocumentSnapshot> GetDocumentAsync(string collectionName, string documentId)
    {
        DocumentReference document = _firestoreDb.Collection(collectionName).Document(documentId);
        DocumentSnapshot snapshot = await document.GetSnapshotAsync();

        if (snapshot.Exists)
        {
            return snapshot;
        }

        throw new Exception($"Document with ID '{documentId}' not found in collection '{collectionName}'.");
    }

    public async Task UpdateDocumentAsync(string collectionName, string documentId, object data)
    {
        DocumentReference document = _firestoreDb.Collection(collectionName).Document(documentId);
        await document.SetAsync(data, SetOptions.MergeAll);
    }

    public async Task<DocumentSnapshot> GetUserDocumentAsync(string userId)
{
    // Replace "users" with your Firestore collection name
    CollectionReference usersRef = _firestoreDb.Collection("users");
    
    // Create a query to find the document with uid field equal to userId
    Query query = usersRef.WhereEqualTo("Uid", userId);

    // Execute the query
    QuerySnapshot querySnapshot = await query.GetSnapshotAsync();

    // Check if any document was found
    if (querySnapshot.Documents.Count > 0)
    {
        // Return the first matching document
        return querySnapshot.Documents[0];
    }
    else
    {
        return null; // Return null if no document was found
    }
}



    // Additional methods for Firestore operations to be added later
}
