using System;
using System.Collections.Generic;
using System.Linq;
using Compze.Abstractions.Tessaging.Teventive.EventStore.Public;
using Compze.Abstractions.Tessaging.Teventive.Public;
using Compze.Abstractions.Time.Public;
using Compze.Tessaging.Teventive;
using Compze.Utilities.SystemCE.ReflectionCE;
using JetBrains.Annotations;

// ReSharper disable ClassNeverInstantiated.Global

namespace Compze.Tests.Common.CQRS.EventRefactoring.Migrations
{
    public interface IRootTevent : IAggregateTevent;

    public abstract class RootTevent : AggregateTevent, IRootTevent;

    namespace Events
    {
        public abstract class EcAbstract : RootTevent, IAggregateCreatedTevent;

        // ReSharper disable ClassNeverInstantiated.Global
        public class Ec1 : EcAbstract;
        public class Ec2 : EcAbstract;
        public class Ec3 : EcAbstract;
        public class E1 : RootTevent;
        public class E2 : RootTevent;
        public class E3 : RootTevent;
        public class E4 : RootTevent;
        public class E5 : RootTevent;
        public class E6 : RootTevent;
        public class E7 : RootTevent;
        public class E8 : RootTevent;
        public class E9 : RootTevent;
        public class Ef : RootTevent;
        // ReSharper restore ClassNeverInstantiated.Global
    }

    public class TestAggregate : Aggregate<TestAggregate, IRootTevent, RootTevent>
    {
        public void Publish(params RootTevent[] events)
        {
#pragma warning disable 618 //Reviewed OK: This test class is allowed to use these "obsolete" methods.
            if (GetIdBypassContractValidation() == Guid.Empty && events.First().AggregateId == Guid.Empty)
            {
                SetIdBeVerySureYouKnowWhatYouAreDoing(Guid.NewGuid());
                events.Cast<IMutableAggregateTevent>().First().SetAggregateIdInternal(Id);
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
                .For<IRootTevent>(e => _history.Add(e));
        }

        public TestAggregate(IUtcTimeTimeSource timeSource, params RootTevent[] events):this(timeSource)
        {
           if(events.First() is not IAggregateCreatedTevent) throw new Exception($"First event must be {nameof(IAggregateCreatedTevent)}");

            Publish(events);
        }

        public static TestAggregate FromEvents(IUtcTimeTimeSource timeSource, Guid? id, IEnumerable<Type> events)
        {
            var rootEvents = events.ToEvents();
#pragma warning disable CS0618 // Type or member is obsolete
            rootEvents.Cast<IMutableAggregateTevent>().First().SetAggregateIdInternal(id ?? Guid.NewGuid());
#pragma warning restore CS0618 // Type or member is obsolete
            return new TestAggregate(timeSource, rootEvents);
        }

        readonly List<IRootTevent> _history = [];
        public IReadOnlyList<IAggregateTevent> History => _history;
    }

    static class EventSequenceGenerator
    {
        public static RootTevent[] ToEvents(this IEnumerable<Type> types) => types.Select(Constructor.CreateInstance).Cast<RootTevent>().ToArray();
    }
}