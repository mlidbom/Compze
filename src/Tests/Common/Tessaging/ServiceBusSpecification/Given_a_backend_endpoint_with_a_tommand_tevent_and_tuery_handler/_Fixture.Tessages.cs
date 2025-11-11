using System;
using Compze.Core.Public;
using Compze.Core.Tessaging.Public;
using Compze.Core.Tessaging.Teventive.Public.Taggregates.BaseClasses.Public;
using Compze.Core.Tessaging.Teventive.Public.Taggregates.Tevents.Public;
using Compze.Core.Tessaging.Typermedia.Public;
using Compze.Tessaging.TyperMediaApi.EventStore;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable InconsistentNaming for testing

#pragma warning disable IDE1006 //Reviewed OK: Test Naming Styles
#pragma warning disable CA1724  // Type names should not match namespaces
#pragma warning disable CA1715  // Interfaces should start with I

namespace Compze.Tests.Common.Tessaging.ServiceBusSpecification.Given_a_backend_endpoint_with_a_tommand_tevent_and_tuery_handler;

public class MyTommandResult;

public class MyAtMostOnceTypermediaTommandWithResult : TessageTypes.Remotable.AtMostOnce.AtMostOnceTypermediaTommand<MyTommandResult>
{
   MyAtMostOnceTypermediaTommandWithResult() : base() {}
   public static MyAtMostOnceTypermediaTommandWithResult Create() => new() {Id = new TessageId()};
}

public class MyTueryResult;
public class MyTuery : TessageTypes.Remotable.NonTransactional.Tueries.Tuery<MyTueryResult>;
public class MyExactlyOnceTevent : TaggregateTevent, IMyExactlyOnceTevent;
public interface IMyExactlyOnceTevent : ITaggregateTevent;
public class MyExactlyOnceTommand : TessageTypes.Remotable.ExactlyOnce.Tommand;

public class MyUpdateTaggregateTommand : TessageTypes.Remotable.AtMostOnce.AtMostOnceTypermediaTommand
{
   [Obsolete("Used by serializer", error:true)]
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
   public MyUpdateTaggregateTommand() {}
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
   public MyUpdateTaggregateTommand(TaggregateId taggregateId) => TaggregateId = taggregateId;
   public TaggregateId TaggregateId { get; private set; }
}

public class MyCreateTaggregateTommand : TessageTypes.Remotable.AtMostOnce.AtMostOnceTypermediaTommand
{
   [Obsolete("Used by serializer", error:true)]
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
   public MyCreateTaggregateTommand() {}
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

   public MyCreateTaggregateTommand(TaggregateId taggregateId) => TaggregateId = taggregateId;

   public static MyCreateTaggregateTommand Create() => new(new TaggregateId());

   public TaggregateId TaggregateId { get; set; }
}

public class MyTaggregate : Taggregate<MyTaggregate, IMyTaggregateTevent, IMyTaggregateTevent.Implementation.Root>
{
   public MyTaggregate()
   {
      RegisterTeventAppliers()
        .IgnoreUnhandled<IMyTaggregateTevent>();
   }

   public void Update() => Publish(new IMyTaggregateTevent.Implementation.Updated());

   public static void Create(TaggregateId id, IInProcessTypermediaNavigator bus)
   {
      var created = new MyTaggregate();
      created.Publish(new IMyTaggregateTevent.Implementation.Created(id));
      bus.Execute(new TeventStoreApi().Tommands.Save(created));
   }
}

public interface IMyTaggregateTevent : ITaggregateTevent
{
   public interface Created : IMyTaggregateTevent, ITaggregateCreatedTevent;
   public interface Updated : IMyTaggregateTevent;
   public static class Implementation
   {
      public class Root : TaggregateTevent, IMyTaggregateTevent
      {
         protected Root() {}
         protected Root(TaggregateId accountId) : base(accountId) {}
      }

      // ReSharper disable once MemberHidesStaticFromOuterClass
      public class Created(TaggregateId accountId) : Root(accountId), IMyTaggregateTevent.Created;

      // ReSharper disable once MemberHidesStaticFromOuterClass
      public class Updated : Root, IMyTaggregateTevent.Updated;
   }
}