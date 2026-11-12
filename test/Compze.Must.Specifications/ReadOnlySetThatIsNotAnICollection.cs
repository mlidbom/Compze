using System.Collections;

namespace Compze.Must.Specifications;

/// <summary>
/// A read-only set that is not an <see cref="ICollection{T}"/>: <see cref="Enumerable.Contains{TSource}(IEnumerable{TSource}, TSource)"/> asks<br/>
/// an <see cref="ICollection{T}"/> itself whether it contains an item, so only a set like this one shows whether its own comparer decides.
/// </summary>
sealed class ReadOnlySetThatIsNotAnICollection : IReadOnlySet<string>
{
   readonly HashSet<string> _set;

   public ReadOnlySetThatIsNotAnICollection(HashSet<string> set) => _set = set;

   public int Count => _set.Count;
   public bool Contains(string item) => _set.Contains(item);
   public bool IsProperSubsetOf(IEnumerable<string> other) => _set.IsProperSubsetOf(other);
   public bool IsProperSupersetOf(IEnumerable<string> other) => _set.IsProperSupersetOf(other);
   public bool IsSubsetOf(IEnumerable<string> other) => _set.IsSubsetOf(other);
   public bool IsSupersetOf(IEnumerable<string> other) => _set.IsSupersetOf(other);
   public bool Overlaps(IEnumerable<string> other) => _set.Overlaps(other);
   public bool SetEquals(IEnumerable<string> other) => _set.SetEquals(other);
   public IEnumerator<string> GetEnumerator() => _set.GetEnumerator();
   IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
