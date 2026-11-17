using Compze.Teventive.StateStore.MemoryPack._private;

namespace Compze.Teventive.StateStore.MemoryPack;

///<summary>Has a state store kept in files start from a MemoryPack cache: <c>files.Of&lt;VocabNote&gt;("vocab").CachedWithMemoryPack()</c>.</summary>
public static class TaggregateFilesMemoryPackCE
{
   extension(TaggregateFiles @this)
   {
      ///<summary>Has the store start from a cache of the snapshots its files held at an earlier start, kept with MemoryPack in the<br/>
      /// store's own folder, so that it reads only the files that are new, or whose length or last write time changed since.</summary>
      ///<remarks>Each class's snapshot type must be one MemoryPack serializes, such as a <c>[MemoryPackable]</c> class: asserted where<br/>
      /// the store is composed. A change to a snapshot type's fields makes the store read every file of the class once, and write<br/>
      /// the cache anew.<br/>
      /// review-naming: CachedWithMemoryPack, a placeholder for the naming pass (jastudio-rep-opus's fork, 2026-11-17).</remarks>
      public TaggregateFiles CachedWithMemoryPack() => @this.StartFrom(new MemoryPackLoadCache());
   }
}
