namespace BumbleBeeWebApp.Models
{
    public class CompanyDocument
    {
        public int DocumentID { get; set; }
        public int CompanyID { get; set; }
        public string Type { get; set; }
        public DateTime UploadDate { get; set; }
        public string FilePath { get; set; }

        public virtual Company Company { get; set; }
    }
}
