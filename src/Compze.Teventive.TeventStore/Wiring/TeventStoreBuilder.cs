using Compze.Contracts;
using Compze.Teventive._internal;
using Compze.Teventive.TaggregateStores._internal;
using Compze.Teventive.Taggregates.Tevents;
using Compze.Teventive.TeventStore.Abstractions;
using Compze.Teventive.TeventStore._private;
using static Compze.Contracts.Contract;

namespace Compze.Teventive.TeventStore.Wiring;

///<summary>Declares, where a tevent store is composed, the taggregate classes it keeps. The container's<br/>
/// <see cref="TaggregateStores.ITaggregateStoreUpdater"/> and <see cref="TaggregateStores.ITaggregateStoreReader"/> route each of them to the<br/>
/// tevent store.</summary>
public sealed class TeventStoreBuilder
{
   readonly Dictionary<Type, Action<ITaggregateTypeValidator>> _taggregateClasses = new();

   internal TeventStoreBuilder() {}

   ///<summary>Declares that the store keeps <typeparamref name="TTaggregate"/>, which it builds from its history through its<br/>
   /// parameterless constructor, public or not.</summary>
   public TeventStoreBuilder Taggregate<TTaggregate>() where TTaggregate : class, ITaggregate<ITaggregateTeventWithIdentity>
   {
      State.Assert(!_taggregateClasses.ContainsKey(typeof(TTaggregate)), () => $"{typeof(TTaggregate).FullName} is declared twice for the tevent store.");
      _taggregateClasses.Add(typeof(TTaggregate), validator => validator.AssertIsValid<TTaggregate>());
      return this;
   }

   ///<summary>The store, keeping the classes declared by now, each of them checked by <paramref name="validator"/>.</summary>
   internal TaggregateTeventStore Build(ITeventStore teventStore, TaggregateUnitsOfWork unitsOfWork, ITaggregateTypeValidator validator)
   {
      foreach(var assertIsValid in _taggregateClasses.Values) assertIsValid(validator);
      return new TaggregateTeventStore(teventStore, unitsOfWork, [.._taggregateClasses.Keys]);
   }
}
