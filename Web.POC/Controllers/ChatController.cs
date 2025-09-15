using Microsoft.AspNetCore.Mvc;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using System.Text.Json;

namespace Web.POC.Controllers
{
    [Route("api/[controller]")]
    public sealed class ChatController : ControllerBase
    {
        private readonly Kernel _kernel;
        private readonly IChatCompletionService _chat;

        public ChatController(Kernel kernel)
        {
            _kernel = kernel;
            // retrieve the chat completion service registered with the kernel
            _chat = _kernel.GetRequiredService<IChatCompletionService>();
        }

        // GET /api/stream?prompt=hello
        [HttpGet]
        public async Task Get([FromQuery] string prompt, CancellationToken ct)
        {
            Response.ContentType = "text/event-stream; charset=utf-8";
            Response.Headers.CacheControl = "no-cache, no-transform";
            Response.Headers["X-Accel-Buffering"] = "no";

            // initial ping for fast first byte
            await Response.WriteAsync(": connected\n\n", ct);
            await Response.Body.FlushAsync(ct);

            // build the chat (system + user)
            var chat = new ChatHistory();
            chat.AddSystemMessage("You are a concise assistant. Stream partial tokens promptly.");
            chat.AddUserMessage(prompt);

            //// request settings (tune as needed)
            //var settings = new ChatRequestSettings
            //{
            //    Temperature = 0.3,
            //    TopP = 0.95,
            //    // MaxOutputTokenCount = 1024, // optional
            //};

            // stream tokens from SK
            await foreach (var chunk in _chat.GetStreamingChatMessageContentsAsync(chat, kernel: _kernel, cancellationToken: ct)
                .WithCancellation(ct))
            {
                // chunk.Content is the partial text; some providers also set Role/Metadata
                if (!string.IsNullOrEmpty(chunk.Content))
                {
                    var json = JsonSerializer.Serialize(new { delta = chunk.Content });
                    await Response.WriteAsync($"data: {json}\n\n", ct);
                    await Response.Body.FlushAsync(ct);
                }
            }

            // done marker (ChatGPT-like)
            await Response.WriteAsync("data: [DONE]\n\n", ct);
            await Response.Body.FlushAsync(ct);
        }
    }
}

