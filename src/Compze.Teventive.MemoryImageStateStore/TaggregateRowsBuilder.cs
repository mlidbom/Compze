using Compze.Contracts;
using Compze.Teventive.MemoryImageStateStore._internal.SqlLayer;
using static Compze.Contracts.Contract;

namespace Compze.Teventive.MemoryImageStateStore;

///<summary>Declares, for a memory image state store kept in the domain database's tables<br/>
/// (<see cref="MemoryImageStateStoreBuilder.StoredInTheDomainDatabase"/>), the name by which each taggregate class's rows hold it.</summary>
public sealed class TaggregateRowsBuilder
{
   readonly Dictionary<Type, string> _namesByClass = new();

   internal TaggregateRowsBuilder() {}

   ///<summary>Declares that the rows of <typeparamref name="TTaggregate"/> hold its class by <paramref name="name"/>, as<br/>
   /// <see cref="TaggregateFilesBuilder.FolderOf{TTaggregate}"/> names a class's folder: a name of the consumer's own, not the class's<br/>
   /// .NET name, so that the class can be renamed or moved and its rows still be read. Each class the store keeps is named, and no two<br/>
   /// share a name.</summary>
   ///<remarks>A store reads only the rows of the names it declares, so stores that keep different classes can share a database.</remarks>
   public TaggregateRowsBuilder ClassNamed<TTaggregate>(string name) where TTaggregate : class, ITaggregate
   {
      Argument.Assert(!string.IsNullOrWhiteSpace(name) && name.Length <= IMemoryImageStateStoreSqlLayer.TableSchemaStrings.ClassNameLength,
                      () => $"The name of {typeof(TTaggregate).FullName} in the rows, \"{name}\", must be a name of at most {IMemoryImageStateStoreSqlLayer.TableSchemaStrings.ClassNameLength} characters.");
      State.Assert(!_namesByClass.ContainsKey(typeof(TTaggregate)), () => $"The name of {typeof(TTaggregate).FullName} in the rows is declared twice.");
      var sharing = _namesByClass.FirstOrDefault(it => it.Value == name).Key;
      State.Assert(sharing == null, () => $"{typeof(TTaggregate).FullName} and {sharing!.FullName} are both named \"{name}\" in the rows. Each class needs a name of its own.");
      _namesByClass.Add(typeof(TTaggregate), name);
      return this;
   }

   internal IReadOnlyDictionary<Type, string> NamesByClass => _namesByClass;
}
