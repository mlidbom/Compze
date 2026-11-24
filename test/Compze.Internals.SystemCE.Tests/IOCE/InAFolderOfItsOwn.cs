using Compze.Tests.Infrastructure;

namespace Compze.Internals.SystemCE.Tests.IOCE;

/// <summary>A specification whose files are in a temporary folder of its own, which is deleted when it is done.</summary>
public abstract class InAFolderOfItsOwn : UniversalTestBase
{
   readonly DirectoryInfo _folder = Directory.CreateTempSubdirectory("compze-IOCE-");

   protected string PathOf(string name) => Path.Combine(_folder.FullName, name);

   /// <summary>The text that <paramref name="reader"/> reads on to the end of its file, which it leaves open.</summary>
   protected static string TextReadOnBy(FileStream reader)
   {
      using var text = new StreamReader(reader, leaveOpen: true);
      return text.ReadToEnd();
   }

   protected override void DisposeInternal() => _folder.Delete(recursive: true);
}
