using Gateway.Core.Interfaces;
using Gateway.Core.Mapping;
using Gateway.Core.Orchestration;
using Gateway.Core.Services;
using Gateway.Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;

namespace Gateway.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, string geminiApiKey)
    {
        // 1. Register Core Mappers, Services, and Orchestrators
        services.AddSingleton<MetadataMapper>();
        services.AddTransient<DataMaskingService>();
        services.AddTransient<SqlGenerationOrchestrator>();

        // 2. Register Infrastructure Implementations against Core Interfaces
        services.AddTransient<ISqlSafetyInterceptor, SqlSafetyInterceptor>();
        services.AddTransient<IQueryExecutor, SqlExecutionService>();

        // 3. Build and Register the Semantic Kernel with Polly V8 Resilience
        services.AddTransient<Kernel>(sp =>
        {
            var builder = Kernel.CreateBuilder();

            // Inject the resilient HTTP client to handle Gemini 429 errors globally
            builder.Services.AddHttpClient("GeminiClient")
                   .AddStandardResilienceHandler();

#pragma warning disable SKEXP0070 
            builder.AddGoogleAIGeminiChatCompletion(
                modelId: "gemini-2.5-flash",
                apiKey: geminiApiKey,
                httpClient: sp.GetRequiredService<IHttpClientFactory>().CreateClient("GeminiClient"));
#pragma warning restore SKEXP0070

            return builder.Build();
        });

        return services;
    }
}