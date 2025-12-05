namespace ECM_API.Services
{
    public class TriggerService
    {
        private readonly IHttpClientFactory _http;
        private readonly TokenService _tokenService;
        private readonly string _vectorBaseUrl = "https://ignite.pal.tech/colibri/api/";
        private readonly string _userId = "f685845db3f048cb8dfc874727021c8f";

        public TriggerService(IHttpClientFactory http, TokenService tokenService)
        {
            _http = http;
            _tokenService = tokenService;
        }

        public async Task<bool> TriggerVectorService(string fileId)
        {
            try
            {
                var client = _http.CreateClient();
                var tokenResponse = await _tokenService.GetToken(_userId);
                var response = await client.PostAsync($"{_vectorBaseUrl}vectorize_document/{fileId}?token={tokenResponse.AccessToken}", null);
                Console.WriteLine(response);
                response.EnsureSuccessStatusCode();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return false;
            }
        }
    }
}
