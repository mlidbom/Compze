using System;
using System.Collections.Generic;
using System.Linq;
using Compze.Abstractions.Tessaging.Teventive.TeventStore.Public;
using Compze.Abstractions.Tessaging.Teventive.Public;
using Compze.Abstractions.Time.Public;
using Compze.Tessaging.Teventive;
using Compze.Utilities.SystemCE.ReflectionCE;
using JetBrains.Annotations;

// ReSharper disable ClassNeverInstantiated.Global

namespace Compze.Tests.Common.CQRS.TeventRefactoring.Migrations
{
    public interface IRootTevent : IAggregateTevent;

    public abstract class RootTevent : AggregateTevent, IRootTevent;

    namespace Tevents
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
        public void Publish(params RootTevent[] tevents)
        {
#pragma warning disable 618 //Reviewed OK: This test class is allowed to use these "obsolete" methods.
            if (GetIdBypassContractValidation() == Guid.Empty && tevents.First().AggregateId == Guid.Empty)
            {
                SetIdBeVerySureYouKnowWhatYouAreDoing(Guid.NewGuid());
                tevents.Cast<IMutableAggregateTevent>().First().SetAggregateIdInternal(Id);
#pragma warning restore 618
            }

            foreach (var @tevent in tevents)
            {
                base.Publish(@tevent);
            }
        }


        [Obsolete("For serialization only", error: true), UsedImplicitly]
        public TestAggregate() => SetupAppliers();

        TestAggregate(IUtcTimeTimeSource timeSource):base(timeSource) => SetupAppliers();

        void SetupAppliers()
        {
            RegisterTeventAppliers()
                .For<IRootTevent>(e => _history.Add(e));
        }

        public TestAggregate(IUtcTimeTimeSource timeSource, params RootTevent[] tevents):this(timeSource)
        {
           if(tevents.First() is not IAggregateCreatedTevent) throw new Exception($"First tevent must be {nameof(IAggregateCreatedTevent)}");

            Publish(tevents);
        }

        public static TestAggregate FromTevents(IUtcTimeTimeSource timeSource, Guid? id, IEnumerable<Type> tevents)
        {
            var rootTevents = tevents.ToTevents();
#pragma warning disable CS0618 // Type or member is obsolete
            rootTevents.Cast<IMutableAggregateTevent>().First().SetAggregateIdInternal(id ?? Guid.NewGuid());
#pragma warning restore CS0618 // Type or member is obsolete
            return new TestAggregate(timeSource, rootTevents);
        }

        readonly List<IRootTevent> _history = [];
        public IReadOnlyList<IAggregateTevent> History => _history;
    }

    static class TeventSequenceGenerator
    {
        public static RootTevent[] ToTevents(this IEnumerable<Type> types) => types.Select(Constructor.CreateInstance).Cast<RootTevent>().ToArray();
    }
}