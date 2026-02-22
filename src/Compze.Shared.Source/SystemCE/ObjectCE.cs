namespace Compze.Utilities.SystemCE;

internal static class ObjectCE
{
   ///<summary>Returns string.Empty if ToString() returns null.</summary>
   public static string ToStringCE(this object @this) => @this.ToString() ?? string.Empty;
}
