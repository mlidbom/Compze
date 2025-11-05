using System;
using System.IO;
using System.Linq;
using System.Text;

namespace Compze.Utilities.SystemCE.IOCE;

public partial class DirectoryCE : FileSystemInfoCE
{
   public DirectoryInfo GetDirectoryInfo() => new(AbsolutePath);

   public DirectoryCE(DirectoryInfo directoryInfo) : base(directoryInfo){}

   public DirectoryCE GetOrCreateDirectory(string subDirectory)
   {
      if(TryGetSubDirector(subDirectory) is {} subdirectory)
      {
         return new DirectoryCE(subdirectory);
      }

      return new DirectoryCE(GetDirectoryInfo().CreateSubdirectory(subDirectory));
   }

   public TextFile GetOrCreateTextFile(string fileName, Encoding? encoding = null, Func<string>? createInitialContent = null)
   {
      if(TryGetFile(fileName) is {} existingFile)
         return new TextFile(existingFile.GetFileInfo(), encoding ?? Encoding.UTF8);

      return TextFile.Create(this, fileName, encoding, createInitialContent?.Invoke() ?? "");
   }

   public FileCE? TryGetFile(string fileName) => (GetDirectoryInfo().GetFiles().SingleOrDefault(it => it.Name == fileName) is {} fileInfo) ? new FileCE(fileInfo) : null;

   DirectoryInfo? TryGetSubDirector(string name) => GetDirectoryInfo().GetDirectories().SingleOrDefault(it => it.Name == name);

   protected override FileSystemInfo GetFileSystemInfo() => GetDirectoryInfo();
}
