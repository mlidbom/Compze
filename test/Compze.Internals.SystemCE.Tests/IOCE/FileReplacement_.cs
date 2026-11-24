using Compze.Internals.SystemCE.IOCE;
using static Compze.Must.MustActions;

namespace Compze.Internals.SystemCE.Tests.IOCE;

public class FileReplacement_ : InAFolderOfItsOwn
{
   readonly string _file, _replacement;

   FileReplacement_() : this(fileName: "file.txt") {}

   FileReplacement_(string fileName)
   {
      _file = PathOf(fileName);
      _replacement = _file + ".being-written";
   }

   string TextOfTheFile => File.ReadAllText(_file);

   void WriteTheReplacementAndReplaceTheFile()
   {
      File.WriteAllText(_replacement, "new");
      FileReplacement.Replace(_file, by: _replacement);
   }

   public class When_there_is_no_file_to_replace : FileReplacement_
   {
      public When_there_is_no_file_to_replace() => WriteTheReplacementAndReplaceTheFile();

      [XF] public void the_file_holds_the_replacement_s_text() => TextOfTheFile.Must().Be("new");
      [XF] public void the_replacement_s_name_is_gone() => File.Exists(_replacement).Must().BeFalse();
   }

   public class When_a_reader_has_the_file_open_letting_it_be_replaced : FileReplacement_
   {
      readonly FileStream _reader;

      public When_a_reader_has_the_file_open_letting_it_be_replaced()
      {
         File.WriteAllText(_file, "old");
         _reader = FileReadLettingItBeReplaced.Open(_file);
         WriteTheReplacementAndReplaceTheFile();
      }

      [XF] public void the_file_holds_the_replacement_s_text() => TextOfTheFile.Must().Be("new");
      [XF] public void the_reader_reads_the_old_text_whole() => TextReadOnBy(_reader).Must().Be("old");

      protected override void DisposeInternal()
      {
         _reader.Dispose();
         base.DisposeInternal();
      }
   }

   /// <summary>Windows' own file functions take a path without the extended-length prefix only up to 259 characters.</summary>
   public class When_the_path_is_longer_than_260_characters : FileReplacement_
   {
      public When_the_path_is_longer_than_260_characters() : base(fileName: Path.Combine(new string('a', 120), new string('b', 120), "file.txt"))
      {
         Directory.CreateDirectory(Path.GetDirectoryName(_file)!);
         File.WriteAllText(_file, "old");
         WriteTheReplacementAndReplaceTheFile();
      }

      [XF] public void the_path_is_longer_than_260_characters() => _file.Length.Must().BeGreaterThan(260);
      [XF] public void the_file_holds_the_replacement_s_text() => TextOfTheFile.Must().Be("new");
   }

   public class When_there_is_no_replacement : FileReplacement_
   {
      [XF] public void it_throws_a_FileNotFoundException() =>
         Invoking(() => FileReplacement.Replace(_file, by: _replacement)).Must().Throw<FileNotFoundException>();
   }
}
