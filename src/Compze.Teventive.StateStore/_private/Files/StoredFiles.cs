using Compze.Contracts;
using static Compze.Contracts.Contract;

namespace Compze.Teventive.StateStore._private.Files;

///<summary>Which file holds each taggregate of a state store kept in files, as it was loaded and committed since: so that a commit<br/>
/// knows which file a deletion deletes, and which old file a taggregate that gets another file leaves. Not thread safe: the storage<br/>
/// shares it between its writes under a lock.</summary>
sealed class StoredFiles
{
   readonly Dictionary<Guid, string> _fileById = new();
   readonly Dictionary<string, Guid> _idByFile = new(StringComparer.OrdinalIgnoreCase);

   internal string? FileOf(Guid id) => _fileById.GetValueOrDefault(id);

   ///<summary>The taggregate whose file <paramref name="file"/> is, if any.</summary>
   internal Guid? TaggregateIn(string file) => _idByFile.TryGetValue(file, out var id) ? id : null;

   internal void Record(Guid id, string file)
   {
      Forget(id);
      _fileById[id] = file;
      _idByFile[file] = id;
   }

   internal void Forget(Guid id)
   {
      if(_fileById.Remove(id, out var file)) _idByFile.Remove(file);
   }

   ///<summary>Forgets every file, and records <paramref name="loaded"/>, the files a load read. Asserts that no two hold one taggregate.</summary>
   internal void ReplaceWith(IEnumerable<(Guid Id, string File)> loaded)
   {
      _fileById.Clear();
      _idByFile.Clear();
      foreach(var (id, file) in loaded)
      {
         State.Assert(!_fileById.ContainsKey(id), () => $"Two files hold taggregate {id}: {_fileById[id]} and {file}.");
         Record(id, file);
      }
   }
}
