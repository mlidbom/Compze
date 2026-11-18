using System.Transactions;
using Compze.Abstractions;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Hosting.Testing;
using Compze.Hosting.Testing.Wiring;
using Compze.Internals.Testing;
using Compze.Must;
using Compze.Tessaging.Engine.Wiring;
using Compze.Teventive.MemoryImageStateStore;
using Compze.Teventive.TaggregateStores;
using Compze.Tests.Infrastructure;
using Compze.Tests.Infrastructure.XUnit;
using static Compze.Must.MustActions;

// ReSharper disable InconsistentNaming for testing
#pragma warning disable IDE1006 //Reviewed OK: Test Naming Styles

namespace Compze.Tests.Integration.CQRS.MemoryImageStateStore;

///<summary>A memory image state store holding one vocab, "taberu", with an index whose key function breaks its contract: the keys it gives start<br/>
/// with a prefix that isn't the vocab's state, and that the specifications change between units of work.</summary>
public class Given_a_state_store_whose_index_key_function_reads_more_than_the_vocabs_state : UniversalTestBase
{
   readonly IDependencyInjectionContainer _container;
   readonly TaggregateId _vocabId = new(Guid.NewGuid());
   string _prefixOutsideTheVocab = "first:";

   protected Given_a_state_store_whose_index_key_function_reads_more_than_the_vocabs_state()
   {
      var builder = TestEnv.DIContainer.CreateTestingContainerBuilder();
      builder.Registrar.MemoryImageStateStore(store => store.Taggregate(() => new StoredVocab())
                                                 .Index<StoredVocab, string>(vocab => [_prefixOutsideTheVocab + vocab.Question]));
      builder.Registrar.LocalTessagingEngine(_ => {});
      _container = builder.Build();
      InUnitOfWork(taggregates => taggregates.Add(StoredVocab.Create(_vocabId, "taberu")));
   }

   void InUnitOfWork(Action<ITaggregateStoreUpdater> work) =>
      _container.ScopeFactory.ExecuteUnitOfWork(unitOfWork => work(unitOfWork.Resolve<ITaggregateStoreUpdater>()));

   ITaggregateStoreReader Reader => _container.RootResolver.Resolve<ITaggregateStoreReader>();

   IReadOnlyList<TaggregateId> IdsFiledUnder(string key) => [.._container.RootResolver.Resolve<ITaggregateIndex<StoredVocab, string>>().Lookup(key).Select(it => it.Id)];

   protected override async Task DisposeAsyncInternal() => await _container.DisposeAsync();

   public class when_the_prefix_changes_and_a_unit_of_work_then_changes_the_vocab : Given_a_state_store_whose_index_key_function_reads_more_than_the_vocabs_state
   {
      readonly Exception _exception;

      public when_the_prefix_changes_and_a_unit_of_work_then_changes_the_vocab()
      {
         _prefixOutsideTheVocab = "second:";
         _exception = Invoking(() => InUnitOfWork(taggregates => taggregates.GetForUpdate<StoredVocab>(_vocabId).ChangeMeaning("to dine")))
                     .Must().Throw<TransactionAbortedException>().Which;
      }

      [PCT] public void the_unit_of_work_rolls_back_saying_that_a_key_function_must_give_keys_from_the_taggregates_own_state() =>
         _exception.InnerException!.Message.Must().Contain("must give keys from the taggregate's own state alone");

      [PCT] public void the_reader_gets_the_meaning_committed_before() => Reader.Get<StoredVocab>(_vocabId).Meaning.Must().Be("");
      [PCT] public void the_index_still_files_the_vocab_under_the_key_it_was_filed_under() => IdsFiledUnder("first:taberu").Must().SequenceEqual([_vocabId]);
   }
}
