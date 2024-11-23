using Google.Cloud.Firestore;

namespace BumbleBeeWebApp.Models
{
    [FirestoreData]
    public class Company
    {
        [FirestoreDocumentId]
        public string CompanyID { get; set; }

        [FirestoreProperty]
        public string UID { get; set; }

        [FirestoreProperty]
        public string Name { get; set; }

        [FirestoreProperty]
        public string ReferenceNumber { get; set; }

        [FirestoreProperty]
        public string TaxNumber { get; set; }

        [FirestoreProperty]
        public string Description { get; set; }

        [FirestoreProperty]
        public string Email { get; set; }

        [FirestoreProperty]
        public string PhoneNumber { get; set; }

        [FirestoreProperty]
        public string ApprovalStatus { get; set; }

        [FirestoreProperty]
        public string DocumentUrl { get; set; }
    }
}
