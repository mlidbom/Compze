namespace Compze.Internals.SystemCE.CollectionsCE.GenericCE;

public static class EnumerableCENotNull
{
   ///<summary>Returns a sequence of types matching the supplied type arguments</summary>
   public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T?> @this) => @this.OfType<T>();
}
