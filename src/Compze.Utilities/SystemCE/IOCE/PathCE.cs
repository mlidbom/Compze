using Compze.Utilities.Functional;
using Compze.Utilities.SystemCE.LinqCE;
using System.IO;

namespace Compze.Utilities.SystemCE.IOCE;

public static class PathCE
{
   internal static string ReplaceInvalidCharactersWith(string path, char replacement) =>
      path.mutate(it => Path.GetInvalidFileNameChars()
                            .ForEach(invalidChar => it = it.Replace(invalidChar, replacement)));


}
