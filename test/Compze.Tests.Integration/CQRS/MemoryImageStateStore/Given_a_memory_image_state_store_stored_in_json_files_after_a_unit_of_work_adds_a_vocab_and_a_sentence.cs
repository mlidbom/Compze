using System.Text.Json;
using Compze.Abstractions;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Hosting.Testing;
using Compze.Hosting.Testing.Wiring;
using Compze.Internals.Testing;
using Compze.Must;
using Compze.Tessaging.Engine.Wiring;
using Compze.Teventive;
using Compze.Teventive.MemoryImageStateStore;
using Compze.Teventive.MemoryImageStateStore.Testing;
using Compze.Teventive.TaggregateStores;
using Compze.Tests.Infrastructure;
using Compze.Tests.Infrastructure.XUnit;
using static Compze.Must.MustActions;

// ReSharper disable InconsistentNaming for testing
#pragma warning disable IDE1006 //Reviewed OK: Test Naming Styles

namespace Compze.Tests.Integration.CQRS.MemoryImageStateStore;

///<summary>The <see cref="ITaggregateJsonFiles"/> of a memory image state store keeping vocabs in "vocab" and sentences in "sentences", in a<br/>
/// fresh folder of its own, after a unit of work has added a vocab, "taberu", and a sentence: on the disk, and on a file system in memory.<br/>
/// What the store wrote is specified on both. How the paths are read is the same on either, and is specified on a file system in memory.</summary>
public abstract class Given_a_memory_image_state_store_stored_in_json_files_after_a_unit_of_work_adds_a_vocab_and_a_sentence : UniversalTestBase
{
   static readonly JsonSerializerOptions Options = new() { WriteIndented = true };
   readonly DirectoryInfo _folder = new(Path.Combine(Path.GetTempPath(), "compze-memory-image-state-store-json-files", Guid.NewGuid().ToString("N")));
   readonly IComponentRegistrar _registrar;
   readonly IDependencyInjectionContainer _container;

   Given_a_memory_image_state_store_stored_in_json_files_after_a_unit_of_work_adds_a_vocab_and_a_sentence(bool onAFileSystemInMemory)
   {
      var builder = TestEnv.DIContainer.CreateTestingContainerBuilder();
      builder.Registrar.MemoryImageStateStore(store => store.Taggregate(() => new StoredVocab())
                                                 .Taggregate(() => new StoredSentence())
                                                 .StoredInJsonFiles(_folder, Options, files =>
                                                  {
                                                     if(onAFileSystemInMemory) files.OnAFileSystemInMemory();
                                                     files.FolderOf<StoredVocab>("vocab")
                                                          .FolderOf<StoredSentence>("sentences");
                                                  }));
      builder.Registrar.LocalTessagingEngine(_ => {});
      _registrar = builder.Registrar;
      _container = builder.Build();
      InUnitOfWork(taggregates =>
      {
         taggregates.Add(StoredVocab.Create(VocabId, "taberu"));
         taggregates.Add(StoredSentence.Create(SentenceId, "taberu yo"));
      });
   }

   TaggregateId VocabId { get; } = new(Guid.NewGuid());

   TaggregateId SentenceId { get; } = new(Guid.NewGuid());

   ITaggregateJsonFiles JsonFiles => _container.RootResolver.Resolve<ITaggregateJsonFiles>();

   ITaggregateStoreReader Reader => _container.RootResolver.Resolve<ITaggregateStoreReader>();

   ///<summary>The vocab and the sentence the store holds, each as an <see cref="ITaggregate"/>, as code that holds taggregates of several<br/>
   /// classes through a type they share holds them.</summary>
   IReadOnlyList<ITaggregate> TheVocabAndTheSentenceAsTaggregates => [Reader.Get<StoredVocab>(VocabId), Reader.Get<StoredSentence>(SentenceId)];

   ///<summary>The paths <see cref="ITaggregateJsonFiles.PathOf{TTaggregate}(TaggregateId)"/> gives the vocab's class and id, and the sentence's.</summary>
   IReadOnlyList<string> PathsOfTheVocabAndTheSentenceByClassAndId => [JsonFiles.PathOf<StoredVocab>(VocabId), JsonFiles.PathOf<StoredSentence>(SentenceId)];

   void InUnitOfWork(Action<ITaggregateStoreUpdater> work) =>
      _container.ScopeFactory.ExecuteUnitOfWork(unitOfWork => work(unitOfWork.Resolve<ITaggregateStoreUpdater>()));

   ///<summary>The full path of every file in the class folder <paramref name="classFolder"/>, at any depth, as the file system lists them.</summary>
   private protected abstract IReadOnlyList<string> FilesIn(string classFolder);

   private protected abstract string ReadAllText(string path);

