namespace Compze.Internals.SystemCE.IOCE;

public class BinaryFile : FileCE, IBinaryFile
{
   internal BinaryFile(FileInfo fileInfo) : base(fileInfo) {}

   public void WriteAllBytes(byte[] bytes) => File.WriteAllBytes(GetFileInfo().FullName, bytes);
   public byte[] ReadAllBytes() => File.ReadAllBytes(GetFileInfo().FullName);
   public void Delete() => GetFileInfo().Delete();

   internal static BinaryFile Create(DirectoryCE directory, string name, byte[] content)
   {
      var path = Path.Combine(directory.GetDirectoryInfo().FullName, name);

      File.WriteAllBytes(path, content);
      return new BinaryFile(new FileInfo(path));
   }
}
