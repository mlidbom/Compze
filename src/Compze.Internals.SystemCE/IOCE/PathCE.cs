namespace Compze.Internals.SystemCE.IOCE;

static class PathCE
{
   public static string ReplaceInvalidCharactersWith(string path, char replacement) =>
      path._mutate(it => Path.GetInvalidFileNameChars()
                            .ForEach(invalidChar => it = it.Replace(invalidChar, replacement)));


}
