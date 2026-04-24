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

        // 3. Register the Resilient HTTP Client on the MAIN service collection
        // This ensures IHttpClientFactory is globally available to the application.
        services.AddHttpClient("GeminiClient")
               .AddStandardResilienceHandler(); // Polly V8

        // 4. Build and Register the Semantic Kernel
        services.AddTransient<Kernel>(sp =>
        {
            var builder = Kernel.CreateBuilder();

#pragma warning disable SKEXP0070 
            builder.AddGoogleAIGeminiChatCompletion(
                modelId: "gemini-2.5-flash",
                apiKey: geminiApiKey,
                // We can now safely resolve the factory from the main provider
                httpClient: sp.GetRequiredService<IHttpClientFactory>().CreateClient("GeminiClient"));
#pragma warning restore SKEXP0070

            return builder.Build();
        });

        return services;
    }
}