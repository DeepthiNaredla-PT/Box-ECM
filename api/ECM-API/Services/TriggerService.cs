namespace ECM_API.Services
{
    public class TriggerService
    {
        private readonly IHttpClientFactory _http;
        private readonly string vectorBaseUrl = "https://ignite.pal.tech/colibri/api/";
        public TriggerService(IHttpClientFactory http)
        {
            _http = http;
        }

        public async Task<bool> TriggerVectorService(string fileId)
        {
            try
            {
                var client = _http.CreateClient();
                var response = await client.PostAsync($"{vectorBaseUrl}vectorize_document/{fileId}", null);
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
