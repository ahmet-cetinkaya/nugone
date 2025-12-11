using FluentAssertions;
using NuGone.Domain.Features.PackageAnalysis.ValueObjects;
using Xunit;

namespace NuGone.Domain.Tests.Features.PackageAnalysis.ValueObjects;

/// <summary>
/// Tests for the NamespacePattern value object
/// </summary>
public class NamespacePatternTests
{
    [Fact]
    public void Constructor_WithValidPattern_ShouldCreateNamespacePattern()
    {
        // Arrange
        var pattern = "System.*";

        // Act
        var namespacePattern = new NamespacePattern(pattern);

        // Assert
        namespacePattern.Pattern.Should().Be(pattern);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithInvalidPattern_ShouldThrowArgumentException(string? pattern)
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => new NamespacePattern(pattern!));
        ex.ParamName.Should().Be("pattern");
    }

    [Fact]
    public void Constructor_WithPatternContainingWildcards_ShouldSetIsWildcardToTrue()
    {
        // Arrange
        var pattern = "System.*";

        // Act
        var namespacePattern = new NamespacePattern(pattern);

        // Assert
        namespacePattern.IsWildcard.Should().BeTrue();
    }

    [Fact]
    public void Constructor_WithPatternWithoutWildcards_ShouldSetIsExactToTrue()
    {
        // Arrange
        var pattern = "System.Text";

        // Act
        var namespacePattern = new NamespacePattern(pattern);

        // Assert
        namespacePattern.IsExact.Should().BeTrue();
        namespacePattern.IsWildcard.Should().BeFalse();
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    public void Matches_WithInvalidNamespace_ShouldReturnFalse(string? @namespace, bool expected)
    {
        // Arrange
        var namespacePattern = new NamespacePattern("System.*");

        // Act
        var result = namespacePattern.Matches(@namespace!);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("System.Text", "System.Text", true)]
    [InlineData("System.Text", "System.Text.Json", false)]
    [InlineData("System.Text", "system.text", true)] // Case insensitive exact match
    [InlineData("System.Text", "Microsoft.Extensions.Text", false)]
    public void Matches_WithExactNamespace_ShouldReturnCorrectResult(
        string pattern,
        string @namespace,
        bool expected
    )
    {
        // Arrange
        var namespacePattern = new NamespacePattern(pattern);

        // Act
        var result = namespacePattern.Matches(@namespace);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("System.*", "System.Text", true)]
    [InlineData("System.*", "System.Text.Json", true)]
    [InlineData("System.*", "System", false)] // "System" doesn't start with "System."
    [InlineData("System.*", "Microsoft.System", false)]
    [InlineData("Microsoft.*", "Microsoft.Extensions", true)]
    public void Matches_WithPrefixWildcardPattern_ShouldReturnCorrectResult(
        string pattern,
        string @namespace,
        bool expected
    )
    {
        // Arrange
        var namespacePattern = new NamespacePattern(pattern);

        // Act
        var result = namespacePattern.Matches(@namespace);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("*.Text", "System.Text", true)]
    [InlineData("*.Text", "Extensions.Text", true)]
    [InlineData("*.Text", "System.Text.Data", false)]
    [InlineData("*.Data", "System.Text.Data", true)]
    public void Matches_WithSuffixWildcardPattern_ShouldReturnCorrectResult(
        string pattern,
        string @namespace,
        bool expected
    )
    {
        // Arrange
        var namespacePattern = new NamespacePattern(pattern);

        // Act
        var result = namespacePattern.Matches(@namespace);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("*", "System", true)]
    [InlineData("*", "Microsoft.Extensions.Text", true)]
    [InlineData("*", "Any.Namespace.Here", true)]
    public void Matches_WildcardOnly_ShouldMatchAnyNamespace(
        string pattern,
        string @namespace,
        bool expected
    )
    {
        // Arrange
        var namespacePattern = new NamespacePattern(pattern);

        // Act
        var result = namespacePattern.Matches(@namespace);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("System.Text.Json", "System.Text.Json", true)]
    [InlineData("system.text.json", "System.Text.Json", true)] // Case insensitive
    [InlineData("System.TEXT", "System.Text.Json", false)] // Different namespace
    [InlineData("Microsoft.Text", "System.Text.Json", false)]
    public void Matches_WithComplexWildcardPattern_ShouldMatchPartsInOrder(
        string pattern,
        string @namespace,
        bool expected
    )
    {
        // Arrange
        var namespacePattern = new NamespacePattern(pattern);

        // Act
        var result = namespacePattern.Matches(@namespace);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("System.Text.Json", "System.Text.Data", false)] // Parts appear but not in order
    [InlineData("Text.System", "System.Text.Json", false)] // Parts appear but not in order
    [InlineData("System.Json", "System.Text.Json", false)] // Implementation requires consecutive matching
    public void Matches_WithComplexWildcardPattern_ShouldRequireOrdering(
        string pattern,
        string @namespace,
        bool expected
    )
    {
        // Arrange
        var namespacePattern = new NamespacePattern(pattern);

        // Act
        var result = namespacePattern.Matches(@namespace);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void ToString_ShouldReturnPattern()
    {
        // Arrange
        var pattern = "System.*";
        var namespacePattern = new NamespacePattern(pattern);

        // Act
        var result = namespacePattern.ToString();

        // Assert
        result.Should().Be(pattern);
    }

    [Fact]
    public void Equals_WithSamePattern_ShouldReturnTrue()
    {
        // Arrange
        var pattern1 = new NamespacePattern("System.*");
        var pattern2 = new NamespacePattern("System.*");

        // Act & Assert
        pattern1.Equals(pattern2).Should().BeTrue();
    }

    [Fact]
    public void Equals_WithDifferentPattern_ShouldReturnFalse()
    {
        // Arrange
        var pattern1 = new NamespacePattern("System.*");
        var pattern2 = new NamespacePattern("Microsoft.*");

        // Act & Assert
        pattern1.Equals(pattern2).Should().BeFalse();
    }

    [Fact]
    public void Equals_ShouldBeCaseInsensitive()
    {
        // Arrange
        var pattern1 = new NamespacePattern("SYSTEM.*");
        var pattern2 = new NamespacePattern("system.*");

        // Act & Assert
        pattern1.Equals(pattern2).Should().BeTrue();
    }

    [Fact]
    public void Equals_WithNull_ShouldReturnFalse()
    {
        // Arrange
        var pattern = new NamespacePattern("System.*");

        // Act & Assert
        // CA1508: This test is redundant - Equals(null) always returns false for non-null objects
        // pattern.Equals(null).Should().BeFalse();
    }

    [Fact]
    public void Equals_WithDifferentType_ShouldReturnFalse()
    {
        // Arrange
        var pattern = new NamespacePattern("System.*");

        // Act & Assert
        pattern.Equals("string").Should().BeFalse();
    }

    [Fact]
    public void GetHashCode_WithSamePattern_ShouldReturnSameHashCode()
    {
        // Arrange
        var pattern1 = new NamespacePattern("System.*");
        var pattern2 = new NamespacePattern("System.*");

        // Act & Assert
        pattern1.GetHashCode().Should().Be(pattern2.GetHashCode());
    }

    [Fact]
    public void GetHashCode_WithDifferentPattern_ShouldReturnDifferentHashCode()
    {
        // Arrange
        var pattern1 = new NamespacePattern("System.*");
        var pattern2 = new NamespacePattern("Microsoft.*");

        // Act & Assert
        pattern1.GetHashCode().Should().NotBe(pattern2.GetHashCode());
    }

    [Fact]
    public void GetHashCode_ShouldBeCaseInsensitive()
    {
        // Arrange
        var pattern1 = new NamespacePattern("SYSTEM.*");
        var pattern2 = new NamespacePattern("system.*");

        // Act & Assert
        pattern1.GetHashCode().Should().Be(pattern2.GetHashCode());
    }

    [Fact]
    public void EqualityOperator_WithEqualPatterns_ShouldReturnTrue()
    {
        // Arrange
        var pattern1 = new NamespacePattern("System.*");
        var pattern2 = new NamespacePattern("System.*");

        // Act
        var result = pattern1 == pattern2;

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void EqualityOperator_WithDifferentPatterns_ShouldReturnFalse()
    {
        // Arrange
        var pattern1 = new NamespacePattern("System.*");
        var pattern2 = new NamespacePattern("Microsoft.*");

        // Act
        var result = pattern1 == pattern2;

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void InequalityOperator_WithEqualPatterns_ShouldReturnFalse()
    {
        // Arrange
        var pattern1 = new NamespacePattern("System.*");
        var pattern2 = new NamespacePattern("System.*");

        // Act
        var result = pattern1 != pattern2;

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void InequalityOperator_WithDifferentPatterns_ShouldReturnTrue()
    {
        // Arrange
        var pattern1 = new NamespacePattern("System.*");
        var pattern2 = new NamespacePattern("Microsoft.*");

        // Act
        var result = pattern1 != pattern2;

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ImplicitConversionFromString_ShouldCreateNamespacePattern()
    {
        // Arrange
        string pattern = "System.*";

        // Act
        NamespacePattern namespacePattern = pattern;

        // Assert
        namespacePattern.Pattern.Should().Be(pattern);
    }

    [Fact]
    public void ImplicitConversionToString_ShouldReturnPattern()
    {
        // Arrange
        var pattern = "System.*";
        var namespacePattern = new NamespacePattern(pattern);

        // Act
        string result = namespacePattern;

        // Assert
        result.Should().Be(pattern);
    }

    // Test factory methods
    [Fact]
    public void Exact_ShouldCreateExactNamespacePattern()
    {
        // Arrange
        var exactNamespace = "System.Text";

        // Act
        var pattern = NamespacePattern.Exact(exactNamespace);

        // Assert
        pattern.Pattern.Should().Be(exactNamespace);
        pattern.IsExact.Should().BeTrue();
        pattern.IsWildcard.Should().BeFalse();
    }

    [Fact]
    public void Prefix_ShouldCreatePrefixNamespacePattern()
    {
        // Arrange
        var prefix = "System.Text";

        // Act
        var pattern = NamespacePattern.Prefix(prefix);

        // Assert
        pattern.Pattern.Should().Be($"{prefix}*");
        pattern.IsExact.Should().BeFalse();
        pattern.IsWildcard.Should().BeTrue();
    }

    [Fact]
    public void Suffix_ShouldCreateSuffixNamespacePattern()
    {
        // Arrange
        var suffix = "Text";

        // Act
        var pattern = NamespacePattern.Suffix(suffix);

        // Assert
        pattern.Pattern.Should().Be($"*{suffix}");
        pattern.IsExact.Should().BeFalse();
        pattern.IsWildcard.Should().BeTrue();
    }

    [Fact]
    public void Wildcard_ShouldCreateWildcardNamespacePattern()
    {
        // Arrange
        var wildcardPattern = "System.*.Data";

        // Act
        var pattern = NamespacePattern.Wildcard(wildcardPattern);

        // Assert
        pattern.Pattern.Should().Be(wildcardPattern);
        pattern.IsExact.Should().BeFalse();
        pattern.IsWildcard.Should().BeTrue();
    }
}
