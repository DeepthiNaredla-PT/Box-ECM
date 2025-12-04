namespace ECM_API.Models
{
    public class BoxErrorResponse
    {
        public string Type { get; set; }           // "error"
        public int Status { get; set; }            // 409, 403, etc.
        public string Code { get; set; }           // "item_name_in_use"
        public string Message { get; set; }        // Human readable message
        public ContextInfo Context_Info { get; set; }
        public string Help_Url { get; set; }
        public string Request_Id { get; set; }
    }

    public class ContextInfo
    {
        public Conflict Conflicts { get; set; }    // Only exists for 409 duplicate file errors
        public List<ErrorField> Errors { get; set; }  // Can appear on validation errors
    }

    public class Conflict
    {
        public string Type { get; set; }           // "file"
        public string Id { get; set; }             // Existing file ID
        public string Name { get; set; }           // Existing file name
    }

    public class ErrorField
    {
        public string Reason { get; set; }         // e.g., "invalid_parameter"
        public string Name { get; set; }           // Field name
        public string Message { get; set; }        // Detail message
    }
}
