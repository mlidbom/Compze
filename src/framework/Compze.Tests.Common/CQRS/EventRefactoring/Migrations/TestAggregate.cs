﻿using System;
using System.Collections.Generic;
using System.Linq;
using Compze.GenericAbstractions.Time;
using Compze.Persistence.EventStore;
using Compze.Persistence.EventStore.Aggregates;
using Compze.SystemCE.ReflectionCE;
using JetBrains.Annotations;

// ReSharper disable ClassNeverInstantiated.Global

namespace Compze.Tests.CQRS.EventRefactoring.Migrations
{
    interface IRootEvent : IAggregateEvent;

    abstract class RootEvent : AggregateEvent, IRootEvent;

    namespace Events
    {
        abstract class EcAbstract : RootEvent, IAggregateCreatedEvent;

        // ReSharper disable ClassNeverInstantiated.Global
        class Ec1 : EcAbstract;
        class Ec2 : EcAbstract;
        class Ec3 : EcAbstract;
        class E1 : RootEvent;
        class E2 : RootEvent;
        class E3 : RootEvent;
        class E4 : RootEvent;
        class E5 : RootEvent;
        class E6 : RootEvent;
        class E7 : RootEvent;
        class E8 : RootEvent;
        class E9 : RootEvent;
        class Ef : RootEvent;
        // ReSharper restore ClassNeverInstantiated.Global
    }

    class TestAggregate : Aggregate<TestAggregate, RootEvent, IRootEvent>
    {
        public void Publish(params RootEvent[] events)
        {
#pragma warning disable 618 //Review OK: This test class is allowed to use these "obsolete" methods.
            if (GetIdBypassContractValidation() == Guid.Empty && events.First().AggregateId == Guid.Empty)
            {
                SetIdBeVerySureYouKnowWhatYouAreDoing(Guid.NewGuid());
                events.Cast<IMutableAggregateEvent>().First().SetAggregateId(Id);
#pragma warning restore 618
            }

            foreach (var @event in events)
            {
                base.Publish(@event);
            }
        }


        [Obsolete("For serialization only", error: true), UsedImplicitly]
        public TestAggregate() => SetupAppliers();

        TestAggregate(IUtcTimeTimeSource timeSource):base(timeSource) => SetupAppliers();

        void SetupAppliers()
        {
            RegisterEventAppliers()
                .For<IRootEvent>(e => _history.Add(e));
        }

        public TestAggregate(IUtcTimeTimeSource timeSource, params RootEvent[] events):this(timeSource)
        {
           if(events.First() is not IAggregateCreatedEvent) throw new Exception($"First event must be {nameof(IAggregateCreatedEvent)}");

            Publish(events);
        }

        public static TestAggregate FromEvents(IUtcTimeTimeSource timeSource, Guid? id, IEnumerable<Type> events)
        {
            var rootEvents = events.ToEvents();
            rootEvents.Cast<IMutableAggregateEvent>().First().SetAggregateId(id ?? Guid.NewGuid());
            return new TestAggregate(timeSource, rootEvents);
        }

        readonly List<IRootEvent> _history = [];
        public IReadOnlyList<IAggregateEvent> History => _history;
    }

    static class EventSequenceGenerator
    {
        public static RootEvent[] ToEvents(this IEnumerable<Type> types) => types.Select(Constructor.CreateInstance).Cast<RootEvent>().ToArray();
    }
}