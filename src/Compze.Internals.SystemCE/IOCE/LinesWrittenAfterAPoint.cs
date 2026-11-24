using System.Buffers;
using System.Text;

namespace Compze.Internals.SystemCE.IOCE;

/// <summary>
/// The lines of a file that is appended to a line at a time, such as a log, written after a point in it: read one at a
/// time, each as its UTF-8 bytes, without the line break. Only lines that end in a line break: a last line without one is
/// still being written, and is left for a later reading, from <see cref="EndOfTheLinesRead"/>.
/// </summary>
/// <remarks>
/// Such a file can grow to hundreds of megabytes, nearly all of it lines a reader skips after their first few bytes, so the
/// lines are handed over as bytes in a buffer of the reader's own, rather than decoded into a string each.<br/>
/// The buffer grows to hold the longest line read.
/// </remarks>
/// <remarks>
/// A line ends in LF or in CRLF, and neither is part of the line. A byte order mark at the start of the file isn't part of
/// the first line.
/// </remarks>
public sealed class LinesWrittenAfterAPoint : IDisposable
{
   const int BytesReadAtOnce = 1024 * 1024;
   const byte LineBreak = (byte)'\n', CarriageReturn = (byte)'\r';
   static readonly byte[] ByteOrderMark = Encoding.UTF8.Preamble.ToArray();

   readonly FileStream _file;
   readonly long _end;
   byte[] _buffer = ArrayPool<byte>.Shared.Rent(BytesReadAtOnce);

   /// <summary>Where in the buffer the line not yet handed over begins, and how much of the buffer holds what was read.</summary>
   int _startOfTheRest, _bytesInTheBuffer;

   int _startOfTheLine, _lengthOfTheLine;

   /// <summary>Where in the file the buffer's first byte is.</summary>
   long _positionOfTheBuffer;

   /// <summary>
   /// The lines of the file at <paramref name="path"/> that begin at <paramref name="point"/> or after it, including those
   /// appended while they are read. The file is open until this is disposed, as
   /// <see cref="FileReadLettingItBeReplaced.Open"/> opens it: others may write it, and replace or delete it, meanwhile.
   /// </summary>
   public LinesWrittenAfterAPoint(string path, long point) : this(path, point, readUpToTheLengthWhenOpened: false) {}

   LinesWrittenAfterAPoint(string path, long point, bool readUpToTheLengthWhenOpened)
   {
      _file = FileReadLettingItBeReplaced.Open(path);
      _end = readUpToTheLengthWhenOpened ? _file.Length : long.MaxValue;
      PointIsPastTheEnd = point > _file.Length;
      _file.Position = point;
      _positionOfTheBuffer = point;
   }

   /// <summary>The file's complete lines in their written order, decoded from UTF-8, up to its length when this reading starts.</summary>
   public static IEnumerable<string> FromStartOf(string path)
   {
      using var lines = new LinesWrittenAfterAPoint(path, 0, readUpToTheLengthWhenOpened: true);
      while(lines.ReadTheNext()) yield return Encoding.UTF8.GetString(lines.Line);
   }

   /// <summary>
   /// Whether the file ends before the point the lines were to be read from: it was written anew, shorter, since that point
   /// was read up to. Read so from the file's open handle, which knows how long the file is while another process writes
   /// to it.
   /// </summary>
   public bool PointIsPastTheEnd { get; }

   /// <summary>The line read last, valid until the next is read.</summary>
   public ReadOnlySpan<byte> Line => _buffer.AsSpan(_startOfTheLine, _lengthOfTheLine);

   /// <summary>Where in the file the line after the last one read begins: where a later reading starts.</summary>
   public long EndOfTheLinesRead => _positionOfTheBuffer + _startOfTheRest;

   /// <summary>
   /// Reads the next line into <see cref="Line"/>: false when no complete line is left. A later call reads on, from the
   /// lines appended since.
   /// </summary>
   public bool ReadTheNext()
   {
      while(true)
      {
         var lineBreak = _buffer.AsSpan(_startOfTheRest, _bytesInTheBuffer - _startOfTheRest).IndexOf(LineBreak);
         if(lineBreak >= 0)
         {
            TakeTheLine(lineBreak);
            return true;
         }

         if(!ReadMore()) return false;
      }
   }

   /// <summary>
   /// Makes the line before the line break, <paramref name="lineBreak"/> bytes after <see cref="_startOfTheRest"/>,
   /// <see cref="Line"/>: without the carriage return of a CRLF line break, nor the byte order mark that may start the file.
   /// </summary>
   void TakeTheLine(int lineBreak)
   {
      _startOfTheLine = _startOfTheRest;
      _lengthOfTheLine = lineBreak;
      _startOfTheRest += lineBreak + 1;
      if(_lengthOfTheLine > 0 && _buffer[_startOfTheLine + _lengthOfTheLine - 1] == CarriageReturn) _lengthOfTheLine--;
      if(LineIsTheFirstInTheFile && Line.StartsWith(ByteOrderMark))
      {
         _startOfTheLine += ByteOrderMark.Length;
         _lengthOfTheLine -= ByteOrderMark.Length;
      }
   }

   bool LineIsTheFirstInTheFile => _positionOfTheBuffer + _startOfTheLine == 0;

   /// <summary>Reads what follows in the file into the buffer: false when nothing follows.</summary>
   bool ReadMore()
   {
      MoveTheRestToTheStart();
      if(_bytesInTheBuffer == _buffer.Length) GrowTheBuffer();
      var count = (int)Math.Min(_buffer.Length - _bytesInTheBuffer, _end - _file.Position);
      var read = _file.Read(_buffer, _bytesInTheBuffer, count);
      _bytesInTheBuffer += read;
      return read > 0;
   }

   void MoveTheRestToTheStart()
   {
      var rest = _bytesInTheBuffer - _startOfTheRest;
      _buffer.AsSpan(_startOfTheRest, rest).CopyTo(_buffer);
      _positionOfTheBuffer += _startOfTheRest;
      _startOfTheRest = 0;
      _bytesInTheBuffer = rest;
   }

   void GrowTheBuffer()
   {
      var larger = ArrayPool<byte>.Shared.Rent(_buffer.Length * 2);
      _buffer.AsSpan(0, _bytesInTheBuffer).CopyTo(larger);
      ArrayPool<byte>.Shared.Return(_buffer);
      _buffer = larger;
   }

   public void Dispose()
   {
      ArrayPool<byte>.Shared.Return(_buffer);
      _file.Dispose();
   }
}
