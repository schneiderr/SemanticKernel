using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Memory;
using System.ComponentModel;
using AICore.Plugins;

namespace CLI.POC
{
    class Program
    {
        static async Task Main(string[] args)
        {
            using IHost host = Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration(cfg =>
                {
                    cfg.AddJsonFile("appsettings.json", optional: true)
                        .AddEnvironmentVariables(); // allow AI__ApiKey, etc.
                })
                .ConfigureServices((context, services) =>
                {
                    services.AddSingleton<Kernel>(sp =>
                    {
                        var configuration = sp.GetRequiredService<IConfiguration>();
                        var kernel = Kernel.CreateBuilder();

                        kernel.AddAzureOpenAIChatCompletion(
                            configuration["AZURE_OPENAI_DEPLOYMENT"],
                            configuration["AZURE_OPENAI_ENDPOINT"],
                            configuration["AZURE_OPENAI_API_KEY"]
                        );

                        kernel.Plugins.Add(KernelPluginFactory.CreateFromType<MenuPlugin>());
                        //kernel.Plugins.Add(KernelPluginFactory.CreateFromType<GithubPlugin>());
                        kernel.Plugins.AddFromObject(sp.GetRequiredService<GithubPlugin>(), "gh");

                        // Optional: add your native plugin(s)
                        kernel.Plugins.AddFromObject(new TimePlugin(), "time");

                        return kernel.Build();
                    });

                    services.AddTransient<App>();
                    services.AddHttpClient<GithubPlugin>();
                })
                .ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddConsole();
                })
                .Build();

            // Run once and exit. If you have background work, use IHostedService instead.
            await host.Services.GetRequiredService<App>().RunAsync();
        }
    }

    public class App
    {
        private readonly Kernel _kernel;
        private readonly ILogger<App> _logger;

        public App(Kernel kernel, ILogger<App> logger)
        {
            _kernel = kernel;
            _logger = logger;
        }

        public async Task RunAsync()
        {
            _logger.LogInformation("Starting SK console app…");

            // 1) Chat completion (conversational)
            var chat = _kernel.GetRequiredService<IChatCompletionService>();
            var history = new ChatHistory("You are a concise, helpful assistant.");
            history.AddUserMessage("In one sentence, what time is it and what can you do?");
            history.AddSystemMessage("You can call a 'time' plugin to report the current time if useful.");
            history.AddSystemMessage(
                """
                You can use this tool:
                - gh.ListIssuesAsync(repo)

                Task: Show 3 concise bullets summarizing the newest open issues for the 'repo'.
                """);
            string? userInput;
            do
            {
                // Collect user input
                Console.Write("User > ");
                userInput = Console.ReadLine();

                // Add user input
                history.AddUserMessage(userInput);

                // Get the response from the AI
                var settings = new OpenAIPromptExecutionSettings { ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions };
                var reply = await chat.GetChatMessageContentAsync(history, kernel: _kernel, executionSettings: settings);
                Console.WriteLine($"\nAssistant: {reply.Content}\n");

                // Add the message from the agent to the chat history
                history.AddMessage(reply.Role, reply.Content ?? string.Empty);
            } while (userInput is not null);


            //// 2) Call a native plugin function directly
            //var timeNow = await _kernel.InvokeAsync(
            //    pluginName: "time",
            //    functionName: nameof(TimePlugin.NowIso8601));
            //Console.WriteLine($"Plugin time: {timeNow}\n");

            //// 3) Prompt as a function (ad-hoc)
            //var summarize = _kernel.CreateFunctionFromPrompt(
            //    "Summarize the following text in 20 words or fewer:\n{{$input}}");
            //var sum = await _kernel.InvokeAsync(summarize, new() { ["input"] = "Semantic Kernel lets me compose LLM calls, tools, and memory using DI." });
            //Console.WriteLine($"Summary: {sum}\n");

            //// 4) (Optional) Store & recall with SK Memory
            //if (_kernel.Services.GetService<ISemanticTextMemory>() is { } memory)
            //{
            //    await memory.SaveInformationAsync(collection: "notes", id: "n1",
            //        text: "CDC: improve UX/accessibility, comms effectiveness, stakeholder trust.");
            //    var recalled = memory.SearchAsync("notes", "stakeholder trust", limit: 1, minRelevanceScore: 0.6);
            //    await foreach (var m in recalled)
            //        Console.WriteLine($"Memory hit: {m.Metadata.Text} (R={m.Relevance:F2})");
            //}

            _logger.LogInformation("Done.");
        }

        public async Task RunAsyncAgent()
        {

            ChatCompletionAgent agent = new()
            {
                Name = "SK-Assistant",
                Instructions = "You are a helpful assistant.",
                Kernel = _kernel,
                Arguments = new KernelArguments(new PromptExecutionSettings() { FunctionChoiceBehavior = FunctionChoiceBehavior.Auto() })
            };

            await foreach (AgentResponseItem<ChatMessageContent> response in agent.InvokeAsync("What is the price of the soup special?"))
            {
                Console.WriteLine(response.Message);
            }
        }
    }
}