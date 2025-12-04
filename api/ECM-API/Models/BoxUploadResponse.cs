namespace ECM_API.Models
{
    public class BoxUploadResponse
    {
        public int Total_Count { get; set; }
        public List<BoxFileEntry> Entries { get; set; }
    }

    public class BoxFileEntry
    {
        public string Type { get; set; }        // "file"
        public string Id { get; set; }          // FileId (IMPORTANT)
        public string Name { get; set; }        // File name
        public string Etag { get; set; }
        public string Sha1 { get; set; }
        public BoxFileVersion File_Version { get; set; }
    }

    public class BoxFileVersion
    {
        public string Type { get; set; }        // "file_version"
        public string Id { get; set; }          // Version ID
        public string Sha1 { get; set; }
    }

    public class BoxFile
    {
        public string Type { get; set; }
        public string Id { get; set; }
        public string Name { get; set; }
        public long Size { get; set; }
        public string Etag { get; set; }
        public BoxFileVersion File_Version { get; set; }
    }

    public class BoxUploadSessionResponse
    {
        public string Id { get; set; }               // upload session id
        public long Part_Size { get; set; }          // recommended chunk size
        public DateTimeOffset Session_Expires_At { get; set; }
    }

    public class BoxPart
    {
        public string Part_Id { get; set; }
        public long Offset { get; set; }
        public long Size { get; set; }
    }

    public class BoxUploadPartResponse
    {
        public BoxPart Part { get; set; }
    }
}
