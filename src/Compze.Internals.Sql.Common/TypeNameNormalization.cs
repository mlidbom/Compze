using System.Text;

namespace Compze.Internals.Sql.Common;

/// <summary>
/// Produces the stable, human-readable fully-qualified name recorded in the type-name history. It is the
/// type's <c>AssemblyQualifiedName</c> with the <c>Version</c>/<c>Culture</c>/<c>PublicKeyToken</c> qualifiers
/// stripped — at every nesting level — so the name does not appear to "change" on a routine assembly-version
/// bump and only a real rename produces a new history entry.
/// </summary>
static class TypeNameNormalization
{
   static readonly string[] Qualifiers = [", Version=", ", Culture=", ", PublicKeyToken="];

   public static string StripAssemblyQualifiers(string assemblyQualifiedName)
   {
      var result = new StringBuilder(assemblyQualifiedName.Length);
      var index = 0;
      while(index < assemblyQualifiedName.Length)
      {
         if(TryMatchQualifier(assemblyQualifiedName, index, out var qualifierLength))
         {
            index += qualifierLength;
            // Skip the qualifier's value, stopping at the delimiter that begins the next token. The value
            // itself never contains these, so the delimiter is the first ',' ']' or '[' we reach.
            while(index < assemblyQualifiedName.Length && assemblyQualifiedName[index] is not (',' or ']' or '['))
               index++;
         }
         else
         {
            result.Append(assemblyQualifiedName[index]);
            index++;
         }
      }

      return result.ToString();
   }

   static bool TryMatchQualifier(string text, int index, out int length)
   {
      foreach(var qualifier in Qualifiers)
         if(index + qualifier.Length <= text.Length
         && string.CompareOrdinal(text, index, qualifier, 0, qualifier.Length) == 0)
         {
            length = qualifier.Length;
            return true;
         }

      length = 0;
      return false;
   }
}
