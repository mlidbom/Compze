using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Teventive.TaggregateStore._private;

namespace Compze.Teventive.TaggregateStore;

///<summary>Declares what a taggregate store keeps beside its taggregates, such as its indexes, where the store is composed.</summary>
public sealed class TaggregateStoreBuilder
{
   readonly InMemoryTaggregateStore _store;
   readonly IComponentRegistrar _registrar;

   internal TaggregateStoreBuilder(InMemoryTaggregateStore store, IComponentRegistrar registrar)
   {
      _store = store;
      _registrar = registrar;
   }

   ///<summary>Declares an <see cref="ITaggregateIndex{TTaggregate,TKey}"/> that files each <typeparamref name="TTaggregate"/> under<br/>
   /// the keys <paramref name="keysOf"/> gives from its state.</summary>
   public TaggregateStoreBuilder Index<TTaggregate, TKey>(Func<TTaggregate, IEnumerable<TKey>> keysOf)
      where TTaggregate : class, ISnapshotTaggregate where TKey : notnull
   {
      var index = new TaggregateIndex<TTaggregate, TKey>(_store, keysOf);
      _store.AddIndex(index);
      _registrar.Register(Singleton.For<ITaggregateIndex<TTaggregate, TKey>>().Instance(index));
      return this;
   }
}
