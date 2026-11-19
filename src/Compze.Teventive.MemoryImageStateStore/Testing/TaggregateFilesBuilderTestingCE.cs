namespace Compze.Teventive.MemoryImageStateStore.Testing;

///<summary>Puts the files of a memory image state store kept in files on a file system in memory, for specifications:<br/>
/// <c>files =&gt; files.OnAFileSystemInMemory().FolderOf&lt;VocabNote&gt;("vocab")</c>. Seen only where this namespace is imported.</summary>
public static class TaggregateFilesBuilderTestingCE
{
   extension(TaggregateFilesBuilder @this)
   {
      ///<summary>Puts the files on a file system in memory instead of on the disk, for specifications: the store works as it does on<br/>
      /// the disk, with the same layout, the consumer's JSON options, the journal and any cache, and touches no file on the disk. The<br/>
      /// folder the store is composed with is then only where the files' paths begin. A specification reads and changes the files<br/>
      /// through <see cref="IFileSystemInMemory"/>, which the container then resolves, and finds a taggregate's file through<br/>
      /// <see cref="ITaggregateJsonFiles"/>, as on the disk.</summary>
      ///<remarks>The file system lives as long as the container, one for each container: a second container is a second store with<br/>
      /// files of its own, not a store started again on the same files.</remarks>
      public TaggregateFilesBuilder OnAFileSystemInMemory() => @this.PutOnAFileSystemInMemory();
   }
}
