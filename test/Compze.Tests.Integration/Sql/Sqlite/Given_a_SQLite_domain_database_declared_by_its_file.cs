using System.Text.Json;
using Compze.Abstractions;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Hosting.Testing;
using Compze.Hosting.Testing.Wiring;
using Compze.Internals.Testing;
using Compze.Must;
using Compze.Sql.Sqlite.Wiring;
using Compze.Tessaging.Engine.Wiring;
using Compze.Teventive.MemoryImageStateStore;
using Compze.Teventive.MemoryImageStateStore.Sqlite.Wiring;
using Compze.Teventive.TaggregateStores;
using Compze.Tests.Infrastructure;
using Compze.Tests.Infrastructure.XUnit;
using Compze.Tests.Integration.CQRS.MemoryImageStateStore;

// ReSharper disable InconsistentNaming for testing
#pragma warning disable IDE1006 //Reviewed OK: Test Naming Styles

namespace Compze.Tests.Integration.Sql.Sqlite;

///<summary>A SQLite domain database declared by its file, in a fresh folder of its own, which a memory image state store keeps its<br/>
/// vocabs in: each container on it is a memory image of its own, built once the one before it has been disposed, as each start of<br/>
/// an application is.</summary>
public class Given_a_SQLite_domain_database_declared_by_its_file : UniversalTestBase
{
   static readonly JsonSerializerOptions Options = new() { Converters = { new TaggregateIdAsAGuid() } };
   readonly DirectoryInfo _folder = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "compze-sqlite-domain-database-file", Guid.NewGuid().ToString("N")));
   readonly List<IDependencyInjectionContainer> _containers = [];

   FileInfo DatabaseFile => new(Path.Combine(_folder.FullName, "domain.sqlite"));

   TaggregateId VocabId { get; } = new(Guid.NewGuid());

   IDependencyInjectionContainer AContainerOnIt()
   {
      var builder = TestEnv.DIContainer.CreateTestingContainerBuilder();
      builder.Registrar.SqliteDomainDatabase(DatabaseFile).SqliteMemoryImageStateStoreSqlLayer();
      builder.Registrar.MemoryImageStateStore(store => store.Taggregate(() => new StoredVocab())
                                                           .StoredInTheDomainDatabase(Options, rows => rows.ClassNamed<StoredVocab>("vocab")));
      builder.Registrar.LocalTessagingEngine(_ => {});
      var container = builder.Build();
      _containers.Add(container);
      return container;
   }

   void DisposeOf(IDependencyInjectionContainer container)
   {
      _containers.Remove(container);
      container.Dispose();
   }

   protected override async Task DisposeAsyncInternal()
   {
      foreach(var container in _containers) await container.DisposeAsync();
      if(_folder.Exists) _folder.Delete(recursive: true);
   }

   public class after_a_container_on_it_added_a_vocab_and_was_disposed : Given_a_SQLite_domain_database_declared_by_its_file
   {
      public after_a_container_on_it_added_a_vocab_and_was_disposed()
      {
         var container = AContainerOnIt();
         container.ScopeFactory.ExecuteUnitOfWork(unitOfWork => unitOfWork.Resolve<ITaggregateStoreUpdater>().Add(StoredVocab.Create(VocabId, "taberu")));
         DisposeOf(container);
      }

      [PCT] public void the_database_is_in_the_file() => DatabaseFile.Exists.Must().BeTrue();

      public class and_its_file_is_deleted : after_a_container_on_it_added_a_vocab_and_was_disposed
      {
         public and_its_file_is_deleted() => DatabaseFile.Delete();

         [PCT] public void the_file_is_gone() => DatabaseFile.Exists.Must().BeFalse();
      }

      public class and_a_second_container_is_built_on_it : after_a_container_on_it_added_a_vocab_and_was_disposed
      {
         readonly IDependencyInjectionContainer _second;

         public and_a_second_container_is_built_on_it() => _second = AContainerOnIt();

         [PCT] public void it_has_the_vocab_the_first_added() => _second.RootResolver.Resolve<ITaggregateStoreReader>().Get<StoredVocab>(VocabId).Question.Must().Be("taberu");
      }
   }
}
