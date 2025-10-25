using System;
using Compze.Abstractions.Tessaging.Public;
using Compze.Abstractions.Tessaging.Teventive.EventStore.Public;
using Compze.Abstractions.Tessaging.Typermedia.Public;
using Compze.Abstractions.Time.Public;
using Compze.Tessaging.Teventive;
using Compze.Tessaging.TyperMediaApi.EventStore;
using JetBrains.Annotations;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable InconsistentNaming for testing

#pragma warning disable IDE1006 //Reviewed OK: Test Naming Styles
#pragma warning disable CA1724  // Type names should not match namespaces
#pragma warning disable CA1715  // Interfaces should start with I

namespace Compze.Tests.Common.Tessaging.ServiceBusSpecification.Given_a_backend_endpoint_with_a_command_event_and_query_handler;

public class MyCommandResult;

public class MyAtMostOnceCommandWithResult : MessageTypes.Remotable.AtMostOnce.AtMostOnceCommand<MyCommandResult>
{
   MyAtMostOnceCommandWithResult() : base(DeduplicationIdHandling.Reuse) {}
   public static MyAtMostOnceCommandWithResult Create() => new() {MessageId = Guid.CreateVersion7()};
}

public class MyQueryResult;
public class MyQuery : MessageTypes.Remotable.NonTransactional.Queries.Query<MyQueryResult>;
public class MyExactlyOnceEvent : AggregateEvent, IMyExactlyOnceEvent;
public interface IMyExactlyOnceEvent : IAggregateEvent;
public class MyExactlyOnceCommand : MessageTypes.Remotable.ExactlyOnce.Command;

public class MyUpdateAggregateCommand : MessageTypes.Remotable.AtMostOnce.AtMostOnceHypermediaCommand
{
   [UsedImplicitly] MyUpdateAggregateCommand() : base(DeduplicationIdHandling.Reuse) {}
   public MyUpdateAggregateCommand(Guid aggregateId) : base(DeduplicationIdHandling.Create) => AggregateId = aggregateId;
   public Guid AggregateId { get; private set; }
}

public class MyCreateAggregateCommand : MessageTypes.Remotable.AtMostOnce.AtMostOnceHypermediaCommand
{
   MyCreateAggregateCommand() : base(DeduplicationIdHandling.Reuse) {}

   public static MyCreateAggregateCommand Create() => new()
                                                      {
                                                         MessageId = Guid.CreateVersion7(),
                                                         AggregateId = Guid.NewGuid()
                                                      };

   public Guid AggregateId { get; set; }
}

public class MyAggregate : Aggregate<MyAggregate, MyAggregateEvent.IRoot, MyAggregateEvent.Implementation.Root>
{
   public MyAggregate() : base(new DateTimeNowTimeSource())
   {
      RegisterEventAppliers()
        .IgnoreUnhandled<MyAggregateEvent.IRoot>();
   }

   public void Update() => Publish(new MyAggregateEvent.Implementation.Updated());

   public static void Create(Guid id, IInProcessHypermediaNavigator bus)
   {
      var created = new MyAggregate();
      created.Publish(new MyAggregateEvent.Implementation.Created(id));
      bus.Execute(new EventStoreApi().Commands.Save(created));
   }
}

public static class MyAggregateEvent
{
   public interface IRoot : IAggregateEvent;
   public interface Created : IRoot, IAggregateCreatedEvent;
   public interface Updated : IRoot;
   public static class Implementation
   {
      public class Root : AggregateEvent, IRoot
      {
         protected Root() {}
         protected Root(Guid aggregateId) : base(aggregateId) {}
      }

      // ReSharper disable once MemberHidesStaticFromOuterClass
      public class Created(Guid aggregateId) : Root(aggregateId), MyAggregateEvent.Created;

      // ReSharper disable once MemberHidesStaticFromOuterClass
      public class Updated : Root, MyAggregateEvent.Updated;
   }
}