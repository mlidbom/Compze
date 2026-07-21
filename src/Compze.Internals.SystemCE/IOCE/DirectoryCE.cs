namespace Compze.Internals.SystemCE.IOCE;

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

   DirectoryInfo? TryGetSubDirector(string name) => GetDirectoryInfo().GetDirectories().SingleOrDefault(it => it.Name == name);
}
