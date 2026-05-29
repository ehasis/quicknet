using Microsoft.Extensions.DependencyInjection;
using QuickNET.Compilation;
using QuickNET.Execution;
using QuickNET.History;
using QuickNET.Templates;

namespace QuickNET;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddQuickNETCore(this IServiceCollection services)
    {
        services.AddSingleton<ITemplateEngine, CSharpTemplateEngine>();
        services.AddSingleton<ITemplateEngine, VbTemplateEngine>();
        services.AddSingleton<CompilationService>();
        services.AddSingleton<ExecutionService>();
        services.AddSingleton<ReplEngine>();
        services.AddSingleton<HistoryManager>();
        services.AddSingleton<HistoryService>();
        return services;
    }
}
