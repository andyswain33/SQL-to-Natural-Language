using Gateway.Core.Mapping;
using Gateway.Core.Orchestration;
using Gateway.Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;

namespace Gateway.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, string geminiApiKey)
        {
            // 1. Register our Core Mappers and Interceptors
            services.AddSingleton<MetadataMapper>();
            services.AddSingleton<SqlSafetyInterceptor>();
            services.AddTransient<SqlGenerationOrchestrator>();
            services.AddTransient<SqlExecutionService>();

            // 2. Build and Register the Semantic Kernel
            services.AddTransient<Kernel>(sp =>
            {
                var builder = Kernel.CreateBuilder();

                // Suppress the experimental warning for the Google Connector
#pragma warning disable SKEXP0070 
                builder.AddGoogleAIGeminiChatCompletion(
                    modelId: "gemini-2.5-flash",
                    apiKey: geminiApiKey);
#pragma warning restore SKEXP0070

                return builder.Build();
            });

            return services;
        }
    }
}