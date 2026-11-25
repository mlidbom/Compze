using System.Diagnostics;
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

   /// <summary>Whether the specifications run on Windows, which alone refuses a file that another has open.</summary>
   public static bool OnWindows => OperatingSystem.IsWindows();

   const string OnlyWindowsRefusesAFileAnotherHasOpen = "Only Windows refuses to rename a file that another has open.";

   /// <summary>How a failure names this process as one that Windows counts as using <paramref name="path"/>.</summary>
   static string ThisProcessNamedAsUsing(string path)
   {
      using var thisProcess = Process.GetCurrentProcess();
      return $"{path}: Windows counts as using it: {thisProcess.ProcessName} (PID {thisProcess.Id})";
   }

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

   public class When_another_has_the_replacement_open_not_letting_it_be_deleted : FileReplacement_
   {
      readonly FileStream _holder;

      public When_another_has_the_replacement_open_not_letting_it_be_deleted()
      {
         File.WriteAllText(_replacement, "new");
         _holder = new FileStream(_replacement, FileMode.Open, FileAccess.Read, FileShare.Read);
      }

      [XF(Skip = OnlyWindowsRefusesAFileAnotherHasOpen, SkipType = typeof(FileReplacement_), SkipUnless = nameof(OnWindows))]
      public void it_throws_an_IOException_that_names_the_process_holding_the_replacement() =>
         Invoking(() => FileReplacement.Replace(_file, by: _replacement)).Must().Throw<IOException>().Which.Message.Must().Contain(ThisProcessNamedAsUsing(_replacement));

      protected override void DisposeInternal()
      {
         _holder.Dispose();
         base.DisposeInternal();
      }
   }

   public class When_a_reader_has_the_file_open_not_letting_it_be_deleted : FileReplacement_
   {
      readonly FileStream _reader;

      public When_a_reader_has_the_file_open_not_letting_it_be_deleted()
      {
         File.WriteAllText(_file, "old");
         File.WriteAllText(_replacement, "new");
         _reader = new FileStream(_file, FileMode.Open, FileAccess.Read, FileShare.Read);
      }

      [XF(Skip = OnlyWindowsRefusesAFileAnotherHasOpen, SkipType = typeof(FileReplacement_), SkipUnless = nameof(OnWindows))]
      public void it_throws_an_IOException_that_names_the_process_holding_the_file() =>
         Invoking(() => FileReplacement.Replace(_file, by: _replacement)).Must().Throw<IOException>().Which.Message.Must().Contain(ThisProcessNamedAsUsing(_file));

      [XF(Skip = OnlyWindowsRefusesAFileAnotherHasOpen, SkipType = typeof(FileReplacement_), SkipUnless = nameof(OnWindows))]
      public void the_exception_counts_no_process_as_using_the_replacement() =>
         Invoking(() => FileReplacement.Replace(_file, by: _replacement)).Must().Throw<IOException>().Which.Message.Must().Contain($"{_replacement}: Windows counts no process as using it now.");

      protected override void DisposeInternal()
      {
         _reader.Dispose();
         base.DisposeInternal();
      }
   }

   public class When_a_file_is_put_in_place_unless_there_is_one : FileReplacement_
   {
      protected bool WriteTheReplacementAndPutItInPlace()
      {
         File.WriteAllText(_replacement, "new");
         return FileReplacement.PutInPlaceUnlessThereIsOne(_file, by: _replacement);
      }

      public class and_there_is_none : When_a_file_is_put_in_place_unless_there_is_one
      {
         readonly bool _putInPlace;

         public and_there_is_none() => _putInPlace = WriteTheReplacementAndPutItInPlace();

         [XF] public void it_returns_true() => _putInPlace.Must().BeTrue();
         [XF] public void the_file_holds_the_replacement_s_text() => TextOfTheFile.Must().Be("new");
         [XF] public void the_replacement_s_name_is_gone() => File.Exists(_replacement).Must().BeFalse();
      }

      public class and_there_is_one : When_a_file_is_put_in_place_unless_there_is_one
      {
         readonly bool _putInPlace;

         public and_there_is_one()
         {
            File.WriteAllText(_file, "old");
            _putInPlace = WriteTheReplacementAndPutItInPlace();
         }

         [XF] public void it_returns_false() => _putInPlace.Must().BeFalse();
         [XF] public void the_file_keeps_its_text() => TextOfTheFile.Must().Be("old");
         [XF] public void the_replacement_is_left_as_it_is() => File.ReadAllText(_replacement).Must().Be("new");
      }

      public class and_there_is_no_replacement : When_a_file_is_put_in_place_unless_there_is_one
      {
         [XF] public void it_throws_a_FileNotFoundException() =>
            Invoking(() => FileReplacement.PutInPlaceUnlessThereIsOne(_file, by: _replacement)).Must().Throw<FileNotFoundException>();
      }

      public class by_sixteen_writers_at_once_each_with_a_replacement_of_its_own : When_a_file_is_put_in_place_unless_there_is_one
      {
         static readonly List<string> TextsOfTheWriters = [.. 1.Through(16).Select(number => $"writer {number}")];

         readonly Barrier _start = new(TextsOfTheWriters.Count);
         List<(string Text, bool PutInPlace)> _writers = [];

         protected override async Task InitializeAsyncInternal()
         {
            await base.InitializeAsyncInternal();
            foreach(var text in TextsOfTheWriters) await File.WriteAllTextAsync(ReplacementOf(text), text);
            var putInPlace = await Task.WhenAll(TextsOfTheWriters.Select(text => OnAThreadOfItsOwnAsync(() =>
            {
               _start.SignalAndWait();
               return FileReplacement.PutInPlaceUnlessThereIsOne(_file, by: ReplacementOf(text));
            })));
            _writers = [.. TextsOfTheWriters.Zip(putInPlace)];
         }

         string ReplacementOf(string text) => $"{_file}.{text}.being-written";

         /// <summary>On a thread of its own, so that every writer waits at the start at once, which the thread pool's threads may not.</summary>
         static Task<bool> OnAThreadOfItsOwnAsync(Func<bool> putInPlace) => Task.Factory.StartNew(putInPlace, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);

         [XF] public void exactly_one_of_them_puts_its_file_in_place() => _writers.Must().Satisfy(writers => writers.Count(it => it.PutInPlace) == 1);
         [XF] public void the_file_holds_that_one_s_text() => TextOfTheFile.Must().Be(_writers.Single(it => it.PutInPlace).Text);

         protected override void DisposeInternal()
         {
            _start.Dispose();
            base.DisposeInternal();
         }
      }
   }
}
