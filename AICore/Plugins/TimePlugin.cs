using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace AICore.Plugins
{
    // Example native plugin
    public sealed class TimePlugin
    {
        [KernelFunction, System.ComponentModel.Description("Returns the current time in ISO-8601")]
        public string NowIso8601() => DateTimeOffset.Now.ToString("O");
    }
}