   ///<summary>The class and id of the taggregate whose file <paramref name="path"/> is, as data a failed assertion shows, or null.</summary>
   object? TaggregateWhoseFileIs(string path) => JsonFiles.TaggregateWhoseFileIs(path) is {} it ? new { Class = it.Class.FullName, it.Id.Value } : null;

   object TheVocab => new { Class = typeof(StoredVocab).FullName, VocabId.Value };

   object TheSentence => new { Class = typeof(StoredSentence).FullName, SentenceId.Value };

   static string SerializedVocab(TaggregateId id, string question, string meaning) => JsonSerializer.Serialize(new StoredVocabSnapshot(id, question, meaning), Options);

   ///<summary>The file of the taggregate <paramref name="id"/> in the layout, &lt;first two hex digits of the id&gt;/&lt;id&gt;.json, under <paramref name="classFolder"/>.</summary>
   static string FileInTheLayout(string classFolder, TaggregateId id) => Path.Combine(classFolder, id.Value.ToString("N")[..2], $"{id.Value}.json");

   string ClassFolder(string name) => Path.Combine(_folder.FullName, name);

   protected override async Task DisposeAsyncInternal()
   {
      await _container.DisposeAsync();
      if(_folder.Exists) _folder.Delete(recursive: true);
   }

   public class on_the_disk() : Given_a_memory_image_state_store_stored_in_json_files_after_a_unit_of_work_adds_a_vocab_and_a_sentence(onAFileSystemInMemory: false)
   {
      private protected override IReadOnlyList<string> FilesIn(string classFolder) =>
         Directory.Exists(ClassFolder(classFolder)) ? [..Directory.EnumerateFiles(ClassFolder(classFolder), "*", SearchOption.AllDirectories)] : [];

      private protected override string ReadAllText(string path) => File.ReadAllText(path);

      [PCT] public void ITaggregateJsonFiles_is_registered() => _registrar.IsRegistered<ITaggregateJsonFiles>().Must().BeTrue();

      [PCT] public void the_file_at_the_vocabs_PathOf_holds_what_the_consumers_options_make_of_its_snapshot() =>
         ReadAllText(JsonFiles.PathOf<StoredVocab>(VocabId)).Must().Be(SerializedVocab(VocabId, "taberu", ""));

      [PCT] public void TaggregateWhoseFileIs_each_file_in_the_vocab_folder_gives_the_vocab_alone() => FilesIn("vocab").Select(TaggregateWhoseFileIs).Must().SequenceEqual([TheVocab]);

      [PCT] public void TaggregateWhoseFileIs_each_file_in_the_sentences_folder_gives_the_sentence_alone() => FilesIn("sentences").Select(TaggregateWhoseFileIs).Must().SequenceEqual([TheSentence]);

      [PCT] public void PathOf_the_vocab_and_the_sentence_each_passed_as_an_ITaggregate_is_PathOf_its_class_and_id() =>
         TheVocabAndTheSentenceAsTaggregates.Select(it => JsonFiles.PathOf(it)).Must().SequenceEqual(PathsOfTheVocabAndTheSentenceByClassAndId);

      public sealed class and_a_unit_of_work_deletes_the_vocab : on_the_disk
      {
         readonly string _vocabsFile;

         public and_a_unit_of_work_deletes_the_vocab()
         {
            _vocabsFile = FilesIn("vocab").Single();
            InUnitOfWork(taggregates => taggregates.GetForUpdate<StoredVocab>(VocabId).Delete());
         }

         [PCT] public void the_vocab_folder_holds_no_file() => FilesIn("vocab").Must().BeEmpty();

         [PCT] public void TaggregateWhoseFileIs_the_deleted_file_is_still_the_vocab() => TaggregateWhoseFileIs(_vocabsFile).Must().DeepEqual(TheVocab);
      }
   }

   public class on_a_file_system_in_memory() : Given_a_memory_image_state_store_stored_in_json_files_after_a_unit_of_work_adds_a_vocab_and_a_sentence(onAFileSystemInMemory: true)
   {
      IFileSystemInMemory FileSystem => _container.RootResolver.Resolve<IFileSystemInMemory>();

      private protected override IReadOnlyList<string> FilesIn(string classFolder) =>
         [..FileSystem.Paths.Where(it => it.StartsWith(ClassFolder(classFolder) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))];

      private protected override string ReadAllText(string path) => FileSystem.ReadAllText(path);

      TaggregateId AVocabTheStoreDoesNotHold { get; } = new(Guid.NewGuid());

      [PCT] public void ITaggregateJsonFiles_is_registered() => _registrar.IsRegistered<ITaggregateJsonFiles>().Must().BeTrue();

