using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using System.Text;
using System.Text.Json;
using AICore.Models;

namespace Web.POC.Controllers
{
    [Route("api/[controller]")]
    public sealed class ChatController : ControllerBase
    {
        private readonly Kernel _kernel;
        private readonly IChatCompletionService _chat;
        private readonly IMemoryCache _cache;

        // Define available tools (customize as needed)
        const string availableTools = "Available tools:\n- gh.ListIssuesAsync(repo)\n- time.NowIso8601\n";

        public ChatController(Kernel kernel, IMemoryCache cache)
        {
            _kernel = kernel;
            // retrieve the chat completion service registered with the kernel
            _chat = _kernel.GetRequiredService<IChatCompletionService>();
            _cache = cache;
        }

        // GET /api/stream?prompt=hello
        [HttpGet]
        public async Task Get([FromQuery] string prompt, CancellationToken ct)
        {

            // Ensure session ID exists
            var sessionId = HttpContext.Session.GetString("ChatSessionId");
            if (string.IsNullOrEmpty(sessionId))
            {
                sessionId = Guid.NewGuid().ToString();
                HttpContext.Session.SetString("ChatSessionId", sessionId);
            }

            Response.ContentType = "text/event-stream; charset=utf-8";
            Response.Headers.CacheControl = "no-cache, no-transform";
            Response.Headers["X-Accel-Buffering"] = "no";

            // initial ping for fast first byte
            await Response.WriteAsync(": connected\n\n", ct);
            await Response.Body.FlushAsync(ct);

            // Retrieve or create chat session from cache
            ChatSession chatSession;
            if (!_cache.TryGetValue(sessionId, out chatSession))
            {
                chatSession = new ChatSession { Id = Guid.Parse(sessionId) };
                chatSession.Messages.Add(new ChatMessage
                {
                    Id = Guid.NewGuid(),
                    SessionId = chatSession.Id,
                    Role = "system",
                    Content = "You are a concise assistant. Stream partial tokens in markdown promptly.\n" + availableTools,
                    CreatedUtc = DateTime.UtcNow
                });

                _cache.Set(sessionId, chatSession);
            }

            // Add user message to session
            chatSession.Messages.Add(new ChatMessage
            {
                Id = Guid.NewGuid(),
                SessionId = chatSession.Id,
                Role = "user",
                Content = prompt,
                CreatedUtc = DateTime.UtcNow
            });

            // Build chat history for SK
            var chat = new ChatHistory();
            foreach (var msg in chatSession.Messages)
            {
                if (msg.Role == "user") chat.AddUserMessage(msg.Content);
                else if (msg.Role == "assistant") chat.AddAssistantMessage(msg.Content);
                else if (msg.Role == "system") chat.AddSystemMessage(msg.Content);
            }

            // stream tokens from SK
            var settings = new OpenAIPromptExecutionSettings { ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions };

            // Accumulate streamed tokens
            var assistantResponseBuilder = new StringBuilder();
            await foreach (var chunk in _chat.GetStreamingChatMessageContentsAsync(chat, executionSettings: settings, kernel: _kernel, cancellationToken: ct)
                .WithCancellation(ct))
            {
                if (!string.IsNullOrEmpty(chunk.Content))
                {
                    assistantResponseBuilder.Append(chunk.Content);
                    var json = JsonSerializer.Serialize(new { delta = chunk.Content });
                    await Response.WriteAsync($"data: {json}\n\n", ct);
                    await Response.Body.FlushAsync(ct);
                }
            }

            // Add assistant message to session with full streamed response
            var assistantResponse = assistantResponseBuilder.ToString();
            if(!string.IsNullOrWhiteSpace(assistantResponse))
            {
                chatSession.Messages.Add(new ChatMessage
                {
                    Id = Guid.NewGuid(),
                    SessionId = chatSession.Id,
                    Role = "assistant",
                    Content = assistantResponseBuilder.ToString(),
                    CreatedUtc = DateTime.UtcNow
                });
            }

            _cache.Set(sessionId, chatSession);

            // done marker (ChatGPT-like)
            await Response.WriteAsync("data: [DONE]\n\n", ct);
            await Response.Body.FlushAsync(ct);
        }

        [HttpGet, Route("[controller]/peek/history")]
        public async Task<IActionResult> PeakHistory([FromQuery] string prompt, CancellationToken ct)
        {
            // Ensure session ID exists
            var sessionId = HttpContext.Session.GetString("ChatSessionId");
            if (string.IsNullOrEmpty(sessionId))
            {
                sessionId = Guid.NewGuid().ToString();
                HttpContext.Session.SetString("ChatSessionId", sessionId);
            }

            // Retrieve or create chat session from cache
            ChatSession chatSession;
            if (!_cache.TryGetValue(sessionId, out chatSession))
            {
                chatSession = new ChatSession { Id = Guid.NewGuid() };
                chatSession.Messages.Add(new ChatMessage
                {
                    Id = Guid.NewGuid(),
                    SessionId = chatSession.Id,
                    Role = "system",
                    Content = "You are a concise assistant. Stream partial tokens promptly.\n" + availableTools,
                    CreatedUtc = DateTime.UtcNow
                });

                _cache.Set(sessionId, chatSession);
            }

            // Add user message to session
            chatSession.Messages.Add(new ChatMessage
            {
                Id = Guid.NewGuid(),
                SessionId = chatSession.Id,
                Role = "user",
                Content = prompt,
                CreatedUtc = DateTime.UtcNow
            });

            // Build chat history for SK
            var chat = new ChatHistory();
            foreach (var msg in chatSession.Messages)
            {
                if (msg.Role == "user") chat.AddUserMessage(msg.Content);
                else if (msg.Role == "assistant") chat.AddAssistantMessage(msg.Content);
                else if (msg.Role == "system") chat.AddSystemMessage(msg.Content);
            }

            return new JsonResult(new { history = chat });
        }
    }
}

