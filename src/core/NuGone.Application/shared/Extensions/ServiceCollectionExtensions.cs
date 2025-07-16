using Microsoft.Extensions.DependencyInjection;
using NuGone.Application.Features.PackageAnalysis.Commands.AnalyzePackageUsage;
using NuGone.Application.Features.PackageAnalysis.Services;
using NuGone.Application.Features.PackageAnalysis.Services.Abstractions;

namespace NuGone.Application.Shared.Extensions;

/// <summary>
/// Extension methods for configuring application services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds application layer services to the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Register command handlers
        _ = services.AddScoped<AnalyzePackageUsageHandler>();

        // Register services
        _ = services.AddScoped<IPackageUsageAnalyzer, PackageUsageAnalyzer>();

        // Add MediatR if needed for CQRS pattern
        // services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(ServiceCollectionExtensions).Assembly));

        return services;
    }
}
