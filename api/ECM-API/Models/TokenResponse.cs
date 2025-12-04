namespace ECM_API.Models
{
    public class TokenResponse
    {
        public string AccessToken { get; set; } = "";
        public int ExpiresIn { get; set; }
    }
}
