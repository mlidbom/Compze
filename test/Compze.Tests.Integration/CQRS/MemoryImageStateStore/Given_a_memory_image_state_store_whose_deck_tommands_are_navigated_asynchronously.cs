using System.Diagnostics;
using System.Text.Json;
using Compze.Abstractions;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Hosting.Testing;
using Compze.Hosting.Testing.Wiring;
using Compze.Internals.Testing;
using Compze.Must;
using Compze.Tessaging.Engine.Wiring;
using Compze.Tessaging.TessageTypes;
using Compze.Tessaging.Typermedia;
using Compze.Teventive.MemoryImageStateStore;
using Compze.Teventive.TaggregateStores;
using Compze.Tests.Infrastructure;
using Compze.Tests.Infrastructure.XUnit;
using Xunit;

// ReSharper disable InconsistentNaming for testing
#pragma warning disable IDE1006 //Reviewed OK: Test Naming Styles

namespace Compze.Tests.Integration.CQRS.MemoryImageStateStore;

///<summary>Spike (2026-11-26, dev_docs/async-local-navigators.md): a memory image state store kept in the domain database, whose<br/>
/// deck strictly local tommands change, navigated through <see cref="IIndependentLocalTypermediaNavigator.ExecuteAsync(IStrictlyLocalTommand)"/>.<br/>
/// One tommand's handler gets the deck for update, awaits, and then adds a card: its unit of work holds the deck across the await,<br/>
/// and goes on, and commits, on whichever thread the await resumes on. The other's handler adds the card without awaiting.</summary>
public class Given_a_memory_image_state_store_whose_deck_tommands_are_navigated_asynchronously : UniversalTestBase
{
   static readonly JsonSerializerOptions Options = new();
   const string DomainDatabase = "memory-image-state-store-navigated-asynchronously";
   static readonly TimeSpan HandlersAwait = TimeSpan.FromMilliseconds(10);

   readonly IDependencyInjectionContainer _container;
   protected TaggregateId DeckId { get; } = new(Guid.NewGuid());

   public Given_a_memory_image_state_store_whose_deck_tommands_are_navigated_asynchronously()
   {
      var builder = TestEnv.DIContainer.CreateTestingContainerBuilder();
      builder.Registrar.CurrentTestsDomainDatabaseWithTheMemoryImageStateStoresSqlLayer(DomainDatabase);
      builder.Registrar.MemoryImageStateStore(store => store.Taggregate(() => new StoredVocab())
                                                           .Taggregate(() => new StoredSentence())
                                                           .Taggregate(StoredDeck.Empty)
                                                           .StoredInTheDomainDatabase(Options, rows => rows.ClassNamed<StoredVocab>("vocab")
                                                                                                           .ClassNamed<StoredSentence>("sentence")
                                                                                                           .ClassNamed<StoredDeck>("deck")));
      builder.Registrar.LocalTessagingEngine(engine => engine.RegisterTypermediaHandlers(handle => handle
         .ForTommand(async (AddACardToTheDeckAfterAnAwait tommand, ITaggregateStoreUpdater taggregates) =>
          {
#pragma warning disable CA1849, VSTHRD103 // What this tommand's contexts specify: the synchronous get for update in an asynchronous handler that the analyzers warn of.
             var deck = taggregates.GetForUpdate<StoredDeck>(tommand.DeckId);
#pragma warning restore CA1849, VSTHRD103
             await Task.Delay(HandlersAwait);
             deck.AddCard(tommand.Text);
          })
         .ForTommand(async (AddACardToTheDeckGotForUpdateAsynchronouslyAfterAnAwait tommand, ITaggregateStoreUpdater taggregates) =>
          {
             var deck = await taggregates.GetForUpdateAsync<StoredDeck>(tommand.DeckId);
             await Task.Delay(HandlersAwait);
             deck.AddCard(tommand.Text);
          })
         .ForTommand((AddACardToTheDeck tommand, ITaggregateStoreUpdater taggregates) => taggregates.GetForUpdate<StoredDeck>(tommand.DeckId).AddCard(tommand.Text))));
      _container = builder.Build();
      _container.ScopeFactory.ExecuteUnitOfWork(unitOfWork => unitOfWork.Resolve<ITaggregateStoreUpdater>().Add(StoredDeck.Create(DeckId)));
   }

   IIndependentLocalTypermediaNavigator Navigator => _container.RootResolver.Resolve<IIndependentLocalTypermediaNavigator>();

   protected int CardsOfTheDeck => _container.RootResolver.Resolve<ITaggregateStoreReader>().Get<StoredDeck>(DeckId).Cards.InCreationOrder.Count;

   ///<summary>Four times as many navigations as the thread pool keeps threads at least.</summary>
   protected static int FourForEachProcessor => Environment.ProcessorCount * 4;

