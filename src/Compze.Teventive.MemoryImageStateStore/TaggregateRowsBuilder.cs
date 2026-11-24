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

   ///<summary>Has the memory image catch up with what other memory images on this machine commit to the tables within about 50<br/>
   /// milliseconds of their commits, while nothing else makes it catch up, and tell <see cref="IMemoryImageStateStore.ChangesCommittedByOtherMemoryImages"/><br/>
   /// what it found. Without it, a memory image catches up at each unit of work's first get for update or add, and reads outside a unit<br/>
   /// of work read what it last caught up with.</summary>
   ///<remarks>Every memory image of a store kept in tables raises an interprocess signal, named from the database, once a commit of its<br/>
   /// own has committed; one declared to catch up waits for it on a task of its own, polling a memory read, which wakes the processor at<br/>
   /// most every 50 milliseconds, and queries the database only when the signal moved. It reaches the memory images of this machine's<br/>
   /// user that name the database by the same connection string: memory images on other machines are caught up with only by units of<br/>
   /// work. A catch-up that fails is logged, and tried again at the next signal.</remarks>
   public TaggregateRowsBuilder CatchingUpAsOtherMemoryImagesOnThisMachineCommit()
   {
      State.Assert(!IsCatchingUpAsOtherMemoryImagesOnThisMachineCommit, () => "That the memory image catches up as other memory images on this machine commit is declared twice.");
      IsCatchingUpAsOtherMemoryImagesOnThisMachineCommit = true;
      return this;
   }

   ///<summary>Whether <see cref="CatchingUpAsOtherMemoryImagesOnThisMachineCommit"/> is declared.</summary>
   internal bool IsCatchingUpAsOtherMemoryImagesOnThisMachineCommit { get; private set; }
}
