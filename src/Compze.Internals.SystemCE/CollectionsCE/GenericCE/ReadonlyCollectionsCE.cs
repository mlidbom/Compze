namespace Compze.Internals.SystemCE.CollectionsCE.GenericCE;

public static class ReadonlyCollectionsCE
{
   public static Dictionary<TKey, TValue> AddToCopy<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> @this, TKey key, TValue value) where TKey : notnull => new(@this) { { key, value } };

   ///<summary>Like <see cref="AddToCopy{TKey,TValue}"/> but overwrites: the copy holds <paramref name="value"/> under <paramref name="key"/> whether or not the key was already present.</summary>
   public static Dictionary<TKey, TValue> SetInCopy<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> @this, TKey key, TValue value) where TKey : notnull => new(@this) { [key] = value };

   ///<summary>The removal counterpart of <see cref="SetInCopy{TKey,TValue}"/>: a copy without <paramref name="key"/> — identical to the original when the key was absent.</summary>
   public static Dictionary<TKey, TValue> RemoveFromCopy<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> @this, TKey key) where TKey : notnull => new Dictionary<TKey, TValue>(@this)._mutate(me => me.Remove(key));

   public static HashSet<T> AddToCopy<T>(this IReadOnlySet<T> @this, T item) => [..@this, item];
}
