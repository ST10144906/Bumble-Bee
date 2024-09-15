namespace BumbleBeeWebApp.Models
{
    public class ProjectDocuments
    {
        public int DocumentID { get; set; }
        public int ProjectID { get; set; }
        public string Type { get; set; }
        public DateTime UploadDate { get; set; }
        public string FilePath { get; set; }

        public virtual Project Project { get; set; }
    }
}
