using System;
using System.Collections.Generic;
using Compze.GenericAbstractions.Time;
using Compze.Persistence.EventStore;
using Compze.Persistence.EventStore.Aggregates;
using Compze.SystemCE.ReactiveCE;
using Compze.Testing;
using FluentAssertions;
using NUnit.Framework;

namespace Compze.Tests.Unit.CQRS.Aggregates;

[TestFixture]
public class AggregateTests : UniversalTestBase
{
   [Test]
   public void VersionIncreasesWithEachAppliedEvent()
   {
      var user = new User();
      Assert.That(user.Version, Is.EqualTo(0));

      user.Register("email", "password", Guid.NewGuid());
      Assert.That(user.Version, Is.EqualTo(1));

      user.ChangeEmail("NewEmail");
      Assert.That(user.Version, Is.EqualTo(2));

      user.ChangePassword("NewPassword");
      Assert.That(user.Version, Is.EqualTo(3));

   }

   [Test]
   public void ResetEmptiesOutListOfUncommittedEvents()
   {
      var user = new User();
      IEventStored userAseventStored = user;
      Assert.That(user.Version, Is.EqualTo(0));

      user.Register("email", "password", Guid.NewGuid());
      userAseventStored.Commit(_ => {});
      userAseventStored.Commit(events => events.Should().BeEmpty());

      user.ChangeEmail("NewEmail");
      userAseventStored.Commit(_ => {});
      userAseventStored.Commit(events => events.Should().BeEmpty());

      user.ChangePassword("NewPassword");
      userAseventStored.Commit(_ => {});
      userAseventStored.Commit(events => events.Should().BeEmpty());
   }




   [Test]
   public void When_Raising_event_that_triggers_another_event_both_events_are_outputted_on_the_observable_only_after_the_triggered_event_and_in_the_raised_order()
   {
      var aggregate = new CascadingEventsAggregate();
      var receivedEvents = new List<IAggregateEvent>();
      using(((IEventStored)aggregate).EventStream.Subscribe(@event =>
            {
               receivedEvents.Add(@event);
               aggregate.TriggeringEventApplied.Should()
                        .BeTrue();
               aggregate.TriggeredEventApplied.Should()
                        .BeTrue();
            }))
      {
         aggregate.RaiseTriggeringEvent();
      }

      receivedEvents.Count.Should().Be(2);
      receivedEvents[0].GetType().Should().Be<TriggeringEvent>();
      receivedEvents[1].GetType().Should().Be<TriggeredEvent>();
   }

   class CascadingEventsAggregate : Aggregate<CascadingEventsAggregate, AggregateEvent, IAggregateEvent>
   {
      public CascadingEventsAggregate():base(TestingTimeSource.FrozenUtcNow())
      {
         RegisterEventHandlers()
           .For<ITriggeringEvent>(_ => Publish(new TriggeredEvent()));

         RegisterEventAppliers()
           .For<ITriggeringEvent>(_ => TriggeringEventApplied = true)
           .For<ITriggeredEvent>(_ => TriggeredEventApplied = true);
      }
      public bool TriggeredEventApplied { get; private set; }
      public bool TriggeringEventApplied { get; private set; }
      public void RaiseTriggeringEvent() => Publish(new TriggeringEvent());
   }

   interface ITriggeringEvent : IAggregateCreatedEvent;

   class TriggeringEvent() : AggregateEvent(Guid.NewGuid()), ITriggeringEvent;

   interface ITriggeredEvent : IAggregateEvent;
   class TriggeredEvent : AggregateEvent, ITriggeredEvent;
}