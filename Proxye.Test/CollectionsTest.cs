using Proxye.Collections;

namespace Proxye.Test;

public sealed class CollectionsTest
{
    [Theory]
    [InlineData("blabla.com")]
    [InlineData("any.com.xyz")]
    [InlineData("nothing.subdomain.xyz")]
    [InlineData("subdomain.xyz")]
    [InlineData(".com.xyz")]
    public void Find_Rule_Via_Domain_Tree(string host)
    {
        // Arrange
        var tree = GetTree(["com", "subdomain.xyz", "com.xyz"]);

        // Act
        var exist = tree.TryGetValue(host, out var result);

        // Assert
        Assert.True(exist);
        Assert.Equal("yes", result);
    }

    [Theory]
    [InlineData("blablacom")]
    [InlineData("anycom.xyz")]
    [InlineData("nothingsubdomain.xyz")]
    [InlineData("tsubdomain.xyz")]
    [InlineData("com.xyz.xyz")]
    public void Unable_To_Find_Rule_Via_Domain_Tree(string host)
    {
        // Arrange
        var tree = GetTree(["com", "subdomain.xyz", "com.xyz"]);

        // Act
        var exist = tree.TryGetValue(host, out var result);

        // Assert
        Assert.False(exist);
        Assert.Equal(null, result);
    }

    private static DomainTree<string> GetTree(string[] domains)
    {
        var tree = new DomainTree<string>();
        foreach (var domain in domains)
        {
            tree.Add(domain, "yes");
        }
        return tree;
    }
}