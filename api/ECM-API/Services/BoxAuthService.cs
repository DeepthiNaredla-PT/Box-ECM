using ECM_API.Models;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace ECM_API.Services
{
    public class BoxAuthService
    {
        private readonly IHttpClientFactory _http;
        private readonly BoxOptions _opts;

        public BoxAuthService(IHttpClientFactory http, IOptions<BoxOptions> opts)
        {
            _http = http;
            _opts = opts.Value;
        }

        public async Task<BoxTokenResponse?> ExchangeCodeForTokenAsync(string code)
        {
            var client = _http.CreateClient();
            var form = new Dictionary<string, string>
        {
            { "grant_type", "authorization_code" },
            { "code", code },
            { "client_id", _opts.ClientId },
            { "client_secret", _opts.ClientSecret },
            { "redirect_uri", _opts.RedirectUri }
        };

            var result = await client.PostAsync("https://api.box.com/oauth2/token",
                new FormUrlEncodedContent(form));

            if (!result.IsSuccessStatusCode)
                return null;

            var json = await result.Content.ReadAsStringAsync();
            var token = JsonSerializer.Deserialize<BoxTokenResponse>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            token!.ObtainedAt = DateTime.UtcNow;
            return token;
        }

        public async Task<BoxTokenResponse?> RefreshTokenAsync(BoxTokenResponse oldToken)
        {
            var client = _http.CreateClient();
            var form = new Dictionary<string, string>
        {
            { "grant_type", "refresh_token" },
            { "refresh_token", oldToken.refresh_token },
            { "client_id", _opts.ClientId },
            { "client_secret", _opts.ClientSecret }
        };

            var result = await client.PostAsync("https://api.box.com/oauth2/token",
                new FormUrlEncodedContent(form));

            if (!result.IsSuccessStatusCode)
                return null;

            var json = await result.Content.ReadAsStringAsync();
            var token = JsonSerializer.Deserialize<BoxTokenResponse>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            token!.ObtainedAt = DateTime.UtcNow;
            return token;
        }
    }
}
