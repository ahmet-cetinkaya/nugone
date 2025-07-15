using System.IO.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using NuGone.Application.Features.PackageAnalysis.Services.Abstractions;
using NuGone.FileSystem.Repositories;

namespace NuGone.FileSystem.Extensions;

/// <summary>
/// Extension methods for configuring file system infrastructure services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds file system infrastructure services to the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddFileSystemServices(this IServiceCollection services)
    {
        // Register file system abstraction
        services.AddSingleton<IFileSystem, System.IO.Abstractions.FileSystem>();

        // Register repositories
        services.AddScoped<IProjectRepository, ProjectRepository>();
        services.AddScoped<ISolutionRepository, SolutionRepository>();

        return services;
    }
}
