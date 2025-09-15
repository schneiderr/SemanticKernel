using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace AICore.Plugins
{
    public sealed class GithubPlugin
    {
        private readonly HttpClient _http;
        public GithubPlugin(HttpClient http) => _http = http;

        [KernelFunction, Description("List open issues for a repo")]
        public async Task<string> ListIssuesAsync([Description("owner/repo, e.g. dotnet/runtime")] string repo)
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, $"https://api.github.com/repos/{repo}/issues");
            req.Headers.UserAgent.ParseAdd("sk-sample");
            var json = await _http.SendAsync(req).Result.Content.ReadAsStringAsync();
            return json; // let the LLM summarize/transform
        }
    }
}
