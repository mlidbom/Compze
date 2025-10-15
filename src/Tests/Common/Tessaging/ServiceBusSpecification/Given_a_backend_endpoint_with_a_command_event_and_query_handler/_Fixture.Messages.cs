using System;
using Compze.Abstractions.Internal.Time;
using Compze.Tessaging.Abstractions;
using Compze.Tessaging.Sql.EventStore;
using Compze.Tessaging.Teventive;
using Compze.Tessaging.Teventive.EventStore.Abstractions;
using Compze.Tessaging.Typermedia.Abstractions;
using JetBrains.Annotations;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable InconsistentNaming for testing

#pragma warning disable IDE1006 //Reviewed OK: Test Naming Styles
#pragma warning disable CA1724  // Type names should not match namespaces
#pragma warning disable CA1715  // Interfaces should start with I

namespace Compze.Tests.Common.Tessaging.ServiceBusSpecification.Given_a_backend_endpoint_with_a_command_event_and_query_handler;

public partial class Fixture
{
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

   public class MyUpdateAggregateCommand : MessageTypes.Remotable.AtMostOnce.AtMostOnceHypermediaCommand
   {
      [UsedImplicitly] MyUpdateAggregateCommand() : base(DeduplicationIdHandling.Reuse) {}
      public MyUpdateAggregateCommand(Guid aggregateId) : base(DeduplicationIdHandling.Create) => AggregateId = aggregateId;
      public Guid AggregateId { get; private set; }
   }

   public class MyExactlyOnceCommand : MessageTypes.Remotable.ExactlyOnce.Command;

   public interface IMyExactlyOnceEvent : IAggregateEvent;
   public class MyExactlyOnceEvent : AggregateEvent, IMyExactlyOnceEvent;
   public class MyQuery : MessageTypes.Remotable.NonTransactional.Queries.Query<MyQueryResult>;
   public class MyQueryResult;

   public class MyAtMostOnceCommandWithResult : MessageTypes.Remotable.AtMostOnce.AtMostOnceCommand<MyCommandResult>
   {
      MyAtMostOnceCommandWithResult() : base(DeduplicationIdHandling.Reuse) {}
      public static MyAtMostOnceCommandWithResult Create() => new() {MessageId = Guid.CreateVersion7()};
   }
   public class MyCommandResult;
}