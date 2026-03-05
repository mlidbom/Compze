namespace Compze.Internals.SystemCE.Core.CollectionsCE.GenericCE;

#pragma warning disable CA1002 // Utility extension methods returning List by design for copy-and-add pattern
static class ReadonlyCollectionsCE
{
   public static IReadOnlyList<T> AddToCopy<T>(this IReadOnlyList<T> @this, T item) => [..@this, item];
}
