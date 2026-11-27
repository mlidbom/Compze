using Compze.DbPool._private;

namespace Compze.DbPool.InternalSpecifications.DbPoolMachineWideState_;

///<summary>A <see cref="DbPoolMachineWideState"/> that no other test, and no other test run, reserves in: of a name of its own, in a folder of its<br/>
/// own, which disposing it deletes.</summary>
sealed class AStateOfItsOwn : IDisposable
{
   readonly DirectoryInfo _directory = Directory.CreateTempSubdirectory("compze-dbpool-specification-");

   internal AStateOfItsOwn(int maximumNumberOfDatabases) =>
      State = new DbPoolMachineWideState($"{nameof(AStateOfItsOwn)}_{Guid.NewGuid():N}", maximumNumberOfDatabases, _directory);

   internal DbPoolMachineWideState State { get; }

   public void Dispose()
   {
      State.Dispose();
      _directory.Delete(recursive: true);
   }
}
