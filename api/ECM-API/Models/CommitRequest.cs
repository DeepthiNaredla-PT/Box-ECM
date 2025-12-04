namespace ECM_API.Models
{
    public class CommitRequest
    {
        public string Digest { get; set; }
        public List<BoxPart> Parts { get; set; }
    }
}
