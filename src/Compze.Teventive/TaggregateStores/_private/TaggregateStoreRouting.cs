using System.Collections.Concurrent;
using Compze.Teventive.TaggregateStores._internal;
using Compze.Contracts;

namespace Compze.Teventive.TaggregateStores._private;

///<summary>Routes each taggregate class to the one store of a container that keeps it: the store that declares that class, or, for a<br/>
/// base class such as the root of a hierarchy of note classes, the store that declares its subclasses.</summary>
sealed class TaggregateStoreRouting
{
   readonly IReadOnlyList<ITaggregateStore> _stores;
   readonly ConcurrentDictionary<Type, ITaggregateStore> _storesByClass = new();

   internal TaggregateStoreRouting(IEnumerable<ITaggregateStore> stores) => _stores = [..stores];

   internal ITaggregateStore StoreKeeping(Type taggregateClass) => _storesByClass.GetOrAdd(taggregateClass, FindTheStoreKeeping);

   ITaggregateStore FindTheStoreKeeping(Type taggregateClass)
   {
      var storesKeepingIt = _stores.Where(store => store.TaggregateClasses.Any(it => it.IsAssignableTo(taggregateClass))).ToList();
      State.Assert(storesKeepingIt.Count > 0,
                   () => $"No taggregate store keeps {taggregateClass.FullName}. Declare the class for a store where the store is composed. {DescriptionOfTheStores()}");
      State.Assert(storesKeepingIt.Count == 1,
                   () => $"More than one taggregate store keeps classes that are {taggregateClass.FullName}, so it is not clear which store to ask. {DescriptionOfTheStores()}");
      return storesKeepingIt[0];
   }

   string DescriptionOfTheStores() =>
      _stores.Count == 0
         ? "This container composes no taggregate store."
         : $"The stores and the classes they keep: {string.Join("; ", _stores.Select(store => $"{store.Name}: {string.Join(", ", store.TaggregateClasses.Select(it => it.FullName))}"))}.";
}
