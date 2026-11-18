using Compze.Contracts;
using Compze.Teventive.StateStore._internal;
using static Compze.Contracts.Contract;

namespace Compze.Teventive.StateStore._private.Files;

///<summary>Which file holds each taggregate of a state store kept in files, with the file's length and last write time when the store<br/>
/// last read or wrote it, as it was loaded and committed since: so that a commit knows which file a deletion deletes, and a reload<br/>
/// which files no other program has changed since. Not thread safe: the storage shares it between its loads and writes under a lock.</summary>
sealed class StoredFiles
{
   readonly Dictionary<Guid, StoredFile> _byId = new();

   internal string? FileOf(Guid id) => _byId.GetValueOrDefault(id)?.Path;

   ///<summary>Whether the store holds the taggregate <paramref name="id"/>, as it last read or wrote its file.</summary>
   internal bool Holds(Guid id) => _byId.ContainsKey(id);

   ///<summary>Whether <paramref name="file"/> is the file of the taggregate <paramref name="id"/> as the store last read or wrote it:<br/>
   /// the same file, with the same length and last write time.</summary>
   internal bool IsAsLastReadOrWritten(Guid id, ListedFile file) =>
      _byId.TryGetValue(id, out var stored)
   && string.Equals(stored.Path, file.FullPath, StringComparison.OrdinalIgnoreCase)
   && stored.Stamp.Equals(FileStamp.Of(file));

   ///<summary>Records that <paramref name="file"/>, with <paramref name="stamp"/>, holds the taggregate <paramref name="id"/>, as the<br/>
   /// store has just written it.</summary>
   internal void Record(Guid id, string file, FileStamp stamp) => _byId[id] = new StoredFile(file, stamp);

   internal void Forget(Guid id) => _byId.Remove(id);

   ///<summary>Forgets every file, and records <paramref name="loaded"/>, the files a load read. Asserts that no two hold one taggregate.</summary>
   internal void ReplaceWith(IEnumerable<(Guid Id, ListedFile File)> loaded)
   {
      _byId.Clear();
      foreach(var (id, file) in loaded)
      {
         State.Assert(!_byId.ContainsKey(id), () => $"Two files hold taggregate {id}: {_byId[id].Path} and {file.FullPath}.");
         _byId[id] = new StoredFile(file.FullPath, FileStamp.Of(file));
      }
   }

   ///<summary>A taggregate's file, and its length and last write time when the store last read or wrote it.</summary>
   sealed class StoredFile(string path, FileStamp stamp)
   {
      internal string Path { get; } = path;
      internal FileStamp Stamp { get; } = stamp;
   }
}
