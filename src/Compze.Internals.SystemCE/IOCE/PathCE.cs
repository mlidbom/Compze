namespace Compze.Internals.SystemCE.IOCE;

public static class PathCE
{
   public static string ReplaceInvalidCharactersWith(string path, char replacement)
   {
      foreach(var invalidChar in Path.GetInvalidFileNameChars())
      {
         path = path.Replace(invalidChar, replacement);
      }

      return path;
   }
}
