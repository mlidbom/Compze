using System.Collections.Generic;

namespace Compze.Utilities.SystemCE.CollectionsCE.GenericCE;

#pragma warning disable CA1002 // Utility extension methods returning List by design for copy-and-add pattern
public static class ReadonlyCollectionsCE
{
   public static List<T> AddToCopy<T>(this IReadOnlyList<T> @this, T item) => [..@this, item];
}
