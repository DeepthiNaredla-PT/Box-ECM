using ECM_API.Models;

namespace ECM_API.Services
{
    public class TokenService
    {
        private readonly Store _store;
        private readonly BoxAuthService _auth;

        public TokenService(Store store, BoxAuthService auth)
        {
            _store = store;
            _auth = auth;
        }

        public async Task<TokenResponse?> GetToken(string userId)
        {
            
            if (!_store.TryGetTokens(userId, out var token))
                return null;
            TokenResponse response = new TokenResponse()
            {
                AccessToken = token.access_token,
                ExpiresIn = token.expires_in,
            };

            if (DateTime.UtcNow >= token.ExpiresAt)
            {
                var refreshed = await _auth.RefreshTokenAsync(token);
                if (refreshed == null) return null;

                _store.UpdateTokens(userId, refreshed);
                response.AccessToken = refreshed.access_token;
                response.ExpiresIn = refreshed.expires_in;
            }

            return response;
        }
    }
}
