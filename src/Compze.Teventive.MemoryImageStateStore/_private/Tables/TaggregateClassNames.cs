using Compze.Teventive.Taggregates._internal;
using Compze.Contracts;
using static Compze.Contracts.Contract;

namespace Compze.Teventive.MemoryImageStateStore._private.Tables;

///<summary>The name declared for each taggregate class a memory image state store keeps in tables, which its rows hold instead of the class's<br/>
/// .NET name, so that the class can be renamed or moved and its rows still be read.</summary>
sealed class TaggregateClassNames
{
   readonly IReadOnlyDictionary<Type, string> _nameByClass;
   readonly IReadOnlyDictionary<string, SnapshotableTaggregateClass> _classByName;

   ///<summary>The names <paramref name="nameByClass"/> declares for <paramref name="classes"/>, which the store keeps.</summary>
   internal TaggregateClassNames(IReadOnlyDictionary<Type, string> nameByClass, IEnumerable<SnapshotableTaggregateClass> classes)
   {
      _nameByClass = nameByClass;
      var classByType = classes.ToDictionary(it => it.Class);
      _classByName = _nameByClass.ToDictionary(it => it.Value, it => classByType[it.Key]);
   }

   ///<summary>Every declared name.</summary>
   internal IReadOnlyList<string> Names => [.._classByName.Keys];

   internal bool Declares(string name) => _classByName.ContainsKey(name);

   internal string NameOf(Type taggregateClass) => _nameByClass[taggregateClass];

   internal SnapshotableTaggregateClass ClassNamed(string name)
   {
      State.Assert(Declares(name), () => $"No taggregate class is declared by the name \"{name}\" for the memory image state store: it declares {string.Join(", ", Names.Select(it => $"\"{it}\""))}.");
      return _classByName[name];
   }
}
