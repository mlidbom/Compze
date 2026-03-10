using System.Text;

namespace Compze.Internals.SystemCE.Core.IOCE;

public class TextFile : FileCE
{
   readonly Encoding _encoding;
   internal TextFile(FileInfo fileInfo, Encoding encoding) : base(fileInfo) => _encoding = encoding;

   public void WriteAllText(string text) => File.WriteAllText(GetFileInfo().FullName, text, _encoding);
   public string ReadAllText() => File.ReadAllText(GetFileInfo().FullName, _encoding);

   internal static TextFile Create(DirectoryCE directory, string name, Encoding? encoding = null, string content = "")
   {
      encoding ??= Encoding.UTF8;
      var path = Path.Combine(directory.GetDirectoryInfo().FullName, name);

      File.WriteAllText(path, content, encoding);
      return new TextFile(new FileInfo(path), encoding);
   }
}
