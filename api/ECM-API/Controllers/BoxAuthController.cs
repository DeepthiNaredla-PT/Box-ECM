using ECM_API.Models;
using ECM_API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace ECM_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BoxAuthController : ControllerBase
    {
        private readonly BoxOptions _opts;
        private readonly BoxAuthService _authService;
        private readonly Store _store;

        public BoxAuthController(
            IOptions<BoxOptions> opts,
            BoxAuthService auth,
            Store store)
        {
            _opts = opts.Value;
            _authService = auth;
            _store = store;
        }

        [HttpGet("/boxauth/login")]
        public IActionResult Login(string returnUrl)
        {
            Console.WriteLine(returnUrl);
            _store.StoreUrl(returnUrl);
            var url = "https://account.box.com/api/oauth2/authorize" +
                      $"?response_type=code&client_id={_opts.ClientId}" +
                      $"&redirect_uri={Uri.EscapeDataString(_opts.RedirectUri)}";

            return Redirect(url);
        }

        [HttpGet("/boxauth/callback")]
        public async Task<IActionResult> Callback(string code)
        {
            var token = await _authService.ExchangeCodeForTokenAsync(code);
            if (token == null) return Content("OAuth error");

            var userId = Guid.NewGuid().ToString("N");

            _store.StoreTokens(userId, token);

            // Redirect back to React UI with userId
            Console.WriteLine(_store.GetUrl());
            return Redirect($"{_store.GetUrl()}?userId={userId}");
            //return Redirect($"http://10.10.97.135:3000/auth/callback?userId={userId}");
        }
    }
}
