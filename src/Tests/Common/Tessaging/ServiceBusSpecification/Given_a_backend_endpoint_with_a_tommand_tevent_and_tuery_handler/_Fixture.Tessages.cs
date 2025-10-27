using System;
using Compze.Core.Tessaging.Public;
using Compze.Core.Tessaging.Teventive.Public.Taggregates.BaseClasses.Public;
using Compze.Core.Tessaging.Teventive.Public.Taggregates.Tevents.Public;
using Compze.Core.Tessaging.Typermedia.Public;
using Compze.Core.Time.Public;
using Compze.Tessaging.TyperMediaApi.EventStore;
using JetBrains.Annotations;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable InconsistentNaming for testing

#pragma warning disable IDE1006 //Reviewed OK: Test Naming Styles
#pragma warning disable CA1724  // Type names should not match namespaces
#pragma warning disable CA1715  // Interfaces should start with I

namespace Compze.Tests.Common.Tessaging.ServiceBusSpecification.Given_a_backend_endpoint_with_a_tommand_tevent_and_tuery_handler;

public class MyTommandResult;

public class MyAtMostOnceTommandWithResult : TessageTypes.Remotable.AtMostOnce.AtMostOnceTommand<MyTommandResult>
{
   MyAtMostOnceTommandWithResult() : base(DeduplicationIdHandling.Reuse) {}
   public static MyAtMostOnceTommandWithResult Create() => new() {TessageId = Guid.CreateVersion7()};
}

public class MyTueryResult;
public class MyTuery : TessageTypes.Remotable.NonTransactional.Queries.Tuery<MyTueryResult>;
public class MyExactlyOnceTevent : TaggregateTevent, IMyExactlyOnceTevent;
public interface IMyExactlyOnceTevent : ITaggregateTevent;
public class MyExactlyOnceTommand : TessageTypes.Remotable.ExactlyOnce.Tommand;

public class MyUpdateTaggregateTommand : TessageTypes.Remotable.AtMostOnce.AtMostOnceTypermediaTommand
{
   [UsedImplicitly] MyUpdateTaggregateTommand() : base(DeduplicationIdHandling.Reuse) {}
   public MyUpdateTaggregateTommand(Guid taggregateId) : base(DeduplicationIdHandling.Create) => TaggregateId = taggregateId;
   public Guid TaggregateId { get; private set; }
}

public class MyCreateTaggregateTommand : TessageTypes.Remotable.AtMostOnce.AtMostOnceTypermediaTommand
{
   MyCreateTaggregateTommand() : base(DeduplicationIdHandling.Reuse) {}

   public static MyCreateTaggregateTommand Create() => new()
                                                      {
                                                         TessageId = Guid.CreateVersion7(),
                                                         TaggregateId = Guid.NewGuid()
                                                      };

   public Guid TaggregateId { get; set; }
}

public class MyTaggregate : Taggregate<MyTaggregate, MyTaggregateTevent.IRoot, MyTaggregateTevent.Implementation.Root>
{
   public MyTaggregate() : base(new DateTimeNowTimeSource())
   {
      RegisterTeventAppliers()
        .IgnoreUnhandled<MyTaggregateTevent.IRoot>();
   }

   public void Update() => Publish(new MyTaggregateTevent.Implementation.Updated());

   public static void Create(Guid id, IInProcessTypermediaNavigator bus)
   {
      var created = new MyTaggregate();
      created.Publish(new MyTaggregateTevent.Implementation.Created(id));
      bus.Execute(new TeventStoreApi().Tommands.Save(created));
   }
}

public static class MyTaggregateTevent
{
   public interface IRoot : ITaggregateTevent;
   public interface Created : IRoot, ITaggregateCreatedTevent;
   public interface Updated : IRoot;
   public static class Implementation
   {
      public class Root : TaggregateTevent, IRoot
      {
         protected Root() {}
         protected Root(Guid taggregateId) : base(taggregateId) {}
      }

      // ReSharper disable once MemberHidesStaticFromOuterClass
      public class Created(Guid taggregateId) : Root(taggregateId), MyTaggregateTevent.Created;

      // ReSharper disable once MemberHidesStaticFromOuterClass
      public class Updated : Root, MyTaggregateTevent.Updated;
   }
}