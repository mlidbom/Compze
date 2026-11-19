using Compze.Contracts;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Teventive._internal;
using Compze.Teventive._internal.Implementation;
using Compze.Teventive.TaggregateStores;
using Compze.Teventive.TaggregateStores._internal;
using Compze.Teventive.TeventStore.Abstractions;
using Compze.Teventive.TeventStore.Abstractions.Refactoring.Migrations;
using Compze.TypeIdentifiers.DependencyInjection;
using Compze.Teventive.TeventStore._private;

namespace Compze.Teventive.TeventStore.Wiring;

///<summary>Composes a tevent store into a container.</summary>
public static class TeventStoreRegistrar
{
   static readonly ITeventMigration[] EmptyMigrationsArray = [];

   ///<summary>Composes a tevent store, which keeps taggregates as their history: the taggregate classes <paramref name="declare"/><br/>
   /// declares. Its taggregates are read through the container's <see cref="ITaggregateStoreReader"/>, their histories<br/>
   /// through its <see cref="ITeventStoreReader"/>, and they are changed through each unit of work's<br/>
   /// <see cref="ITaggregateStoreUpdater"/>.</summary>
   ///<remarks>It holds no committed taggregate, so every read builds one, but inside the unit of work that holds it. A read through<br/>
   /// <see cref="ITaggregateStoreReader"/>, or of a reference's <see cref="ITaggregateReference{TTaggregate}.Current"/>, reads the<br/>
   /// taggregate's history, asking the database for the tevents newer than its cache of histories holds, and replays the whole history<br/>
   /// into a new instance, read only: two reads give two instances.<br/>
   /// A get for update reads the history with the database's update lock, which the transaction holds until it completes, so another<br/>
   /// unit of work that gets the taggregate for update waits for as long as the database lets it. The unit of work's instance is built<br/>
   /// once, and each tevent it publishes is written to the database as it is published, inside the transaction.<br/>
   /// <see cref="ITaggregateStoreBulkReader.ReferencesToAll{TTaggregate}"/> reads the creation tevent of every history of the class, and<br/>
   /// the ids of the deleted taggregates, at each call. A taggregate's <see cref="ITaggregate.Version"/> counts the tevents of its history.</remarks>
   ///<remarks>A history doesn't say which class it is the history of, so the store builds a taggregate as the class it is asked for as,<br/>
   /// or as the one declared class derived from it: asked for as a base class from which several declared classes derive, it throws.<br/>
   /// Get a taggregate as its own class.</remarks>
   public static IComponentRegistrar TeventStore(this IComponentRegistrar registrar, string connectionName, Action<TeventStoreBuilder>? declare = null) =>
      registrar.TeventStoreForFlexibleTesting(connectionName, () => EmptyMigrationsArray, declare);

   ///<summary>As <see cref="TeventStore"/>, with the migrations <paramref name="migrations"/> gives, which it calls at each use, so<br/>
   /// that a specification may change them while the container lives.</summary>
   public static IComponentRegistrar TeventStoreForFlexibleTesting(this IComponentRegistrar registrar,
                                                                   string connectionName,
                                                                   Func<IReadOnlyList<ITeventMigration>> migrations,
                                                                   Action<TeventStoreBuilder>? declare = null)
   {
      Contract.Argument.NotNullEmptyOrWhitespace(connectionName);
      var builder = new TeventStoreBuilder();
      declare?.Invoke(builder);

      Teventive.TeventStore.TeventStore.RegisterWith(registrar, migrations);

      //Everything the store persists needs identity that survives renames: its own stored types, the taggregate tevent
      //hierarchy it stores, and the entity ids those tevents carry.
      registrar.RequireMappedTypesFromAssemblyContaining<Refactoring.Migrations._private.EndOfTaggregateHistoryTeventPlaceHolder>();

      registrar.Register(TaggregateTypeValidator.RegisterWith, TeventCache.RegisterWith);
      registrar.Register(
         Singleton.For<TaggregateTeventStore>()
                  .CreatedBy((TeventStore teventStore, TaggregateUnitsOfWork unitsOfWork, ITaggregateTypeValidator validator) => builder.Build(teventStore, unitsOfWork, validator)),
         Singleton.For<ITeventStoreReader>()
                  .CreatedBy((TaggregateTeventStore store) => new TeventStoreReader(store)));
      registrar.RegisterTaggregateStore<TaggregateTeventStore>();
      return registrar;
   }
}
