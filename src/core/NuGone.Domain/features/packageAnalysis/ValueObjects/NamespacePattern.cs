namespace NuGone.Domain.Features.PackageAnalysis.ValueObjects;

/// <summary>
/// Value object representing a namespace pattern for package usage detection.
/// RFC-0002: Namespace pattern matching for usage scanning.
/// </summary>
public class NamespacePattern : IEquatable<NamespacePattern>
{
    public NamespacePattern(string pattern)
    {
        if (string.IsNullOrWhiteSpace(pattern))
            throw new ArgumentException("Pattern cannot be null or empty", nameof(pattern));

        Pattern = pattern.Trim();
        IsWildcard = Pattern.Contains('*');
        IsExact = !IsWildcard;
    }

    /// <summary>
    /// The namespace pattern string.
    /// </summary>
    public string Pattern { get; }

    /// <summary>
    /// Indicates whether this pattern contains wildcards.
    /// </summary>
    public bool IsWildcard { get; }

    /// <summary>
    /// Indicates whether this is an exact namespace match.
    /// </summary>
    public bool IsExact { get; }

    /// <summary>
    /// Checks if a namespace matches this pattern.
    /// </summary>
    /// <param name="namespace">The namespace to check</param>
    /// <returns>True if the namespace matches the pattern</returns>
    public bool Matches(string @namespace)
    {
        if (string.IsNullOrWhiteSpace(@namespace))
            return false;

        if (IsExact)
        {
            return Pattern.Equals(@namespace, StringComparison.OrdinalIgnoreCase);
        }

        // Handle wildcard patterns
        if (Pattern.EndsWith('*'))
        {
            var prefix = Pattern[..^1]; // Remove the trailing *
            return @namespace.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
        }

        if (Pattern.StartsWith('*'))
        {
            var suffix = Pattern[1..]; // Remove the leading *
            return @namespace.EndsWith(suffix, StringComparison.OrdinalIgnoreCase);
        }

        if (Pattern.Contains('*'))
        {
            // More complex wildcard matching - convert to regex-like behavior
            var parts = Pattern.Split('*', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
                return true; // Pattern is just "*"

            var currentIndex = 0;
            foreach (var part in parts)
            {
                var index = @namespace.IndexOf(
                    part,
                    currentIndex,
                    StringComparison.OrdinalIgnoreCase
                );
                if (index == -1)
                    return false;
                currentIndex = index + part.Length;
            }

            return true;
        }

        return false;
    }

    /// <summary>
    /// Creates a namespace pattern for exact matching.
    /// </summary>
    /// <param name="exactNamespace">The exact namespace to match</param>
    /// <returns>NamespacePattern for exact matching</returns>
    public static NamespacePattern Exact(string exactNamespace) => new(exactNamespace);

    /// <summary>
    /// Creates a namespace pattern for prefix matching.
    /// </summary>
    /// <param name="prefix">The namespace prefix</param>
    /// <returns>NamespacePattern for prefix matching</returns>
    public static NamespacePattern Prefix(string prefix) => new($"{prefix}*");

    /// <summary>
    /// Creates a namespace pattern for suffix matching.
    /// </summary>
    /// <param name="suffix">The namespace suffix</param>
    /// <returns>NamespacePattern for suffix matching</returns>
    public static NamespacePattern Suffix(string suffix) => new($"*{suffix}");

    /// <summary>
    /// Creates a namespace pattern for wildcard matching.
    /// </summary>
    /// <param name="pattern">The wildcard pattern</param>
    /// <returns>NamespacePattern for wildcard matching</returns>
    public static NamespacePattern Wildcard(string pattern) => new(pattern);

    public bool Equals(NamespacePattern? other)
    {
        if (other is null)
            return false;

        if (ReferenceEquals(this, other))
            return true;

        return Pattern.Equals(other.Pattern, StringComparison.OrdinalIgnoreCase);
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as NamespacePattern);
    }

    public override int GetHashCode()
    {
        return Pattern.ToLowerInvariant().GetHashCode();
    }

    public override string ToString()
    {
        return Pattern;
    }

    public static bool operator ==(NamespacePattern? left, NamespacePattern? right)
    {
        return EqualityComparer<NamespacePattern>.Default.Equals(left, right);
    }

    public static bool operator !=(NamespacePattern? left, NamespacePattern? right)
    {
        return !(left == right);
    }

    public static implicit operator string(NamespacePattern pattern) => pattern.Pattern;

    public static implicit operator NamespacePattern(string pattern) => new(pattern);
}
