using System.Text;
using Compze.Internals.SystemCE.IOCE;

namespace Compze.Internals.SystemCE.Tests.IOCE;

public class LinesWrittenAfterAPoint_ : InAFolderOfItsOwn
{
   readonly string _file;

   LinesWrittenAfterAPoint_() => _file = PathOf("lines.txt");

   void Write(string text) => File.WriteAllText(_file, text, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
   void Append(string text) => File.AppendAllText(_file, text);

   LinesWrittenAfterAPoint LinesAfter(long point) => new(_file, point);

   static List<string> TheRestOf(LinesWrittenAfterAPoint lines)
   {
      List<string> read = [];
      while(lines.ReadTheNext()) read.Add(Encoding.UTF8.GetString(lines.Line));
      return read;
   }

   public class FromStartOf : LinesWrittenAfterAPoint_
   {
      public class Given_lines_ending_in_LF_and_CRLF_and_a_last_line_without_a_line_break : FromStartOf
      {
         public Given_lines_ending_in_LF_and_CRLF_and_a_last_line_without_a_line_break() => Write("första\nsecond\r\n日本語\nstill being writ");

         [XF] public void they_are_the_complete_lines_without_their_line_breaks_in_their_written_order() =>
            LinesWrittenAfterAPoint.FromStartOf(_file).Must().SequenceEqual(["första", "second", "日本語"]);
      }

      public class Given_a_byte_order_mark_at_the_start_of_the_file : FromStartOf
      {
         public Given_a_byte_order_mark_at_the_start_of_the_file() => File.WriteAllText(_file, "first\nsecond\n", new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));

         [XF] public void it_is_not_part_of_the_first_line() => LinesWrittenAfterAPoint.FromStartOf(_file).Must().SequenceEqual(["first", "second"]);
      }

      public class When_lines_are_appended_while_the_lines_are_enumerated : FromStartOf
      {
         readonly List<string> _read = [];

         public When_lines_are_appended_while_the_lines_are_enumerated()
         {
            Write("first\nsecond\n");
            using var lines = LinesWrittenAfterAPoint.FromStartOf(_file).GetEnumerator();
            lines.MoveNext();
            _read.Add(lines.Current);
            Append("appended\n");
            while(lines.MoveNext()) _read.Add(lines.Current);
         }

         [XF] public void the_appended_lines_are_left_out() => _read.Must().SequenceEqual(["first", "second"]);
      }
   }

   public class When_the_lines_are_read_from_the_start_of_a_file_whose_last_line_is_still_being_written : LinesWrittenAfterAPoint_
   {
      readonly List<string> _readFirst;
      readonly long _endOfTheLinesReadFirst;

      public When_the_lines_are_read_from_the_start_of_a_file_whose_last_line_is_still_being_written()
      {
         Write("first\nsec");
         using var lines = LinesAfter(0);
         _readFirst = TheRestOf(lines);
         _endOfTheLinesReadFirst = lines.EndOfTheLinesRead;
      }

      [XF] public void only_the_complete_line_is_read() => _readFirst.Must().SequenceEqual(["first"]);
      [XF] public void the_end_of_the_lines_read_is_where_the_line_still_being_written_starts() => _endOfTheLinesReadFirst.Must().Be(6L);

      public class and_then_from_the_end_of_the_lines_read_once_more_is_written : When_the_lines_are_read_from_the_start_of_a_file_whose_last_line_is_still_being_written
      {
         readonly List<string> _readThen;
         readonly bool _pointIsPastTheEnd;

         public and_then_from_the_end_of_the_lines_read_once_more_is_written()
         {
            Append("ond\r\nthird\n");
            using var lines = LinesAfter(_endOfTheLinesReadFirst);
            _pointIsPastTheEnd = lines.PointIsPastTheEnd;
            _readThen = TheRestOf(lines);
         }

         [XF] public void the_rest_of_the_lines_are_read() => _readThen.Must().SequenceEqual(["second", "third"]);
         [XF] public void the_point_is_not_past_the_end() => _pointIsPastTheEnd.Must().BeFalse();
      }

      public class and_then_from_the_end_of_the_lines_read_after_the_file_is_written_anew_shorter : When_the_lines_are_read_from_the_start_of_a_file_whose_last_line_is_still_being_written
      {
         readonly bool _pointIsPastTheEnd;

         public and_then_from_the_end_of_the_lines_read_after_the_file_is_written_anew_shorter()
         {
            Write("new\n");
            using var lines = LinesAfter(_endOfTheLinesReadFirst);
            _pointIsPastTheEnd = lines.PointIsPastTheEnd;
         }

         [XF] public void the_point_is_past_the_end() => _pointIsPastTheEnd.Must().BeTrue();
      }
   }

   public class When_lines_are_appended_while_the_lines_are_read : LinesWrittenAfterAPoint_
   {
      readonly List<string> _read;

      public When_lines_are_appended_while_the_lines_are_read()
      {
         Write("first\n");
         using var lines = LinesAfter(0);
         _read = TheRestOf(lines);
         Append("appended\n");
         _read.AddRange(TheRestOf(lines));
      }

      [XF] public void the_appended_lines_are_read_too() => _read.Must().SequenceEqual(["first", "appended"]);
   }

   public class Given_a_line_longer_than_what_is_read_at_once : LinesWrittenAfterAPoint_
   {
      static readonly string LongLine = new('x', 3 * 1024 * 1024);

      public Given_a_line_longer_than_what_is_read_at_once() => Write($"before\n{LongLine}\nafter\n");

      [XF] public void it_is_read_whole_between_the_others() => LinesWrittenAfterAPoint.FromStartOf(_file).Must().SequenceEqual(["before", LongLine, "after"]);
   }
}
