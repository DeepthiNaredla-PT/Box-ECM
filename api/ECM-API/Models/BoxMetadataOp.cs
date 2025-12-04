namespace ECM_API.Models
{
    public class BoxMetadataOp
    {
        public string Op { get; set; }     // add, replace, remove
        public string Path { get; set; }   // field path
        public object Value { get; set; }  // field value
    }
}
