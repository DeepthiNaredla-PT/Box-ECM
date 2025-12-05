namespace ECM_API.Models
{
    public class FileUploadResponse
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public bool IsVectorizationTriggered { get; set; } = false;
    }
}
