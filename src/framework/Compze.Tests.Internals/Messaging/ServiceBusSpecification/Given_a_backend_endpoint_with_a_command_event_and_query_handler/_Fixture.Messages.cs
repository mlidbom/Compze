using System;
using Compze.GenericAbstractions.Time;
using Compze.Messaging;
using Compze.Messaging.Hypermedia;
using Compze.Persistence.EventStore;
using Compze.Persistence.EventStore.Aggregates;
using JetBrains.Annotations;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable InconsistentNaming for testing

#pragma warning disable IDE1006 //Review OK: Test Naming Styles
#pragma warning disable CA1724  // Type names should not match namespaces
#pragma warning disable CA1715  // Interfaces should start with I

namespace Compze.Tests.Messaging.ServiceBusSpecification.Given_a_backend_endpoint_with_a_command_event_and_query_handler;

public partial class Fixture
{
   protected static class MyAggregateEvent
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

   protected class MyAggregate : Aggregate<MyAggregate, MyAggregateEvent.Implementation.Root, MyAggregateEvent.IRoot>
   {
      public MyAggregate() : base(new DateTimeNowTimeSource())
      {
         RegisterEventAppliers()
           .IgnoreUnhandled<MyAggregateEvent.IRoot>();
      }

      internal void Update() => Publish(new MyAggregateEvent.Implementation.Updated());

      internal static void Create(Guid id, ILocalHypermediaNavigator bus)
      {
         var created = new MyAggregate();
         created.Publish(new MyAggregateEvent.Implementation.Created(id));
         bus.Execute(new CompzeApi().EventStore.Commands.Save(created));
      }
   }

   protected class MyCreateAggregateCommand : MessageTypes.Remotable.AtMostOnce.AtMostOnceHypermediaCommand
   {
      MyCreateAggregateCommand() : base(DeduplicationIdHandling.Reuse) {}

      internal static MyCreateAggregateCommand Create() => new()
                                                           {
                                                              MessageId = Guid.NewGuid(),
                                                              AggregateId = Guid.NewGuid()
                                                           };

      public Guid AggregateId { get; set; }
   }

   protected class MyUpdateAggregateCommand : MessageTypes.Remotable.AtMostOnce.AtMostOnceHypermediaCommand
   {
      [UsedImplicitly] MyUpdateAggregateCommand() : base(DeduplicationIdHandling.Reuse) {}
      public MyUpdateAggregateCommand(Guid aggregateId) : base(DeduplicationIdHandling.Create) => AggregateId = aggregateId;
      public Guid AggregateId { get; private set; }
   }

   protected class MyExactlyOnceCommand : MessageTypes.Remotable.ExactlyOnce.Command;

   protected interface IMyExactlyOnceEvent : IAggregateEvent;
   protected class MyExactlyOnceEvent : AggregateEvent, IMyExactlyOnceEvent;
   protected class MyQuery : MessageTypes.Remotable.NonTransactional.Queries.Query<MyQueryResult>;
   protected class MyQueryResult;
   protected class MyAtMostOnceCommand : MessageTypes.Remotable.AtMostOnce.AtMostOnceCommand<MyCommandResult>
   {
      protected MyAtMostOnceCommand() : base(DeduplicationIdHandling.Reuse) {}
      internal static MyAtMostOnceCommand Create() => new() {MessageId = Guid.NewGuid()};
   }

   protected class MyAtMostOnceCommandWithResult : MessageTypes.Remotable.AtMostOnce.AtMostOnceCommand<MyCommandResult>
   {
      MyAtMostOnceCommandWithResult() : base(DeduplicationIdHandling.Reuse) {}
      internal static MyAtMostOnceCommandWithResult Create() => new() {MessageId = Guid.NewGuid()};
   }
   protected class MyCommandResult;
}