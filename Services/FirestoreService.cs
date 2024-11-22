using Google.Cloud.Firestore;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Firestore.V1;
using BumbleBeeWebApp.Models;
using Microsoft.AspNetCore.Mvc;

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

    public async Task<QuerySnapshot> GetCollectionAsync(string collectionName)
    {
        CollectionReference collectionRef = _firestoreDb.Collection(collectionName);
        return await collectionRef.GetSnapshotAsync();
    }

    public async Task<DocumentSnapshot> GetUserDocumentAsync(string userId)
    {
        CollectionReference usersRef = _firestoreDb.Collection("users");
    
        Query query = usersRef.WhereEqualTo("Uid", userId);

        QuerySnapshot querySnapshot = await query.GetSnapshotAsync();

        if (querySnapshot.Documents.Count > 0)
        {
            return querySnapshot.Documents[0];
        }
        else
        {
            return null;
        }
    }

    // Fetch testimonials from Firestore
    public async Task<List<Testimonial>> GetTestimonialsAsync()
    {
        var testimonials = new List<Testimonial>();

        try
        {
            var testimonialsRef = _firestoreDb.Collection("testimonial");
            var snapshot = await testimonialsRef.GetSnapshotAsync();

            foreach (var document in snapshot.Documents)
            {
                // Safely map Firestore document fields to your model
                var testimonial = new Testimonial
                {
                    UID = document.Id,  // Assuming UID is the Firestore document ID
                    Email = document.ContainsField("Email") ? document.GetValue<string>("Email") : "No email provided", // Default value if missing
                    Content = document.ContainsField("Content") ? document.GetValue<string>("Content") : "No content available", // Default value if missing
                    Type = document.ContainsField("Type") ? document.GetValue<string>("Type") : "General", // Default value if missing
                    SubmittedAt = document.ContainsField("SubmittedAt") ? document.GetValue<DateTime>("SubmittedAt") : DateTime.UtcNow // Default value if missing
                };

                testimonials.Add(testimonial);
            }
        }
        catch (Exception ex)
        {
            // Handle any errors that occur during the Firestore operation
            Console.WriteLine($"Error fetching testimonials: {ex.Message}");
        }

        return testimonials;
    }

    // Fetch a random testimonial
    public async Task<Testimonial> GetRandomTestimonialAsync()
    {
        var testimonials = await GetTestimonialsAsync();
        var random = new Random();

        // Return a random testimonial if available
        return testimonials.Count > 0 ? testimonials[random.Next(testimonials.Count)] : null;
    }

    // Delete user from firestore database
    public async Task DeleteUserFromFirestoreAsync(string userId)
    {
        var userDoc = _firestoreDb.Collection("users").Document(userId);
        await userDoc.DeleteAsync();
    }

    // Delete document from friestore databsae
    public async Task DeleteDocumentAsync(string collectionName, string documentId)
    {
        if (string.IsNullOrEmpty(collectionName) || string.IsNullOrEmpty(documentId))
        {
            throw new ArgumentException("Collection name and document ID cannot be null or empty.");
        }

        DocumentReference docRef = _firestoreDb.Collection(collectionName).Document(documentId);

        // Delete the document
        await docRef.DeleteAsync();
    }

    
}

