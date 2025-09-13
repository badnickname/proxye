using System.Diagnostics.CodeAnalysis;

namespace Proxye.Rules.Collections;

internal sealed class DomainTree<T>
{
    private readonly Node<T> _root = new() { Key = '.' };

    public void Add(string host, T rule)
    {
        var reversed = host.Reverse().ToArray().AsSpan();
        Add(_root, reversed[0], reversed[1..], rule);
    }

    public bool TryGetValue(string host, [NotNullWhen(true)] out T? rule)
    {
        rule = Iterate(_root, host.AsSpan());
        return rule is not null;
    }

    private static T? Iterate(Node<T> node, ReadOnlySpan<char> buffer)
    {
        if (node.Value is not null && (buffer.IsEmpty || buffer[^1] == '.'))
        {
            return node.Value;
        }

        var value = buffer[^1];
        var child = node.Children.FirstOrDefault(x => x.Key == value);
        if (child is null)
        {
            return default;
        }

        return Iterate(child, buffer[..^1]);
    }

    private static void Add(Node<T> node, char key, Span<char> buffer, T? value)
    {
        foreach (var child in node.Children)
        {
            if (child.Key == key)
            {
                if (buffer.Length > 0)
                {
                    Add(child, buffer[0], buffer[1..], value);
                }
                return;
            }
        }

        var insert = new Node<T> { Key = key, Value = buffer.Length > 0 ? default : value };
        node.Children.Add(insert);
        if (buffer.Length > 0)
        {
            Add(insert, buffer[0], buffer[1..], value);
        }
    }
}

internal sealed class Node<T>
{
    public List<Node<T>> Children { get; } = [];

    public required char Key { get; init; }

    public T? Value { get; init; }
}