using Microsoft.AspNetCore.Mvc;
using Web.POC.Models;
using System.Diagnostics;
using System.Text.Json;

namespace Web.POC.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public IAsyncEnumerable<string> Get(CancellationToken ct) => GetMessagesAsync(ct);

        private static async IAsyncEnumerable<string> GetMessagesAsync(
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
        {
            for (var i = 1; i <= 5; i++)
            {
                ct.ThrowIfCancellationRequested();
                await Task.Delay(5000, ct);
                yield return $"Message {i}";
            }
        }

        [HttpGet]
        public async Task GetSteam(CancellationToken ct)
        {
            Response.ContentType = "text/event-stream; charset=utf-8";
            Response.Headers.CacheControl = "no-cache";
            Response.Headers["X-Accel-Buffering"] = "no";

            await Response.WriteAsync(": connected\n\n", ct);
            await Response.Body.FlushAsync(ct);

            await foreach (var token in GenerateTokens(ct))
            {
                var json = JsonSerializer.Serialize(new { delta = token });
                await Response.WriteAsync($"data: {json}\n\n", ct);
                await Response.Body.FlushAsync(ct);
            }

            await Response.WriteAsync("data: [DONE]\n\n", ct);
            await Response.Body.FlushAsync(ct);
        }

        private static async IAsyncEnumerable<string> GenerateTokens(
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
        {
            string[] words = { "Hello", " ", "from", " ", "MVC", " ", "SSE", "!" };
            foreach (var w in words)
            {
                await Task.Delay(200, ct);
                yield return w;
            }
        }



        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
