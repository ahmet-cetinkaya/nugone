using Spectre.Console.Cli;

namespace NuGone.Cli.Infrastructure;

/// <summary>
/// Type resolver for Spectre.Console.Cli dependency injection.
/// Resolves services from the Microsoft.Extensions.DependencyInjection container.
/// </summary>
internal sealed class TypeResolver(IServiceProvider provider) : ITypeResolver, IDisposable
{
    private readonly IServiceProvider _provider =
        provider ?? throw new ArgumentNullException(nameof(provider));

    /// <summary>
    /// Resolves a service of the specified type from the service provider.
    /// </summary>
    /// <param name="type">The type of service to resolve</param>
    /// <returns>The resolved service instance, or null if not found</returns>
    public object? Resolve(Type? type)
    {
        if (type == null)
        {
            return null;
        }

        return _provider.GetService(type);
    }

    /// <summary>
    /// Disposes the underlying service provider if it's disposable.
    /// </summary>
    public void Dispose()
    {
        if (_provider is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}
