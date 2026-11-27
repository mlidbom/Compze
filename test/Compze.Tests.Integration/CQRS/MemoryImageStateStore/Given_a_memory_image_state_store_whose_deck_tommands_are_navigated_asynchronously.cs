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

#pragma warning disable IDE1006 //Reviewed OK: Test Naming Styles

namespace Compze.Tests.Integration.CQRS.MemoryImageStateStore;

///<summary>A memory image state store kept in the domain database, whose deck a strictly local tommand changes, navigated through<br/>
/// <see cref="IIndependentLocalTypermediaNavigator.ExecuteAsync(IStrictlyLocalTommand)"/>. Its handler gets the deck for update, awaits,<br/>
/// and then adds a card: the navigation's unit of work holds the deck across the await, goes on on whichever thread the await resumes<br/>
/// on, and commits there.</summary>
public class Given_a_memory_image_state_store_whose_deck_tommands_are_navigated_asynchronously : UniversalTestBase
{
   static readonly JsonSerializerOptions Options = new();
   const string DomainDatabase = "memory-image-state-store-navigated-asynchronously";

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
             var deck = await taggregates.GetForUpdateAsync<StoredDeck>(tommand.DeckId);
             await Task.Delay(TimeSpan.FromMilliseconds(10));
             deck.AddCard(tommand.Text);
          })));
      _container = builder.Build();
      _container.ScopeFactory.ExecuteUnitOfWork(unitOfWork => unitOfWork.Resolve<ITaggregateStoreUpdater>().Add(StoredDeck.Create(DeckId)));
   }

   protected IIndependentLocalTypermediaNavigator Navigator => _container.RootResolver.Resolve<IIndependentLocalTypermediaNavigator>();

   protected int CardsOfTheDeck => _container.RootResolver.Resolve<ITaggregateStoreReader>().Get<StoredDeck>(DeckId).Cards.InCreationOrder.Count;

   protected override async Task DisposeAsyncInternal() => await _container.DisposeAsync();

   public class after_one_tommand_whose_handler_awaits_while_it_holds_the_deck_adds_a_card : Given_a_memory_image_state_store_whose_deck_tommands_are_navigated_asynchronously
   {
      protected override async Task InitializeAsyncInternal()
      {
         await base.InitializeAsyncInternal();
         await Task.Run(() => Navigator.ExecuteAsync(new AddACardToTheDeckAfterAnAwait(DeckId, "the card")));
      }

      [PCT] public void the_deck_has_the_card() => CardsOfTheDeck.Must().Be(1);
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
