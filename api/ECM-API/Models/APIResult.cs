namespace ECM_API.Models
{
    public class APIResult<T>
    {
        public bool Success { get; set; }
        public T Data { get; set; }
        public BoxErrorResponse Error { get; set; }
    }
}
