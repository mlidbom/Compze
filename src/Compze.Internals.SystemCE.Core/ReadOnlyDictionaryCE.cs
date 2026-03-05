namespace Compze.Internals.SystemCE.Core;

static class ReadOnlyDictionaryCE
{
   internal static (bool Success, TValue? Value) TryGetTuple<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> me, TKey key) =>
      TryGetToTuple.Call<TKey, TValue>(me.TryGetValue, key);
}
