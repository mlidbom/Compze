using System;
using System.Collections.Generic;
using System.Linq;
using Compze.Contracts;
using Compze.Functional;
using Compze.Persistence.EventStore.PersistenceLayer;
using Compze.SystemCE.CollectionsCE.GenericCE;
using Compze.SystemCE.LinqCE;

namespace Compze.Persistence.EventStore.Refactoring.Migrations;

//Yes this class has quite a bit of code that looks overly complex. Like it could be simplified a lot.
//What you are seeing is likely optimizations. Please do not change this class for reasons of readability unless you do thorough performance testing and verify that no performance is lost.
//The performance of this class is extremely important since it is called at least once for every event that is loaded from the event store when you have any migrations activated. It is called A LOT.
//This is one of those central classes for which optimization is actually vitally important.
//Each of the optimizations were done with the help of a profiler and running benchmarks on the tested performance improvements time and time again.
//Performance: Consider whether using the new stackalloc and Range types might allow us to improve performance of migrations.
class EventModifier(Action<IReadOnlyList<EventModifier.RefactoredEvent>> eventsAddedCallback) : IEventModifier
{
   internal class RefactoredEvent(AggregateEvent newEvent, AggregateEventStorageInformation storageInformation)
   {
      public AggregateEvent NewEvent { get; private set; } = newEvent;
      public AggregateEventStorageInformation StorageInformation { get; private set; } = storageInformation;
   }

   readonly Action<IReadOnlyList<RefactoredEvent>> _eventsAddedCallback = eventsAddedCallback;
   internal LinkedList<AggregateEvent>? Events;
   RefactoredEvent[]? _replacementEvents;
   RefactoredEvent[]? _insertedEvents;

   AggregateEvent? _inspectedEvent;

   LinkedListNode<AggregateEvent>? _currentNode;
   AggregateEvent? _lastEventInActualStream;

   LinkedListNode<AggregateEvent> CurrentNode
   {
      get
      {
         if (_currentNode == null)
         {
            Events = [];
            _currentNode = Events.AddFirst(_inspectedEvent!);
         }
         return _currentNode;
      }
      set
      {
         _currentNode = value;
         _inspectedEvent = _currentNode.Value;
      }
   }

   void AssertNoPriorModificationsHaveBeenMade()
   {
      if(_replacementEvents != null || _insertedEvents != null)
      {
         throw new Exception("You can only modify the current event once.");
      }

   }

   public void Replace(params AggregateEvent[] replacementEvents)
   {
      AssertNoPriorModificationsHaveBeenMade();
      if(_inspectedEvent is EndOfAggregateHistoryEventPlaceHolder)
      {
         throw new Exception("You cannot call replace on the event that signifies the end of the stream");

      }

      _replacementEvents = replacementEvents.Select(@event => new RefactoredEvent(@event, new AggregateEventStorageInformation())).ToArray();

      _replacementEvents.ForEach(
         (e, index) =>
         {
            ((IMutableAggregateEvent)e.NewEvent).SetAggregateVersion(_inspectedEvent!.AggregateVersion + index);

            e.StorageInformation.RefactoringInformation = AggregateEventRefactoringInformation.Replaces(_inspectedEvent.MessageId);
            e.StorageInformation.EffectiveVersion = _inspectedEvent.AggregateVersion + index;

            ((IMutableAggregateEvent)e.NewEvent).SetAggregateId(_inspectedEvent.AggregateId);
            ((IMutableAggregateEvent)e.NewEvent).SetUtcTimeStamp(_inspectedEvent.UtcTimeStamp);
         });

      CurrentNode = CurrentNode.Replace(replacementEvents);
      _eventsAddedCallback.Invoke(_replacementEvents);
   }

   public void Reset(AggregateEvent @event)
   {
      if(@event is EndOfAggregateHistoryEventPlaceHolder && _inspectedEvent is not EndOfAggregateHistoryEventPlaceHolder)
      {
         _lastEventInActualStream = _inspectedEvent;
      }
      _inspectedEvent = @event;
      Events = null;
      _currentNode = null;
      _insertedEvents = null;
      _replacementEvents = null;
   }

   public void MoveTo(LinkedListNode<AggregateEvent> current)
   {
      if (current.Value is EndOfAggregateHistoryEventPlaceHolder && _inspectedEvent is not EndOfAggregateHistoryEventPlaceHolder)
      {
         _lastEventInActualStream = _inspectedEvent;
      }
      CurrentNode = current;
      _insertedEvents = null;
      _replacementEvents = null;
   }

   public void InsertBefore(params AggregateEvent[] insert)
   {
      AssertNoPriorModificationsHaveBeenMade();

      _insertedEvents = insert.Select(@event => new RefactoredEvent(@event, new AggregateEventStorageInformation())).ToArray();

      if(_inspectedEvent is EndOfAggregateHistoryEventPlaceHolder)
      {
         _insertedEvents.ForEach(
            (e, index) =>
            {
               ((IMutableAggregateEvent)e.NewEvent).SetAggregateVersion(_inspectedEvent.AggregateVersion + index);

               e.StorageInformation.RefactoringInformation = AggregateEventRefactoringInformation.InsertAfter(_lastEventInActualStream!.MessageId);
               e.StorageInformation.EffectiveVersion = _inspectedEvent.AggregateVersion + index;

               ((IMutableAggregateEvent)e.NewEvent).SetAggregateId(_inspectedEvent.AggregateId);
               ((IMutableAggregateEvent)e.NewEvent).SetUtcTimeStamp(_lastEventInActualStream.UtcTimeStamp);
            });
      }
      else
      {
         _insertedEvents.ForEach(
            (e, index) =>
            {
               ((IMutableAggregateEvent)e.NewEvent).SetAggregateVersion(_inspectedEvent!.AggregateVersion + index);

               e.StorageInformation.RefactoringInformation = AggregateEventRefactoringInformation.InsertBefore(_inspectedEvent.MessageId);
               e.StorageInformation.EffectiveVersion = _inspectedEvent.AggregateVersion + index;

               ((IMutableAggregateEvent)e.NewEvent).SetAggregateId(_inspectedEvent.AggregateId);
               ((IMutableAggregateEvent)e.NewEvent).SetUtcTimeStamp(_inspectedEvent.UtcTimeStamp);
            });
      }

      CurrentNode.ValuesFrom().ForEach((@event, _) => ((IMutableAggregateEvent)@event).SetAggregateVersion(@event.AggregateVersion + _insertedEvents.Length));

      CurrentNode.AddBefore(insert);
      _eventsAddedCallback.Invoke(_insertedEvents);
   }

   internal AggregateEvent[] MutatedHistory => Events?.ToArray() ?? [Assert.Result.NotNull(_inspectedEvent).then(_inspectedEvent)];
}