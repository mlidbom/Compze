using System.Text.Json;
using Compze.Hosting.Testing;
using Compze.Hosting.Testing.Wiring;
using Compze.Internals.Testing;
using Compze.Must;
using Compze.Teventive.MemoryImageStateStore;
using Compze.Tests.Infrastructure;
using Compze.Tests.Infrastructure.XUnit;
using static Compze.Must.MustActions;

#pragma warning disable IDE1006 //Reviewed OK: Test Naming Styles

namespace Compze.Tests.Integration.CQRS.MemoryImageStateStore;

///<summary>What composing a memory image state store kept in the domain database's tables refuses, before anything runs.</summary>
public class When_a_memory_image_state_store_is_composed_to_be_kept_in_the_domain_database : UniversalTestBase
{
   static readonly JsonSerializerOptions Options = new();

   static Exception TheFailureComposing(Action<MemoryImageStateStoreBuilder> declare) =>
      Invoking(() => TestEnv.DIContainer.CreateTestingContainerBuilder().Registrar.MemoryImageStateStore(declare)).Must().Throw<Exception>().Which;

   public class keeping_vocabs_and_sentences_with_a_name_declared_only_for_the_vocabs : When_a_memory_image_state_store_is_composed_to_be_kept_in_the_domain_database
   {
      readonly Exception _thrown = TheFailureComposing(store => store.Taggregate(() => new StoredVocab())
                                                                     .Taggregate(() => new StoredSentence())
                                                                     .StoredInTheDomainDatabase(Options, rows => rows.ClassNamed<StoredVocab>("vocab")));

      [PCT] public void it_fails_naming_the_sentence_class_that_has_no_name() => _thrown.Message.Must().Contain(typeof(StoredSentence).FullName!);
   }

   public class declaring_the_same_name_for_the_vocabs_and_the_sentences : When_a_memory_image_state_store_is_composed_to_be_kept_in_the_domain_database
   {
      readonly Exception _thrown = TheFailureComposing(store => store.Taggregate(() => new StoredVocab())
                                                                     .Taggregate(() => new StoredSentence())
                                                                     .StoredInTheDomainDatabase(Options, rows => rows.ClassNamed<StoredVocab>("note")
                                                                                                                     .ClassNamed<StoredSentence>("note")));

      [PCT] public void it_fails_naming_both_classes_and_the_name() =>
         _thrown.Message.Must().Satisfy(it => it.Contains(typeof(StoredVocab).FullName!, StringComparison.Ordinal) && it.Contains(typeof(StoredSentence).FullName!, StringComparison.Ordinal) && it.Contains("\"note\"", StringComparison.Ordinal));
   }
}
