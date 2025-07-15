using Microsoft.Extensions.DependencyInjection;
using NuGone.Application.Features.PackageAnalysis.Services.Abstractions;
using NuGone.NuGet.Repositories;

namespace NuGone.NuGet.Extensions;

/// <summary>
/// Extension methods for configuring NuGet infrastructure services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds NuGet infrastructure services to the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddNuGetServices(this IServiceCollection services)
    {
        // Register repositories
        services.AddScoped<INuGetRepository, NuGetRepository>();

        return services;
    }
}
