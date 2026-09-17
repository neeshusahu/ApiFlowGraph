using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Reader;

public static class ServiceCollectionExtension
{
    public static IServiceCollection AddOpenApi(this IServiceCollection services)
    {
        services.AddOllamaNetConfig(options =>
        {
            options.BaseAddress = "http://localhost:11434";

            options.Models.Add(
            new OllamaModel
            {
                ModelName = "phi4-mini",
                type = ModelOperation.Generate,
            });
        });

        services.AddScoped<DependencyGraph>();
        services.AddSingleton(_ =>
        {
            var readerSettings = new OpenApiReaderSettings();
            readerSettings.AddYamlReader();
            return readerSettings;
        });
        services.AddSingleton<GraphOnlyPrompt>();
        services.AddSingleton<GraphWithSpecPrompt>();
        services.AddSingleton<RawSpecPrompt>();
        services.AddTransient<OpenApiGraphExtractor>();

        return services;
    }
}