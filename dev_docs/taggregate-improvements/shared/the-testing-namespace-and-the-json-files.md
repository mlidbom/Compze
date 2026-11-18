# The testing namespace and the JSON files: OnAFileSystemInMemory, IFileSystemInMemory and ITaggregateJsonFiles

**Decided by Magnus, 2026-11-18** (the end review's sixth question). The Fable reviewer recommended moving the testing
aids into testing packages: `AwaitNoTessagesInFlightAsync`, `KeptInMemory()` and `ITaggregateFilesInMemory`. The Opus
reviewer recommended keeping the first, and leaned to leaving the others. Magnus: "maybe just putting them in a testing
namespace is enough. Maintaining another project and nuget for those two interfaces…", and "given that we will make the
whole thing public either way, I see little loss." Specified here before it is built, on the spike after 56a764ac1.

Three changes: the files on a file system in memory move to a `Testing` namespace of the package, under new names; a new
`ITaggregateJsonFiles` finds the way between a store's files and its taggregates; and `AwaitNoTessagesInFlightAsync`
stays where it is, documented for applications too.

## The file system in memory is in a Testing namespace of the package

- **`TaggregateFilesBuilder.KeptInMemory()` becomes `OnAFileSystemInMemory()`,** an extension of `TaggregateFilesBuilder`
  in the namespace `Compze.Teventive.MemoryImageStateStore.Testing`, declared in a C# 14 extension block of the class
  `TaggregateFilesBuilderTestingCE`. It is seen only where that namespace is imported.
- **`ITaggregateFilesInMemory` becomes `IFileSystemInMemory`,** in the same namespace. Its members don't change:
  `Paths`, `ReadAllText`, `WriteAllText` and `Delete`.
- **Their files are in the package's `Testing` folder,** and their `review-naming:` marks go.
- **The internal file system they give is `FileSystemInMemory`,** the name of what it implements.

### Why the new names

The package is now a memory image: it keeps every taggregate in memory. So "kept in memory" reads as what the store does
with every taggregate. What these two give is something else: a file system that isn't the disk, on which the store
works as on the disk.

### Why a namespace, not a package

A package of its own would be one more project and one more NuGet package to maintain, for an extension method and an
interface. The namespace keeps them out of sight of code that doesn't import it, and that is all a package would add.

## ITaggregateJsonFiles finds the way between files and taggregates

```csharp
public interface ITaggregateJsonFiles
{
   string PathOf<TTaggregate>(TaggregateId id) where TTaggregate : class, ITaggregate;

   TaggregateOfAFile? TaggregateWhoseFileIs(string path);
}

public sealed class TaggregateOfAFile : IEquatable<TaggregateOfAFile>
{
   public Type Class { get; }
   public TaggregateId Id { get; }
}
```

- **It is in the package's root namespace,** since applications use it as much as specifications do.
- **The container resolves it whenever the store is composed with `StoredInJsonFiles`,** on the disk or on a file system
  in memory. A store composed without files doesn't register it.
- **`PathOf` gives the full path of a taggregate's file, whether it exists yet or not.** It throws for a class the store
  doesn't keep.
- **`TaggregateWhoseFileIs` gives the class and the id of the taggregate whose file a path is, or null.** Null for a path
  outside the folders of the store's classes, for the store's own files under `.state-store`, and for anything but a
  taggregate's .json file in a class's folder.
- **It reads only the path, not the file.** So it answers for a file that is gone, such as one version control lists as
  deleted, and for one the store doesn't hold yet, such as one a checkout added before a load.
- **`TaggregateOfAFile` is equal to another of the same class and id.** Its constructor is internal: only the store
  makes one.

### Paths are full paths, as strings

- **Strings, because files on a file system in memory aren't on the disk.** A `FileInfo` would read the disk.
- **Compared ignoring case, as the store compares them,** as on Windows.
- **A path must be full.** A relative path throws, rather than being read against the process's current folder. A path
  version control gives relative to a checkout is combined with the checkout's folder first.
- **Forward slashes are read as `Path.GetFullPath` reads them,** so a path git gives, combined with the checkout's folder,
  needs no conversion.

### The layout is written once

The layout already lives in the internal `FilesOfAClass`: `<class folder>/<first two hex digits of the id>/<id>.json`.
`PathOf` asks it. `TaggregateWhoseFileIs` takes the id a file is named by, and asks it whether that id's file is the
path. So a file named by an id in the wrong folder, or by an id in another format, is no taggregate's file, and the
layout isn't written a second time backwards.

`FilesOfAClass` becomes only where a class's files are. What the storage reads them as, the class's snapshot type, stays
with the storage. The layout of all the classes, and of the store's own folder, is one internal `TaggregateJsonFiles`,
which is what the container resolves as `ITaggregateJsonFiles`, and what the storage writes by.

### Why

The files are the consumer's artifact as much as the store's: JSON, so that they can be read, diffed and kept in version
control. Code around them needs to go between files and taggregates. JAStudio does it today with its own copy of the
layout:

- `CorpusNoteFiles.FileOf` builds a note's path from Compze's layout, marked `todo:compze:` to use what Compze gives once
  it does;
- `CorpusRepositorySpecification.FileOfTheNoteWithTheId` builds it again for the MCP specifications;
- `CorpusCheckout` reads the ids of changed notes from the paths git lists, by parsing each file's name.

With `ITaggregateJsonFiles`, each asks the store instead.

## AwaitNoTessagesInFlightAsync stays, documented for applications too

`ILocalTessagingEngine.AwaitNoTessagesInFlightAsync()` stays where it is. Its documentation gains:

- **An application's use beside a specification's:** a batch step or a tool that waits until the observers have caught
  up before it reports, exports or backs up.
- **That "no tessages in flight" holds only while nothing else publishes.** A tevent committed during the wait may or may
  not be waited for, so it is for code that controls what runs.
- **That it gives up after 30 seconds for each observer's queue, and throws.** That is the drain's timeout, the one the
  container's disposal uses.

Its changelog line in Compze.Tessaging's top section says the same.

## What a consumer writes

Before:

```csharp
using Compze.Teventive.MemoryImageStateStore;

store.StoredInJsonFiles(folder, options, files => files.KeptInMemory()
                                                       .FolderOf<VocabNote>("vocab"));

var files = container.RootResolver.Resolve<ITaggregateFilesInMemory>();
var path = Path.Combine(folder.FullName, "vocab", id.Value.ToString("N")[..2], $"{id.Value}.json");
files.WriteAllText(path, json);
```

After:

```csharp
using Compze.Teventive.MemoryImageStateStore;
using Compze.Teventive.MemoryImageStateStore.Testing;

store.StoredInJsonFiles(folder, options, files => files.OnAFileSystemInMemory()
                                                       .FolderOf<VocabNote>("vocab"));

var files = container.RootResolver.Resolve<IFileSystemInMemory>();
var path = container.RootResolver.Resolve<ITaggregateJsonFiles>().PathOf<VocabNote>(id);
files.WriteAllText(path, json);
```

And from a path version control lists, relative to the checkout:

```csharp
var jsonFiles = container.RootResolver.Resolve<ITaggregateJsonFiles>();
if(jsonFiles.TaggregateWhoseFileIs(Path.Combine(checkout.FullName, pathGitListed)) is {} taggregate)
   changed.Add(taggregate.Id);
```

## Releasing

The package has never been released, so the top section of its changelog, 0.1.0-alpha, is rewritten: the line on
`KeptInMemory()` says `OnAFileSystemInMemory()` and `IFileSystemInMemory`, and a line says what `ITaggregateJsonFiles`
gives. Its README says the same. Compze.Tessaging's public API doesn't change.

## Specifications

Beside the store's specifications, in `CQRS/MemoryImageStateStore`, through the public API.

After a unit of work adds a vocab and a sentence, on the disk and on a file system in memory alike:

1. The vocab's file, where `PathOf` says, holds what the consumer's options make of the vocab's snapshot.
2. `TaggregateWhoseFileIs` the one file in the vocab folder, as the file system lists it, is the vocab.
3. `TaggregateWhoseFileIs` the one file in the sentence folder is the sentence.
4. After a unit of work deletes the vocab, its file is gone, and `TaggregateWhoseFileIs` its path is still the vocab.

How the paths are read is the same on either, so these are specified once, on a file system in memory:

5. `PathOf` a vocab not yet written is its file in the layout, `vocab/<first two hex digits>/<id>.json`, and no such file
   exists.
6. `TaggregateWhoseFileIs` that path is that vocab: a file the store doesn't hold yet.
7. It is the vocab for its path in other case, and for its path written with forward slashes.
8. It is null for a path under `.state-store`.
9. It is null for another file in the vocab folder: a text file, a .json file not named by an id, and the vocab's file
   name in a folder not named by its id's first two hex digits.
10. It is null for a path outside the store's folder.
11. It throws for a relative path.
12. `PathOf` a class the store doesn't keep throws, naming the class.

And for a store composed without files:

13. `ITaggregateJsonFiles` is not registered.

The renamed members' specifications are the existing ones, under the new names: the specifications whose files were
"kept in memory" are those whose files are "on a file system in memory". The full suite passes.
