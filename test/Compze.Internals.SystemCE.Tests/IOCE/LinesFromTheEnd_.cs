using System.Text;
using Compze.Internals.SystemCE.IOCE;

namespace Compze.Internals.SystemCE.Tests.IOCE;

public class LinesFromTheEnd_ : InAFolderOfItsOwn
{
   readonly string _file;

   LinesFromTheEnd_() => _file = PathOf("lines.txt");

   List<string> Lines => new LinesFromTheEnd(_file).ToList();

   void Write(string text) => File.WriteAllText(_file, text, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

   public class Given_lines_ending_in_LF_and_CRLF_and_a_last_line_without_a_line_break : LinesFromTheEnd_
   {
      public Given_lines_ending_in_LF_and_CRLF_and_a_last_line_without_a_line_break() => Write("första\nsecond\r\n日本語\nstill being writ");

      [XF] public void they_are_the_complete_lines_without_their_line_breaks_last_first() => Lines.Must().SequenceEqual(["日本語", "second", "första"]);
   }

   public class Given_a_byte_order_mark_at_the_start_of_the_file : LinesFromTheEnd_
   {
      public Given_a_byte_order_mark_at_the_start_of_the_file() => File.WriteAllText(_file, "first\nsecond\n", new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));

      [XF] public void it_is_not_part_of_the_first_line() => Lines.Must().SequenceEqual(["second", "first"]);
   }

   public class Given_an_empty_file : LinesFromTheEnd_
   {
      public Given_an_empty_file() => Write("");

      [XF] public void there_are_none() => Lines.Must().BeEmpty();
   }

   public class Given_only_a_line_without_a_line_break : LinesFromTheEnd_
   {
      public Given_only_a_line_without_a_line_break() => Write("still being writ");

      [XF] public void there_are_none() => Lines.Must().BeEmpty();
   }

   /// <summary>
   /// The file is read a chunk at a time from the end. Lines of three-byte characters, ended by a line of one to three
   /// ASCII characters, put a chunk's start inside a character in at least one of the three files.
   /// </summary>
   public class Given_files_of_several_megabytes_of_three_byte_characters : LinesFromTheEnd_
   {
      static readonly List<string> LinesOfThreeByteCharacters =
         [.. 1.Through(12_000).Select(number => string.Concat(Enumerable.Repeat("日本語", 30 + number % 37)))];

      readonly List<(long Bytes, List<string> Written, List<string> Read)> _files = [];

      public Given_files_of_several_megabytes_of_three_byte_characters()
      {
         foreach(var lastLine in new[] { "a", "ab", "abc" })
         {
            List<string> written = [.. LinesOfThreeByteCharacters, lastLine];
            Write(string.Concat(written.Select(it => it + "\n")));
            _files.Add((new FileInfo(_file).Length, written, Lines));
         }
      }

      [XF] public void each_file_is_larger_than_four_megabytes() =>
         _files.Select(it => it.Bytes).Must().Satisfy(sizes => sizes.All(size => size > 4 * 1024 * 1024));

      [XF] public void every_line_of_each_is_read_whole_last_first() =>
         _files.Select(it => new { LinesRead = it.Read.Count, LinesReadWrong = it.Read.Zip(Enumerable.Reverse(it.Written)).Count(pair => pair.First != pair.Second) })
               .Must().SequenceEqual(_files.Select(it => new { LinesRead = it.Written.Count, LinesReadWrong = 0 }));
   }

   /// <summary>
   /// The file is read a chunk of four megabytes at a time from the end. A line of four megabytes, less some bytes, after the
   /// lines puts the start of the last chunk at each byte of the lines in turn: inside a three-byte character, between a
   /// CR and its LF, and at each end of an empty line.
   /// </summary>
   public class Given_files_whose_last_chunk_starts_at_each_byte_of_lines_holding_empty_lines_CRLF_and_three_byte_characters : LinesFromTheEnd_
   {
      const string LinesTheChunksStartIn = "first\n\n\r\n日本語\r\n\nlast\n";
      const int BytesInAChunk = 4 * 1024 * 1024;
      static readonly List<string> LinesOfThemLastFirst = ["last", "", "日本語", "", "", "first"];

      readonly List<int> _chunkStartsThatALineWasReadWrongWith = [];

      public Given_files_whose_last_chunk_starts_at_each_byte_of_lines_holding_empty_lines_CRLF_and_three_byte_characters()
      {
         var bytesOfTheLines = Encoding.UTF8.GetByteCount(LinesTheChunksStartIn);
         foreach(var chunkStart in 0.Through(bytesOfTheLines))
         {
            var lastLine = new string('x', BytesInAChunk - (bytesOfTheLines - chunkStart) - 1);
            Write(LinesTheChunksStartIn + lastLine + "\n");
            if(!Lines.SequenceEqual([lastLine, .. LinesOfThemLastFirst])) _chunkStartsThatALineWasReadWrongWith.Add(chunkStart);
         }
      }

      [XF] public void every_line_of_each_is_read_whole_last_first() => _chunkStartsThatALineWasReadWrongWith.Must().BeEmpty();
   }

   public class Given_a_line_longer_than_four_megabytes : LinesFromTheEnd_
   {
      static readonly string LongLine = new('x', 5 * 1024 * 1024);

      public Given_a_line_longer_than_four_megabytes() => Write($"before\n{LongLine}\nafter\n");

      [XF] public void it_is_read_whole_between_the_others() => Lines.Must().SequenceEqual(["after", LongLine, "before"]);
   }

   public class When_lines_are_appended_while_the_lines_are_enumerated : LinesFromTheEnd_
   {
      readonly List<string> _read = [];

      public When_lines_are_appended_while_the_lines_are_enumerated()
      {
         Write("first\nsecond\n");
         using var lines = new LinesFromTheEnd(_file).GetEnumerator();
         lines.MoveNext();
         _read.Add(lines.Current);
         File.AppendAllText(_file, "appended\n");
         while(lines.MoveNext()) _read.Add(lines.Current);
      }

      [XF] public void the_appended_lines_are_left_out() => _read.Must().SequenceEqual(["second", "first"]);
   }
}
