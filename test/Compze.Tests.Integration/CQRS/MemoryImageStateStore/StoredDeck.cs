using Compze.Abstractions;
using Compze.Teventive;
using Compze.Teventive.Taggregates;
using Compze.Teventive.Taggregates.BaseClasses;
using Compze.Teventive.Taggregates.Tevents;
using JetBrains.Annotations;

namespace Compze.Tests.Integration.CQRS.MemoryImageStateStore;
#pragma warning disable CA1812 //Uninstantiated class (used via reflection)

///<summary>A deck of cards: a taggregate with entities, which its restore from a snapshot adds without their tevents.</summary>
class StoredDeck : Taggregate<StoredDeck, IStoredDeckTevent, StoredDeckTevent, IStoredDeckTevent<IStoredDeckTevent>, StoredDeckTevent<StoredDeckTevent>>,
                   ISnapshotable<StoredDeck, StoredDeckSnapshot>
{
   static readonly TeventRouting<StoredDeck, IStoredDeckTevent> Appliers = DeclareAppliers(it => it
     .IgnoreUnhandled<IStoredDeckTevent.Created>()
     .Entities(static deck => deck._cards));

   protected override TeventRouting TeventAppliers => Appliers;

   readonly StoredCard.ICollectionManager _cards;

   StoredDeck() => _cards = StoredCard.CreateSelfManagingCollection(this);

   public IReadOnlyEntityCollection<StoredCard, Guid> Cards => _cards.Entities;

   public static StoredDeck Create(TaggregateId id)
   {
      var deck = new StoredDeck();
      deck.Publish(new StoredDeckTevent.Created(id));
      return deck;
   }

   ///<summary>A new, empty instance, to restore from a snapshot.</summary>
   internal static StoredDeck Empty() => new();

   public StoredCard AddCard(string text) => _cards.AddByPublishing(new StoredDeckTevent.Card.Added(Guid.NewGuid(), text));

   ///<summary>Adds a card without its tevent, as a restore does: for the specification that only a restore may.</summary>
   internal StoredCard AddARestoredCard() => _cards.AddWhileRestoring(Guid.NewGuid());

   public StoredDeckSnapshot TakeSnapshot() => new(Id, [..Cards.InCreationOrder.Select(it => it.TakeSnapshot())]);

   public void RestoreFrom(StoredDeckSnapshot snapshot)
   {
      RestoreIdentity(snapshot.Id);
      foreach(var card in snapshot.Cards) _cards.AddWhileRestoring(card.Id).RestoreFrom(card);
   }
}

///<summary>A card of a <see cref="StoredDeck"/>.</summary>
class StoredCard : StoredDeck.Entity<StoredCard, Guid, IStoredDeckTevent.Card, StoredDeckTevent.Card, IStoredDeckTevent.Card.Added, StoredDeckTevent.Card.IdGetterSetter>
{
   static readonly TeventRouting<StoredCard, IStoredDeckTevent.Card> CardAppliers = DeclareAppliers(it => it
     .For<IStoredDeckTevent.Card.Added>(static (card, tevent) => card.Text = tevent.Text));

   protected override TeventRouting TeventAppliers => CardAppliers;

   public StoredCard(StoredDeck deck) : base(deck) {}

   public string Text { get; private set; } = "";

   internal StoredCardSnapshot TakeSnapshot() => new(Id, Text);
   internal void RestoreFrom(StoredCardSnapshot snapshot) => Text = snapshot.Text;
}

class StoredDeckSnapshot(TaggregateId id, IReadOnlyList<StoredCardSnapshot> cards)
{
   public TaggregateId Id { get; } = id;
   public IReadOnlyList<StoredCardSnapshot> Cards { get; } = cards;
}

class StoredCardSnapshot(Guid id, string text)
{
   public Guid Id { get; } = id;
   public string Text { get; } = text;
}

interface IStoredDeckTevent<out T> : ITaggregateTevent<T> where T : IStoredDeckTevent;

interface IStoredDeckTevent : ITaggregateTevent
{
#pragma warning disable CA1715 // Nested tevent interfaces follow the semantic events naming convention
   interface Created : IStoredDeckTevent, ITaggregateCreatedTevent;

   interface Card : IStoredDeckTevent
   {
      Guid CardId { get; }

      interface Added : Card
      {
         string Text { get; }
      }
   }
#pragma warning restore CA1715
}

[UsedImplicitly] //Built by reflection, around each tevent the taggregate publishes.
class StoredDeckTevent<T>(T tevent) : TaggregateTevent<T>(tevent), IStoredDeckTevent<T> where T : IStoredDeckTevent;

abstract class StoredDeckTevent : TaggregateTevent, IStoredDeckTevent
{
   protected StoredDeckTevent() {}
   protected StoredDeckTevent(TaggregateId taggregateId) : base(taggregateId) {}

   internal sealed class Created(TaggregateId id) : StoredDeckTevent(id), IStoredDeckTevent.Created;

   internal abstract class Card : StoredDeckTevent, IStoredDeckTevent.Card
   {
      public Guid CardId { get; private set; }

      [UsedImplicitly] internal sealed class IdGetterSetter : IGetSetTaggregateEntityTeventEntityId<Guid, Card, IStoredDeckTevent.Card>
      {
         public void SetEntityId(Card tevent, Guid id) => tevent.CardId = id;
         public Guid GetId(IStoredDeckTevent.Card tevent) => tevent.CardId;
      }

      internal sealed class Added : Card, IStoredDeckTevent.Card.Added
      {
         public Added(Guid cardId, string text)
         {
            CardId = cardId;
            Text = text;
         }

         public string Text { get; }
      }
   }
}
