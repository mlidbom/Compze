using System.Text;

namespace Compze.Internals.SystemCE.Core.IOCE;

public partial class DirectoryCE : FileSystemInfoCE
{
#pragma warning disable CA1024 // Use properties. No, because that would imply that it is part of the instance state and that changing properties in it would change instance state.
   public DirectoryInfo GetDirectoryInfo() => new(AbsolutePath);
#pragma warning restore CA1024 // Use properties

   public DirectoryCE(DirectoryInfo directoryInfo) : base(directoryInfo){}

   public DirectoryCE GetOrCreateDirectory(string subDirectory)
   {
      if(TryGetSubDirector(subDirectory) is {} subdirectory)
      {
         return new DirectoryCE(subdirectory);
      }

      return new DirectoryCE(GetDirectoryInfo().CreateSubdirectory(subDirectory));
   }

   public string GetFilePath(string fileName) => Path.Combine(GetDirectoryInfo().FullName, fileName);

   public TextFile GetOrCreateTextFile(string fileName, Encoding? encoding = null, Func<string>? createInitialContent = null)
   {
      if(TryGetFile(fileName) is {} existingFile)
         return new TextFile(existingFile.GetFileInfo(), encoding ?? Encoding.UTF8);

      return TextFile.Create(this, fileName, encoding, createInitialContent?.Invoke() ?? "");
   }

   public BinaryFile GetOrCreateBinaryFile(string fileName, Func<byte[]>? createInitialContent = null)
   {
      if(TryGetFile(fileName) is {} existingFile)
         return new BinaryFile(existingFile.GetFileInfo());

      return BinaryFile.Create(this, fileName, createInitialContent?.Invoke() ?? []);
   }

   FileCE? TryGetFile(string fileName) => (GetDirectoryInfo().GetFiles().SingleOrDefault(it => it.Name == fileName) is {} fileInfo) ? new FileCE(fileInfo) : null;

   DirectoryInfo? TryGetSubDirector(string name) => GetDirectoryInfo().GetDirectories().SingleOrDefault(it => it.Name == name);
}
