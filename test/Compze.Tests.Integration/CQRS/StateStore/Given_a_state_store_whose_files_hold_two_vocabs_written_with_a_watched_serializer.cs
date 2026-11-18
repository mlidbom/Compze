using System.Text.Json;
using Compze.Abstractions;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Hosting.Testing;
using Compze.Hosting.Testing.Wiring;
using Compze.Internals.Testing;
using Compze.Must;
using Compze.Tessaging.Engine.Wiring;
using Compze.Teventive.StateStore;
using Compze.Teventive.TaggregateStores;
using Compze.Tests.Infrastructure;
using Compze.Tests.Infrastructure.XUnit;
using static Compze.Must.MustActions;

// ReSharper disable InconsistentNaming for testing
#pragma warning disable IDE1006 //Reviewed OK: Test Naming Styles

namespace Compze.Tests.Integration.CQRS.StateStore;

///<summary>A state store kept in JSON files in memory, whose files hold two vocabs, "taberu" and "nomu", and which has been used once.<br/>
/// The consumer's JSON options serialize taggregate ids with a <see cref="WatchedTaggregateIdConverter"/>, so a specification sees whose<br/>
/// snapshots the store serializes, and can have one refused, as a snapshot that can't be serialized.</summary>
public class Given_a_state_store_whose_files_hold_two_vocabs_written_with_a_watched_serializer : UniversalTestBase
{
   readonly DirectoryInfo _folder = new(Path.Combine(Path.GetTempPath(), "compze-state-store-files-in-memory", Guid.NewGuid().ToString("N")));
   readonly WatchedTaggregateIdConverter _converter = new();
   readonly JsonSerializerOptions _options;
   readonly IDependencyInjectionContainer _container;
   TaggregateId TaberuId { get; } = new(Guid.NewGuid());
   TaggregateId NomuId { get; } = new(Guid.NewGuid());

   protected Given_a_state_store_whose_files_hold_two_vocabs_written_with_a_watched_serializer()
   {
      _options = new JsonSerializerOptions { WriteIndented = true, Converters = { _converter } };
      var builder = TestEnv.DIContainer.CreateTestingContainerBuilder();
      builder.Registrar.StateStore(store => store.Taggregate(() => new StoredVocab())
                                                 .StoredInFiles(_folder, _options, files => files.Of<StoredVocab>("vocab").KeptInMemory()));
      builder.Registrar.LocalTessagingEngine(_ => {});
      _container = builder.Build();
      WriteTheVocabsFile(TaberuId, "taberu", "to eat");
      WriteTheVocabsFile(NomuId, "nomu", "to drink");
      _ = Reader.Get<StoredVocab>(TaberuId);
      _converter.ForgetWhatWasWritten();
   }

   ITaggregateStoreReader Reader => _container.RootResolver.Resolve<ITaggregateStoreReader>();

   IStateStore StateStore => _container.RootResolver.Resolve<IStateStore>();

   ITaggregateFilesInMemory Files => _container.RootResolver.Resolve<ITaggregateFilesInMemory>();

   void InUnitOfWork(Action<ITaggregateStoreUpdater> work) =>
      _container.ScopeFactory.ExecuteUnitOfWork(unitOfWork => work(unitOfWork.Resolve<ITaggregateStoreUpdater>()));

   string VocabFile(TaggregateId id) => Path.Combine(_folder.FullName, "vocab", id.Value.ToString("N")[..2], $"{id.Value}.json");

   ///<summary>Writes a vocab's file behind the store's back, as another program would, then forgets that the converter wrote its id.</summary>
   void WriteTheVocabsFile(TaggregateId id, string question, string meaning)
   {
      Files.WriteAllText(VocabFile(id), JsonSerializer.Serialize(new StoredVocabSnapshot(id, question, meaning), _options));
      _converter.ForgetWhatWasWritten();
   }

   ///<summary>Every file and its text, sorted by path.</summary>
   IReadOnlyList<(string Path, string Text)> EveryFileAndItsText => [..Files.Paths.Order(StringComparer.OrdinalIgnoreCase).Select(it => (it, Files.ReadAllText(it)))];

   protected override async Task DisposeAsyncInternal() => await _container.DisposeAsync();

   public class when_the_store_reloads_files_that_no_other_program_has_changed : Given_a_state_store_whose_files_hold_two_vocabs_written_with_a_watched_serializer
   {
      readonly ReloadedTaggregates _reloaded;

      public when_the_store_reloads_files_that_no_other_program_has_changed() => _reloaded = StateStore.ReloadFromStorage();

      [PCT] public void the_reload_serializes_no_snapshot() => _converter.Written.Must().BeEmpty();
      [PCT] public void the_reload_reports_nothing_changed() => _reloaded.Changed.Must().BeEmpty();
   }

   public class after_a_unit_of_work_changes_a_vocab_and_the_store_reloads : Given_a_state_store_whose_files_hold_two_vocabs_written_with_a_watched_serializer
   {
      readonly ReloadedTaggregates _reloaded;

      public after_a_unit_of_work_changes_a_vocab_and_the_store_reloads()
      {
         InUnitOfWork(taggregates => taggregates.GetForUpdate<StoredVocab>(TaberuId).ChangeMeaning("to dine"));
         _converter.ForgetWhatWasWritten();
         _reloaded = StateStore.ReloadFromStorage();
      }

      [PCT] public void the_reload_serializes_no_snapshot_since_the_store_wrote_the_file_itself() => _converter.Written.Must().BeEmpty();
      [PCT] public void the_reload_reports_nothing_changed() => _reloaded.Changed.Must().BeEmpty();
   }

   public class after_another_program_changes_one_vocabs_file_and_the_store_reloads : Given_a_state_store_whose_files_hold_two_vocabs_written_with_a_watched_serializer
   {
      readonly ReloadedTaggregates _reloaded;

      public after_another_program_changes_one_vocabs_file_and_the_store_reloads()
      {
         WriteTheVocabsFile(TaberuId, "taberu", "to eat, quickly");
         _reloaded = StateStore.ReloadFromStorage();
      }

      [PCT] public void the_reload_serializes_the_changed_vocabs_snapshots_alone() => _converter.Written.Distinct().Must().SequenceEqual([TaberuId]);
      [PCT] public void the_reload_reports_exactly_that_vocab_changed() => _reloaded.Changed.Must().SequenceEqual([TaberuId]);
   }

   public class when_a_snapshot_cant_be_serialized_as_the_store_writes_every_taggregate_anew : Given_a_state_store_whose_files_hold_two_vocabs_written_with_a_watched_serializer
   {
      readonly IReadOnlyList<(string Path, string Text)> _filesBefore;
      readonly Exception _thrown;

      public when_a_snapshot_cant_be_serialized_as_the_store_writes_every_taggregate_anew()
      {
         _filesBefore = EveryFileAndItsText;
         _converter.RefuseToWrite(NomuId);
         _thrown = Invoking(() => StateStore.WriteEverythingAnew()).Must().Throw<Exception>().Which;
      }

      [PCT] public void the_serializers_failure_comes_out() => _thrown.Message.Must().Contain($"refuses to have taggregate {NomuId} written");
      [PCT] public void every_file_is_as_before_and_no_temporary_file_is_left() => EveryFileAndItsText.Must().SequenceEqual(_filesBefore);
      [PCT] public void the_reader_gets_both_vocabs_as_before() =>
         new[] { Reader.Get<StoredVocab>(TaberuId).Meaning, Reader.Get<StoredVocab>(NomuId).Meaning }.Must().SequenceEqual(["to eat", "to drink"]);
   }
}
