using System.Collections;
using System.Text;

namespace Compze.Internals.SystemCE.IOCE;

/// <summary>
/// The lines of a UTF-8 file that is appended to a line at a time, such as a log, from the last one backwards. Only lines
/// that end in a line break: a last line without one is still being written.
/// </summary>
/// <remarks>
/// Such a file can grow to tens of megabytes, and what is looked for is usually near its end, so the file is read a chunk
/// at a time from the end. Another process may write the file, or replace or delete it, while it is read.
/// </remarks>
/// <remarks>
/// A line ends in LF or in CRLF, and neither is part of the line. A byte order mark at the start of the file isn't part of
/// the first line.<br/>
/// A chunk's first bytes usually belong to a line that starts in the chunk before it, so they're carried over to that
/// chunk, as bytes: decoded apart, a character cut in two would be garbled.<br/>
/// Each enumeration reads the file as it was when the enumeration started: what is appended meanwhile is left out.
/// </remarks>
public sealed class LinesFromTheEnd : IEnumerable<string>
{
   readonly string _path;
   const int BytesInAChunk = 4 * 1024 * 1024;
   const byte LineBreak = (byte)'\n', CarriageReturn = (byte)'\r';
   static readonly byte[] ByteOrderMark = Encoding.UTF8.Preamble.ToArray();

   /// <summary>The lines of the file at <paramref name="path"/>, read anew each time they are enumerated.</summary>
   public LinesFromTheEnd(string path) => _path = path;

   public IEnumerator<string> GetEnumerator() =>
      //What follows the last line break is either nothing or a line that is still being written.
      PiecesBetweenLineBreaksFromTheEnd().Skip(1).GetEnumerator();

   IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

   IEnumerable<string> PiecesBetweenLineBreaksFromTheEnd()
   {
      using var file = FileReadLettingItBeReplaced.Open(_path);
      var end = file.Length;
      byte[] startOfTheChunkAfter = [];
      while(true)
      {
         var start = Math.Max(0, end - BytesInAChunk);
         byte[] bytes = [.. Read(file, start, end), .. startOfTheChunkAfter];
         if(start == 0)
         {
            foreach(var piece in PiecesBetweenLineBreaksIn(WithoutAByteOrderMark(bytes)).Reverse()) yield return piece;
            yield break;
         }

         var firstLineBreak = Array.IndexOf(bytes, LineBreak);
         if(firstLineBreak == -1)
         {
            startOfTheChunkAfter = bytes;
         } else
         {
            foreach(var piece in PiecesBetweenLineBreaksIn(bytes[(firstLineBreak + 1)..]).Reverse()) yield return piece;
            startOfTheChunkAfter = bytes[..firstLineBreak];
         }

         end = start;
      }
   }

   static byte[] Read(FileStream file, long start, long end)
   {
      var bytes = new byte[end - start];
      file.Position = start;
      file.ReadExactly(bytes);
      return bytes;
   }

   static byte[] WithoutAByteOrderMark(byte[] startOfTheFile) =>
      startOfTheFile.AsSpan().StartsWith(ByteOrderMark) ? startOfTheFile[ByteOrderMark.Length..] : startOfTheFile;

   static IEnumerable<string> PiecesBetweenLineBreaksIn(byte[] bytes)
   {
      var pieceStart = 0;
      for(var index = 0; index < bytes.Length; index++)
      {
         if(bytes[index] != LineBreak) continue;
         yield return TextWithoutACarriageReturnAtTheEnd(bytes, pieceStart, index);
         pieceStart = index + 1;
      }

      yield return TextWithoutACarriageReturnAtTheEnd(bytes, pieceStart, bytes.Length);
   }

   /// <summary>
   /// The text of <paramref name="bytes"/> from <paramref name="start"/> up to <paramref name="end"/>, without a carriage
   /// return at its end: the first half of a CRLF line break.
   /// </summary>
   static string TextWithoutACarriageReturnAtTheEnd(byte[] bytes, int start, int end)
   {
      if(end > start && bytes[end - 1] == CarriageReturn) end--;
      return Encoding.UTF8.GetString(bytes, start, end - start);
   }
}
