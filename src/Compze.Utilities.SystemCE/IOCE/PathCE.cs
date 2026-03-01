using Compze.Underscore;
using Compze.Utilities.SystemCE.LinqCE;
using System.IO;

namespace Compze.Utilities.SystemCE.IOCE;

internal static class PathCE
{
   public static string ReplaceInvalidCharactersWith(string path, char replacement) =>
      path._mutate(it => Path.GetInvalidFileNameChars()
                            .ForEach(invalidChar => it = it.Replace(invalidChar, replacement)));


}
