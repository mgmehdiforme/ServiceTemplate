using Microsoft.Extensions.AI;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ServiceTemplate.Application.Common.Interfaces;
using ServiceTemplate.Infrastructure.Ai.Configuration;
using ServiceTemplate.Infrastructure.Ai.Decorators;
using ServiceTemplate.Infrastructure.Ai.Mock;

namespace ServiceTemplate.Infrastructure.Ai;

public static class AiServiceRegistration
{
    public static IServiceCollection AddAiInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var aiOptions = new AiOptions();
        configuration.GetSection(AiOptions.SectionName).Bind(aiOptions);
        services.Configure<AiOptions>(configuration.GetSection(AiOptions.SectionName));

        // Register HybridCache for L1/L2 response and embedding caching
        #pragma warning disable EXTEXP0018 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        services.AddHybridCache();
        #pragma warning restore EXTEXP0018

        // Register Embedding Generator
        services.AddSingleton<IEmbeddingGenerator<string, Embedding<float>>>(sp =>
        {
            if (aiOptions.Provider.Equals("Mock", StringComparison.OrdinalIgnoreCase) ||
                string.IsNullOrWhiteSpace(aiOptions.ApiKey))
            {
                var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("AiServiceRegistration");
                logger.LogInformation("Registering OfflineMockEmbeddingGenerator (offline mode, dimensions: {Dimensions})", aiOptions.VectorDimensions);
                return new OfflineMockEmbeddingGenerator(aiOptions.VectorDimensions, aiOptions.EmbeddingModelId);
            }

            // Remote OpenAI / Ollama compatible provider
            var openAiClient = new OpenAI.OpenAIClient(aiOptions.ApiKey);
            return openAiClient.AsEmbeddingGenerator(aiOptions.EmbeddingModelId);
        });

        // Register Resilient Decorator Chat Pipeline
        services.AddSingleton<IChatClient>(sp =>
        {
            IChatClient terminalClient;

            if (aiOptions.Provider.Equals("Mock", StringComparison.OrdinalIgnoreCase) ||
                string.IsNullOrWhiteSpace(aiOptions.ApiKey))
            {
                var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("AiServiceRegistration");
                logger.LogInformation("Registering OfflineMockChatClient (offline mode, model: {ModelId})", aiOptions.ModelId);
                terminalClient = new OfflineMockChatClient(aiOptions.ModelId);
            }
            else
            {
                var openAiClient = new OpenAI.OpenAIClient(aiOptions.ApiKey);
                terminalClient = openAiClient.AsChatClient(aiOptions.ModelId);
            }

            // Decorator 1: Caching Layer (HybridCache)
            IChatClient pipeline = terminalClient;
            if (aiOptions.EnableCaching)
            {
                var hybridCache = sp.GetRequiredService<HybridCache>();
                pipeline = new Decorators.CachingChatClient(pipeline, hybridCache, TimeSpan.FromMinutes(aiOptions.CacheDurationMinutes));
            }

            // Decorator 2: Resilience Layer (Polly v8 Retries & Circuit Breaker)
            pipeline = new ResilientChatClient(pipeline);

            // Decorator 3: Telemetry Layer (OpenTelemetry GenAI Spans)
            pipeline = new TelemetryChatClient(pipeline);

            return pipeline;
        });

        // Register Semantic Search Service
        services.AddScoped<ISemanticSearchService, SemanticSearchService>();

        return services;
    }
}
