using System;
using System.IO;
using System.Linq;
using System.Text;


namespace Compze.Utilities.SystemCE.IOCE;

partial class DirectoryCE : FileSystemInfoCE
{
#pragma warning disable CA1024 // Use properties. No, because that would imply that it is part of the instance state and that changing properties in it would change instance state.
   internal DirectoryInfo GetDirectoryInfo() => new(AbsolutePath);
#pragma warning restore CA1024 // Use properties

   private DirectoryCE(DirectoryInfo directoryInfo) : base(directoryInfo){}

   internal DirectoryCE GetOrCreateDirectory(string subDirectory)
   {
      if(TryGetSubDirector(subDirectory) is {} subdirectory)
      {
         return new DirectoryCE(subdirectory);
      }

      return new DirectoryCE(GetDirectoryInfo().CreateSubdirectory(subDirectory));
   }

   internal TextFile GetOrCreateTextFile(string fileName, Encoding? encoding = null, Func<string>? createInitialContent = null)
   {
      if(TryGetFile(fileName) is {} existingFile)
         return new TextFile(existingFile.GetFileInfo(), encoding ?? Encoding.UTF8);

      return TextFile.Create(this, fileName, encoding, createInitialContent?.Invoke() ?? "");
   }

   private FileCE? TryGetFile(string fileName) => (GetDirectoryInfo().GetFiles().SingleOrDefault(it => it.Name == fileName) is {} fileInfo) ? new FileCE(fileInfo) : null;

   DirectoryInfo? TryGetSubDirector(string name) => GetDirectoryInfo().GetDirectories().SingleOrDefault(it => it.Name == name);

   protected override FileSystemInfo GetFileSystemInfo() => GetDirectoryInfo();
}