      [PCT] public void the_file_at_the_vocabs_PathOf_holds_what_the_consumers_options_make_of_its_snapshot() =>
         ReadAllText(JsonFiles.PathOf<StoredVocab>(VocabId)).Must().Be(SerializedVocab(VocabId, "taberu", ""));

      [PCT] public void TaggregateWhoseFileIs_each_file_in_the_vocab_folder_gives_the_vocab_alone() => FilesIn("vocab").Select(TaggregateWhoseFileIs).Must().SequenceEqual([TheVocab]);

      [PCT] public void TaggregateWhoseFileIs_each_file_in_the_sentences_folder_gives_the_sentence_alone() => FilesIn("sentences").Select(TaggregateWhoseFileIs).Must().SequenceEqual([TheSentence]);

      [PCT] public void PathOf_the_vocab_and_the_sentence_each_passed_as_an_ITaggregate_is_PathOf_its_class_and_id() =>
         TheVocabAndTheSentenceAsTaggregates.Select(it => JsonFiles.PathOf(it)).Must().SequenceEqual(PathsOfTheVocabAndTheSentenceByClassAndId);

      [PCT] public void nothing_is_written_to_the_disk() => Directory.Exists(_folder.FullName).Must().BeFalse();

      [PCT] public void PathOf_a_vocab_the_store_does_not_hold_is_its_file_in_the_layout_under_the_vocab_folder() =>
         JsonFiles.PathOf<StoredVocab>(AVocabTheStoreDoesNotHold).Must().Be(FileInTheLayout(ClassFolder("vocab"), AVocabTheStoreDoesNotHold));

      [PCT] public void there_is_no_file_at_PathOf_a_vocab_the_store_does_not_hold() =>
         FileSystem.Paths.Must().Satisfy(it => !it.Contains(JsonFiles.PathOf<StoredVocab>(AVocabTheStoreDoesNotHold), StringComparer.OrdinalIgnoreCase));

      [PCT] public void TaggregateWhoseFileIs_PathOf_a_vocab_the_store_does_not_hold_is_that_vocab() =>
         TaggregateWhoseFileIs(JsonFiles.PathOf<StoredVocab>(AVocabTheStoreDoesNotHold)).Must().DeepEqual(new { Class = typeof(StoredVocab).FullName, AVocabTheStoreDoesNotHold.Value });

      [PCT] public void TaggregateWhoseFileIs_the_vocabs_path_in_upper_case_is_the_vocab() =>
         TaggregateWhoseFileIs(JsonFiles.PathOf<StoredVocab>(VocabId).ToUpperInvariant()).Must().DeepEqual(TheVocab);

      [PCT] public void TaggregateWhoseFileIs_the_vocabs_path_with_forward_slashes_is_the_vocab() =>
         TaggregateWhoseFileIs(JsonFiles.PathOf<StoredVocab>(VocabId).Replace(Path.DirectorySeparatorChar, '/')).Must().DeepEqual(TheVocab);

      [PCT] public void TaggregateWhoseFileIs_the_vocabs_file_name_in_its_layout_under_the_stores_own_folder_is_null() =>
         TaggregateWhoseFileIs(FileInTheLayout(ClassFolder(".state-store"), VocabId)).Must().BeNull();

      [PCT] public void TaggregateWhoseFileIs_a_text_file_in_the_vocab_folder_is_null() => TaggregateWhoseFileIs(Path.Combine(ClassFolder("vocab"), "notes.txt")).Must().BeNull();

      [PCT] public void TaggregateWhoseFileIs_a_json_file_in_the_vocab_folder_not_named_by_an_id_is_null() =>
         TaggregateWhoseFileIs(Path.Combine(ClassFolder("vocab"), VocabId.Value.ToString("N")[..2], "notes.json")).Must().BeNull();

      [PCT] public void TaggregateWhoseFileIs_the_vocabs_file_name_directly_in_the_vocab_folder_is_null() =>
         TaggregateWhoseFileIs(Path.Combine(ClassFolder("vocab"), $"{VocabId.Value}.json")).Must().BeNull();

      [PCT] public void TaggregateWhoseFileIs_the_vocabs_file_in_its_layout_under_a_folder_outside_the_store_is_null() =>
         TaggregateWhoseFileIs(FileInTheLayout(Path.Combine(Path.GetTempPath(), "another-folder", "vocab"), VocabId)).Must().BeNull();

      [PCT] public void TaggregateWhoseFileIs_a_relative_path_throws() => Invoking(() => JsonFiles.TaggregateWhoseFileIs(FileInTheLayout("vocab", VocabId))).Must().Throw<Exception>();

