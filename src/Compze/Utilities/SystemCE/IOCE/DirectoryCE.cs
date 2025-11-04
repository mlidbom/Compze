using System;
using System.IO;
using System.Linq;
using System.Text;

namespace Compze.Utilities.SystemCE.IOCE;

public partial class DirectoryCE : FileSystemInfoCE
{
   public readonly DirectoryInfo DirectoryInfo;

   public DirectoryCE(DirectoryInfo directoryInfo) : base(directoryInfo) => DirectoryInfo = directoryInfo;

   public DirectoryCE GetOrCreateDirectory(string subDirectory)
   {
      if(TryGetSubDirector(subDirectory) is {} subdirectory)
      {
         return new DirectoryCE(subdirectory);
      }

      return new DirectoryCE(DirectoryInfo.CreateSubdirectory(subDirectory));
   }

   public TextFile GetOrCreateTextFile(string fileName, Encoding? encoding = null, Func<string>? createInitialContent = null)
   {
      if(TryGetFile(fileName) is {} existingFile)
         return new TextFile(existingFile.FileInfo, encoding ?? Encoding.UTF8);

      return TextFile.Create(this, fileName, encoding, createInitialContent?.Invoke() ?? "");
   }

   public FileCE? TryGetFile(string fileName) => (DirectoryInfo.GetFiles().SingleOrDefault(it => it.Name == fileName) is {} fileInfo) ? new FileCE(fileInfo) : null;

   DirectoryInfo? TryGetSubDirector(string name) => DirectoryInfo.GetDirectories().SingleOrDefault(it => it.Name == name);
}
