using Microsoft.Extensions.DependencyInjection;
using QuickNET.Compilation;
using QuickNET.Templates;

namespace QuickNET;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddQuickNETCore(this IServiceCollection services)
    {
        services.AddSingleton<ITemplateEngine, CSharpTemplateEngine>();
        services.AddSingleton<ITemplateEngine, VbTemplateEngine>();
        services.AddSingleton<CompilationService>();
        return services;
    }
}
