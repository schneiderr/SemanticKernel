using AICore.Plugins;
using AICore.Services;
using Microsoft.SemanticKernel;

namespace Web.POC.Configuration
{
    public static class ServiceConfiguration
    {
        public static void AddAppServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddSingleton<Kernel>(sp =>
            {
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


            services.AddTransient<IAppChatCompletionService, AppChatCompletionService>();
            services.AddHttpClient<GithubPlugin>();
        }
    }
}