   ///<summary>Navigates the tommand <paramref name="tommandForTheCard"/> creates for each of <paramref name="cards"/> cards at once, each<br/>
   /// started on a thread-pool thread, as an ASP.NET Core route's is, and reports how long they took.</summary>
   protected async Task NavigateAtOnce(int cards, Func<string, IStrictlyLocalTommand> tommandForTheCard)
   {
      var stopwatch = Stopwatch.StartNew();
      await Task.WhenAll(Enumerable.Range(0, cards).Select(card => Task.Run(() => Navigator.ExecuteAsync(tommandForTheCard($"card {card}")))));
      ThreadPool.GetMinThreads(out var minimumWorkerThreads, out _);
      TestContext.Current.TestOutputHelper?.WriteLine($"{cards} {tommandForTheCard("").GetType().Name} at once: {stopwatch.ElapsedMilliseconds} ms; {Environment.ProcessorCount} processors, pool minimum {minimumWorkerThreads}, pool threads now {ThreadPool.ThreadCount}");
   }

   protected override async Task DisposeAsyncInternal() => await _container.DisposeAsync();

   public class after_one_tommand_whose_handler_awaits_while_it_holds_the_deck_adds_a_card : Given_a_memory_image_state_store_whose_deck_tommands_are_navigated_asynchronously
   {
      protected override async Task InitializeAsyncInternal()
      {
         await base.InitializeAsyncInternal();
         await NavigateAtOnce(1, text => new AddACardToTheDeckAfterAnAwait(DeckId, text));
      }

      [PCT] public void the_deck_has_the_card() => CardsOfTheDeck.Must().Be(1);
   }

   ///<summary>Each navigation but the one holding the deck waits for the deck's update lock synchronously, holding the thread it<br/>
   /// started on, while the one holding the deck awaits: its await resumes only on a thread the pool has to add. It fails: the<br/>
   /// navigations time out waiting for the deck after 30 seconds. Skipped: it starves the pool for a minute or more.</summary>
   public class after_four_tommands_for_each_processor_whose_handlers_await_while_they_hold_the_deck_add_a_card_at_once : Given_a_memory_image_state_store_whose_deck_tommands_are_navigated_asynchronously
   {
      protected override async Task InitializeAsyncInternal()
      {
         await base.InitializeAsyncInternal();
         await NavigateAtOnce(FourForEachProcessor, text => new AddACardToTheDeckAfterAnAwait(DeckId, text));
      }

      [PCT(Skip = "Spike: fails, timing out on the deck's update lock, and starves the thread pool meanwhile. Run it on its own.")] public void the_deck_has_every_card() => CardsOfTheDeck.Must().Be(FourForEachProcessor);
   }

   ///<summary>Each navigation but the one holding the deck waits for the deck's update lock holding no thread, through<br/>
   /// <see cref="ITaggregateStoreUpdater.GetForUpdateAsync{TTaggregate}"/>, so the one holding the deck finds a thread to resume on.</summary>
   public class after_four_tommands_for_each_processor_whose_handlers_get_the_deck_for_update_asynchronously_and_await_while_they_hold_it_add_a_card_at_once : Given_a_memory_image_state_store_whose_deck_tommands_are_navigated_asynchronously
   {
      protected override async Task InitializeAsyncInternal()
      {
         await base.InitializeAsyncInternal();
         await NavigateAtOnce(FourForEachProcessor, text => new AddACardToTheDeckGotForUpdateAsynchronouslyAfterAnAwait(DeckId, text));
      }

      [PCT] public void the_deck_has_every_card() => CardsOfTheDeck.Must().Be(FourForEachProcessor);
   }

   public class after_four_tommands_for_each_processor_whose_handlers_do_not_await_add_a_card_at_once : Given_a_memory_image_state_store_whose_deck_tommands_are_navigated_asynchronously
   {
      protected override async Task InitializeAsyncInternal()
      {
         await base.InitializeAsyncInternal();
         await NavigateAtOnce(FourForEachProcessor, text => new AddACardToTheDeck(DeckId, text));
      }

      [PCT] public void the_deck_has_every_card() => CardsOfTheDeck.Must().Be(FourForEachProcessor);
   }

   ///<summary>Adds a card to the deck <see cref="DeckId"/>, without awaiting.</summary>
   protected class AddACardToTheDeck : StrictlyLocal.Tommands.StrictlyLocalTommand
   {
      internal AddACardToTheDeck(TaggregateId deckId, string text)
      {
         DeckId = deckId;
         Text = text;
      }

      internal TaggregateId DeckId { get; }
      internal string Text { get; }
   }

   ///<summary>Adds a card to the deck <see cref="DeckId"/>, after its handler has got the deck for update through<br/>
   /// <see cref="ITaggregateStoreUpdater.GetForUpdateAsync{TTaggregate}"/> and awaited.</summary>
   protected class AddACardToTheDeckGotForUpdateAsynchronouslyAfterAnAwait : StrictlyLocal.Tommands.StrictlyLocalTommand
   {
      internal AddACardToTheDeckGotForUpdateAsynchronouslyAfterAnAwait(TaggregateId deckId, string text)
      {
         DeckId = deckId;
         Text = text;
      }

      internal TaggregateId DeckId { get; }
      internal string Text { get; }
   }

   ///<summary>Adds a card to the deck <see cref="DeckId"/>, after its handler has got the deck for update and awaited.</summary>
   protected class AddACardToTheDeckAfterAnAwait : StrictlyLocal.Tommands.StrictlyLocalTommand
   {
      internal AddACardToTheDeckAfterAnAwait(TaggregateId deckId, string text)
      {
         DeckId = deckId;
         Text = text;
      }

      internal TaggregateId DeckId { get; }
      internal string Text { get; }
   }
}