      [PCT] public void PathOf_a_class_the_store_does_not_keep_throws_naming_the_class() =>
         Invoking(() => JsonFiles.PathOf<StoredDeck>(TaggregateId.New())).Must().Throw<Exception>().Which.Message.Must().Contain(typeof(StoredDeck).FullName!);

      [PCT] public void PathOf_a_taggregate_passed_as_an_ITaggregate_of_a_class_the_store_does_not_keep_throws_naming_the_class() =>
         Invoking(() => JsonFiles.PathOf(ADeckAsATaggregate)).Must().Throw<Exception>().Which.Message.Must().Contain(typeof(StoredDeck).FullName!);

      [PCT] public void PathOf_a_taggregate_passed_as_an_ITaggregate_of_a_class_derived_from_a_class_the_store_keeps_throws_naming_the_class() =>
         Invoking(() => JsonFiles.PathOf(ADerivedVocabAsATaggregate)).Must().Throw<Exception>().Which.Message.Must().Contain(typeof(DerivedStoredVocab).FullName!);

      ///<summary>A new <see cref="StoredDeck"/>, a class the store doesn't keep, as an <see cref="ITaggregate"/>.</summary>
      static ITaggregate ADeckAsATaggregate => StoredDeck.Create(new TaggregateId(Guid.NewGuid()));

      ///<summary>A new <see cref="DerivedStoredVocab"/>, of a class derived from <see cref="StoredVocab"/>, which the store keeps, as an<br/>
      /// <see cref="ITaggregate"/>.</summary>
      static ITaggregate ADerivedVocabAsATaggregate => DerivedStoredVocab.Create(new TaggregateId(Guid.NewGuid()));

      ///<summary>A vocab of a class derived from <see cref="StoredVocab"/>: a store keeps exactly the classes declared for it, so a store<br/>
      /// that keeps <see cref="StoredVocab"/> doesn't keep this one.</summary>
      sealed class DerivedStoredVocab : StoredVocab
      {
         static readonly TeventRouting<DerivedStoredVocab, IStoredVocabTevent> DerivedAppliers = Appliers.Extend<DerivedStoredVocab>(_ => {});

         protected override TeventRouting TeventAppliers => DerivedAppliers;

         internal static DerivedStoredVocab Create(TaggregateId id)
         {
            var vocab = new DerivedStoredVocab();
            vocab.Publish(new StoredVocabCreated(id, "taberu"));
            return vocab;
         }
      }

      public sealed class and_a_unit_of_work_deletes_the_vocab : on_a_file_system_in_memory
      {
         readonly string _vocabsFile;

         public and_a_unit_of_work_deletes_the_vocab()
         {
            _vocabsFile = FilesIn("vocab").Single();
            InUnitOfWork(taggregates => taggregates.GetForUpdate<StoredVocab>(VocabId).Delete());
         }

         [PCT] public void the_vocab_folder_holds_no_file() => FilesIn("vocab").Must().BeEmpty();

         [PCT] public void TaggregateWhoseFileIs_the_deleted_file_is_still_the_vocab() => TaggregateWhoseFileIs(_vocabsFile).Must().DeepEqual(TheVocab);
      }

      public class when_TaggregateWhoseFileIs_is_asked_for_the_vocabs_path_and_for_it_in_upper_case : on_a_file_system_in_memory
      {
         readonly TaggregateOfAFile _fromThePath;
         readonly TaggregateOfAFile _fromThePathInUpperCase;
         readonly TaggregateOfAFile _fromTheSentencesPath;

         public when_TaggregateWhoseFileIs_is_asked_for_the_vocabs_path_and_for_it_in_upper_case()
         {
            _fromThePath = JsonFiles.TaggregateWhoseFileIs(JsonFiles.PathOf<StoredVocab>(VocabId))!;
            _fromThePathInUpperCase = JsonFiles.TaggregateWhoseFileIs(JsonFiles.PathOf<StoredVocab>(VocabId).ToUpperInvariant())!;
            _fromTheSentencesPath = JsonFiles.TaggregateWhoseFileIs(JsonFiles.PathOf<StoredSentence>(SentenceId))!;
         }

         [PCT] public void the_two_are_equal_by_Equals_and_by_the_equality_operator_and_have_the_same_hash_code() =>
            new { ByEquals = _fromThePath.Equals(_fromThePathInUpperCase), ByOperator = _fromThePath == _fromThePathInUpperCase, SameHashCode = _fromThePath.GetHashCode() == _fromThePathInUpperCase.GetHashCode() }
              .Must().DeepEqual(new { ByEquals = true, ByOperator = true, SameHashCode = true });

         [PCT] public void the_vocabs_and_the_sentences_are_not_equal_by_the_inequality_operator() => (_fromThePath != _fromTheSentencesPath).Must().BeTrue();
      }
   }
}
