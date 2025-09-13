using System.Collections.Concurrent;

namespace Proxye.Rules.Collections;

internal sealed class BoundedDictionary<TKey, TValue>(int size) : ConcurrentDictionary<TKey, TValue> where TKey : notnull
{
    private readonly LinkedList<TKey> _keys = [];

    public void Add(TKey key, TValue value)
    {
        lock (_keys)
        {
            if (TryAdd(key, value))
            {
                _keys.AddLast(key);
            }
            if (_keys.Count > size && TryRemove(_keys.First!.Value, out _))
            {
                _keys.RemoveFirst();
            }
        }
    }
}