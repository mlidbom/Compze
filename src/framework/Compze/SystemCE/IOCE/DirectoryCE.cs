using System.IO;
using System.Linq;
using Compze.Contracts;
using Compze.GenericAbstractions.Hierarchies;

namespace Compze.SystemCE.IOCE;

/// <summary/>
static class DirectoryCE
{
   /// <summary>
   /// Called on <paramref name="path"/> return a DirectoryInfo instance
   /// pointed at that path.
   /// </summary>
   /// <param name="path"></param>
   /// <returns></returns>
   public static DirectoryInfo AsDirectory(this string path)
   {
      Assert.Argument.NotNullEmptyOrWhitespace(path);
      return new DirectoryInfo(path);
   }

   /// <summary>
   /// Returns the size of the directory.
   /// </summary>
   public static long Size(this DirectoryInfo me)
   {
      Contracts.Assert.Argument.NotNull(me);
      return me.FullName
               .AsHierarchy(Directory.GetDirectories).Flatten().Unwrap()
               .SelectMany(Directory.GetFiles)
               .Sum(file => new FileInfo(file).Length);
   }

   /// <summary>
   /// Recursively deletes everything in a airectory and the directory itself.
   /// 
   /// A more intuitive alias for <see cref="DirectoryInfo.Delete(bool)"/>
   /// called with <paramref name="me"/> and true.
   /// </summary>
   /// <param name="me"></param>
   public static void DeleteRecursive(this DirectoryInfo me)
   {
      Contracts.Assert.Argument.NotNull(me);
      me.Delete(true);
   }
}