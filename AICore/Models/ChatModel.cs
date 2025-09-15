using System;

namespace AICore.Models
{
    public class ChatSession
    {
        public Guid Id { get; set; }
        public string? UserId { get; set; }          // if authenticated
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
        public ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
    }

    public class ChatMessage
    {
        public Guid Id { get; set; }
        public Guid SessionId { get; set; }
        public ChatSession Session { get; set; } = default!;
        public string Role { get; set; } = "user";    // "user" | "assistant" | "system"
        public string Content { get; set; } = "";
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
        public ICollection<ToolCallLog> ToolCalls { get; set; } = new List<ToolCallLog>();
        public ICollection<RetrievalLog> Retrievals { get; set; } = new List<RetrievalLog>();
        public TokenUsage? Usage { get; set; }        // optional
    }

    public class ToolCallLog
    {
        public Guid Id { get; set; }
        public Guid MessageId { get; set; }
        public string Name { get; set; } = "";
        public string ArgumentsJson { get; set; } = "";
        public string ResultJson { get; set; } = "";
        public double? DurationMs { get; set; }
    }

    public class RetrievalLog
    {
        public Guid Id { get; set; }
        public Guid MessageId { get; set; }
        public string Corpus { get; set; } = "";          // e.g., "ai-search"
        public string DocumentId { get; set; } = "";
        public double? Score { get; set; }                 // your vector/BM25 score
        public string? Snippet { get; set; }
    }

    public class TokenUsage
    {
        public Guid Id { get; set; }
        public Guid MessageId { get; set; }
        public int? PromptTokens { get; set; }
        public int? CompletionTokens { get; set; }
        public int? TotalTokens => (PromptTokens ?? 0) + (CompletionTokens ?? 0);
    }

}
