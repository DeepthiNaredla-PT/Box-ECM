using ECM_API.Models;
namespace ECM_API.Services
{
    public class Store
    {
        private readonly Dictionary<string, BoxTokenResponse> _store = new();
        private string _returnUrl = string.Empty;

        public void StoreTokens(string userId, BoxTokenResponse token)
        {
            _store[userId] = token;
        }

        public bool TryGetTokens(string userId, out BoxTokenResponse token)
        {
            return _store.TryGetValue(userId, out token);
        }

        public void UpdateTokens(string userId, BoxTokenResponse token)
        {
            _store[userId] = token;
        }

        public void StoreUrl(string returnUrl)
        {
            _returnUrl = returnUrl;
        }

        public string GetUrl()
        {
            return _returnUrl;
        }
    }
}
