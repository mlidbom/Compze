using System.Diagnostics.CodeAnalysis;

namespace Compze.Internals.SystemCE;

static class TryGetToTuple
{
   internal delegate bool TryGetMethod<in TKey, TValue>(TKey key, [MaybeNullWhen(false)]out TValue value);

   internal static (bool Success, TValue? Value) Call<TKey, TValue>(TryGetMethod<TKey, TValue> tryGet, TKey key) =>
      tryGet(key, out var value) ? (true, value) : (false, default);
}
