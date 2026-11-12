namespace Compze.Must._private;

/// <summary>
/// Keeps failure messages readable, and small enough for test runners to carry: text longer than
/// <see cref="MustSerialization.LongestTextShownInFull"/> is written to a file, and the message shows its start and the file's path.
/// </summary>
/// <remarks>
/// Each test process writes to a folder of its own, <c>MustOutput/{start time}-{process id}</c>, under the test assembly's output folder.
/// Nothing deletes the files: cleaning the build output does.
/// </remarks>
static class LongTextFiles
{
   const int CharactersShownOfALongText = 5_000;

   static readonly string Folder = Path.Combine(AppContext.BaseDirectory, "MustOutput", $"{DateTime.Now:yyyyMMdd-HHmmss}-{Environment.ProcessId}");
   static int _filesWritten;

   /// <summary><paramref name="text"/> itself when it is short enough to show in full, otherwise its start and the path of the file holding all of it.</summary>
   /// <param name="text">The text to show.</param>
   /// <param name="fileName">What the file is named after, such as the expression whose value the text is; made safe for a file name.</param>
   /// <param name="extension">The file's extension, such as <c>json</c>.</param>
   public static string ShownInAMessage(this string text, string fileName, string extension)
   {
      if(text.Length <= MustSerialization.LongestTextShownInFull)
         return text;

      var path = WriteToAFile(text, fileName, extension);
      return $"""
              {StartOf(text)}
              ... shortened: all {text.Length} characters are in the file:
              {path}
              """;
   }

   static string WriteToAFile(string text, string fileName, string extension)
   {
      Directory.CreateDirectory(Folder);
      var path = Path.Combine(Folder, $"{Interlocked.Increment(ref _filesWritten):D3}-{Sanitized(fileName)}.{extension}");
      File.WriteAllText(path, text);
      return path;
   }

   /// <summary>The start of <paramref name="text"/>, ending at a line break where there is one.</summary>
   static string StartOf(string text)
   {
      var start = text[..Math.Min(CharactersShownOfALongText, MustSerialization.LongestTextShownInFull)];
      var lastLineBreak = start.LastIndexOf('\n');
      return lastLineBreak > 0 ? start[..lastLineBreak].TrimEnd('\r') : start;
   }

   static string Sanitized(string fileName)
   {
      var invalidCharacters = Path.GetInvalidFileNameChars();
      var sanitized = new string(fileName.Select(it => invalidCharacters.Contains(it) || char.IsWhiteSpace(it) ? '_' : it).ToArray());
      return sanitized.Length <= 100 ? sanitized : sanitized[..100];
   }
}
