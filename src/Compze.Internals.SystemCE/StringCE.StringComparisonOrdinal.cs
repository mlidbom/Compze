namespace Compze.Internals.SystemCE;

///<summary>Contains extensions on <see cref="string"/></summary>
public static partial class StringCE
{
   extension(string @this)
   {
      public string ReplaceOrdinal(string oldValue, string newValue) => @this.Replace(oldValue, newValue, StringComparison.Ordinal);
      public bool ContainsOrdinal(string value) => @this.Contains(value, StringComparison.Ordinal);
      public int GetHashcodeOrdinal() => @this.GetHashCode(StringComparison.Ordinal);
      public bool StartsWithOrdinal(string ending) => @this.StartsWith(ending, StringComparison.Ordinal);
      public bool EndsWithOrdinal(string ending) => @this.EndsWith(ending, StringComparison.Ordinal);
      public int IndexOfOrdinal(char character) => @this.IndexOf(character, StringComparison.Ordinal);
   }
}
