using System;
using System.IO;
using System.Linq;
using Compze.Functional;
using Compze.Logging;
using Compze.SystemCE.IOCE;
using Compze.SystemCE.LinqCE;
using Compze.Testing;
using NUnit.Framework;

namespace Compze.Tests.IO;

[TestFixture]
public class DirectoryExtensionsTest : UniversalTestBase
{
   [Test]
   public void AsDirectoryShouldReturnDirectoryInfoWithFullNameBeingTheOriginalString()
   {
      const string dir = @"C:\";
      Assert.That(dir.AsDirectory().FullName, Is.EqualTo(dir));
   }

   [Test]
   public void DeleteRecursiveShouldRemoveDirectoryHierarchy()
   {
      var directory = CreateUsableFolderPath();
      CreateDirectoryHierarchy(directory, 2);

      Assert.That(directory.AsDirectory().Exists, "There should be a directory at first");
      Assert.That(directory.AsDirectory().GetDirectories().Any(), Is.True, "There should be subdirectories");

      ConsoleCE.WriteLine("Deleting directory {0}", directory);
      directory.AsDirectory().DeleteRecursive();
      ConsoleCE.WriteLine("Deleted directory {0}", directory);


      Assert.That(directory.AsDirectory().Exists, Is.False, "Directory should have been deleted");
   }

   [Test]
   public void SizeShouldCorrectlyCalculateSize()
   {
      var directory = CreateUsableFolderPath();
      var size = CreateDirectoryHierarchy(directory, 3);

      Assert.That(directory.AsDirectory().Size(), Is.EqualTo(size));

      ConsoleCE.WriteLine("Deleting directory {0}", directory);
      directory.AsDirectory().Delete(true);
      ConsoleCE.WriteLine("Deleted directory {0}", directory);
   }

   static string CreateUsableFolderPath() => Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

   static int CreateDirectoryHierarchy(string directoryPath, int depth)
   {
      if(depth <= 0)
      {
         return 0;
      }

      directoryPath.AsDirectory().Create();
      ConsoleCE.WriteLine("created directory {0}", directoryPath);
      var fileContent = new byte[100];
      var size = 0;
      directoryPath.Repeat(2).Select(dir => Path.Combine(dir, Guid.NewGuid().ToString())).ForEach(
         file =>
         {
            using(var stream = File.Create(file))
            {
               stream.Write(fileContent, 0, fileContent.Length);
               size += fileContent.Length;
            }
            ConsoleCE.WriteLine("created file {0}", file);
         });

      size += directoryPath.Repeat(2)
                           .Select(dir => Path.Combine(dir, Guid.NewGuid().ToString()))
                           .Sum(subdir => CreateDirectoryHierarchy(subdir, depth - 1));

      return size;
   }
}