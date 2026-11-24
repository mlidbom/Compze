using System.Text;

namespace Compze.Internals.SystemCE.IOCE;

/// <summary>
/// Reads a file that another thread or process may replace while it is read, as <see cref="FileReplacement"/> replaces
/// it: opened so that it may be replaced, written or deleted meanwhile, and read on whole as it was when opened.
/// </summary>
/// <remarks>
/// <see cref="File.ReadAllText(string)"/>, <see cref="File.ReadLines(string)"/> and <see cref="File.OpenRead"/> open a file
/// letting others only read it: while one of them reads it, Windows refuses to replace or delete the file, with "Access to
/// the path is denied". These let others write it and delete it meanwhile, which replacing it takes.
/// </remarks>
/// <remarks>review-naming: the type <c>FileReadLettingItBeReplaced</c>.</remarks>
public static class FileReadLettingItBeReplaced
{
   /// <summary>What a reader of the file lets others do with it meanwhile: write it, and replace or delete it.</summary>
   const FileShare WhatOthersMayDoMeanwhile = FileShare.ReadWrite | FileShare.Delete;

   /// <summary>The file's whole text, as <see cref="File.ReadAllText(string)"/> reads it: UTF-8, unless a byte order mark says otherwise.</summary>
   public static string AllText(string path)
   {
      using var reader = new StreamReader(Open(path), Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
      return reader.ReadToEnd();
   }

   /// <summary>The file's lines, read as they are enumerated, as <see cref="File.ReadLines(string)"/> reads them.</summary>
   public static IEnumerable<string> Lines(string path)
   {
      using var reader = new StreamReader(Open(path), Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
      while(reader.ReadLine() is {} line) yield return line;
   }

   /// <summary>The file opened to read, as <see cref="File.OpenRead"/> opens it.</summary>
   public static FileStream Open(string path) => new(path, FileMode.Open, FileAccess.Read, WhatOthersMayDoMeanwhile);
}
