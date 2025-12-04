using Newtonsoft.Json.Linq;
using System.Net.Http.Headers;

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

        public async Task TriggerVectorService(string fileId)
        {
            var client = _http.CreateClient();
            var response = await client.PostAsync($"{vectorBaseUrl}vectorize_document/{fileId}", null);
            Console.WriteLine( response );
        }
    }
}
