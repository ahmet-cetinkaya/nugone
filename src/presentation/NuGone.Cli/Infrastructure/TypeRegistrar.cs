using Microsoft.Extensions.DependencyInjection;
using Spectre.Console.Cli;

namespace NuGone.Cli.Infrastructure;

/// <summary>
/// Type registrar for Spectre.Console.Cli dependency injection.
/// Bridges the Microsoft.Extensions.DependencyInjection container with Spectre.Console.Cli.
/// </summary>
public sealed class TypeRegistrar(IServiceCollection builder) : ITypeRegistrar
{
    private readonly IServiceCollection _builder = builder;

    /// <summary>
    /// Builds the service provider and creates a type resolver.
    /// </summary>
    /// <returns>A type resolver for the built service provider</returns>
    public ITypeResolver Build()
    {
        return new TypeResolver(_builder.BuildServiceProvider());
    }

    /// <summary>
    /// Registers a service type with its implementation type.
    /// </summary>
    /// <param name="service">The service type to register</param>
    /// <param name="implementation">The implementation type to register</param>
    public void Register(Type service, Type implementation)
    {
        _ = _builder.AddSingleton(service, implementation);
    }

    /// <summary>
    /// Registers a service instance.
    /// </summary>
    /// <param name="service">The service type to register</param>
    /// <param name="implementation">The service instance to register</param>
    public void RegisterInstance(Type service, object implementation)
    {
        _ = _builder.AddSingleton(service, implementation);
    }

    /// <summary>
    /// Registers a lazy service factory.
    /// </summary>
    /// <param name="service">The service type to register</param>
    /// <param name="factory">The factory function to create the service instance</param>
    public void RegisterLazy(Type service, Func<object> factory)
    {
        _ = _builder.AddSingleton(service, _ => factory());
    }
}
