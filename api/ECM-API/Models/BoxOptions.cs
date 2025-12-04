using System.Numerics;

namespace ECM_API.Models
{
    public record BoxOptions
    {
        public string ClientId { get; init; } = "";
        public string ClientSecret { get; init; } = "";
        public string RedirectUri { get; init; } = "";
    }

    public class BoxTokenResponse
    {
        public string access_token { get; set; } = "";
        public string token_type { get; set; } = "";
        public int expires_in { get; set; }
        public string refresh_token { get; set; } = "";
        public string scope { get; set; } = "";
        public DateTime ObtainedAt { get; set; } = DateTime.UtcNow;
        public DateTime ExpiresAt => ObtainedAt.AddSeconds(expires_in - 30); // refresh 30s earlier
    }
}
