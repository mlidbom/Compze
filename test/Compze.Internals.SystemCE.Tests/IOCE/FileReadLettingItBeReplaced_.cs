using System.Text;
using Compze.Internals.SystemCE.IOCE;

namespace Compze.Internals.SystemCE.Tests.IOCE;

public class FileReadLettingItBeReplaced_ : InAFolderOfItsOwn
{
   readonly string _file;

   FileReadLettingItBeReplaced_() => _file = PathOf("file.txt");

   public class Given_a_file_of_UTF_8_lines_ending_in_LF_and_CRLF : FileReadLettingItBeReplaced_
   {
      const string Text = "första\nsecond\r\n日本語";

      public Given_a_file_of_UTF_8_lines_ending_in_LF_and_CRLF() => File.WriteAllText(_file, Text, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

      [XF] public void AllText_is_the_whole_text() => FileReadLettingItBeReplaced.AllText(_file).Must().Be(Text);
      [XF] public void Lines_are_the_lines_without_their_line_breaks() => FileReadLettingItBeReplaced.Lines(_file).Must().SequenceEqual(["första", "second", "日本語"]);
   }

   public class Given_a_file_written_in_UTF_16_with_a_byte_order_mark : FileReadLettingItBeReplaced_
   {
      public Given_a_file_written_in_UTF_16_with_a_byte_order_mark() => File.WriteAllText(_file, "日本語", Encoding.Unicode);

      [XF] public void AllText_is_the_text_decoded_as_the_byte_order_mark_says() => FileReadLettingItBeReplaced.AllText(_file).Must().Be("日本語");
   }

   public class While_the_file_is_open_to_read : FileReadLettingItBeReplaced_
   {
      readonly FileStream _reader;

      public While_the_file_is_open_to_read()
      {
         File.WriteAllText(_file, "first");
         _reader = FileReadLettingItBeReplaced.Open(_file);
         File.AppendAllText(_file, " and second");
         File.Delete(_file);
      }

      [XF] public void another_may_write_it_and_delete_it() => File.Exists(_file).Must().BeFalse();
      [XF] public void the_reader_reads_on() => TextReadOnBy(_reader).Must().Be("first and second");

      protected override void DisposeInternal()
      {
         _reader.Dispose();
         base.DisposeInternal();
      }
   }
}
