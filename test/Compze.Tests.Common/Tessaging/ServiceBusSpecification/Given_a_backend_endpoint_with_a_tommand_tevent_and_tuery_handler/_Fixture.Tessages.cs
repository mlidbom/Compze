using System;
using Compze.Core.Public;
using Compze.Core.Tessaging.Public;
using Compze.Core.Tessaging.Teventive.Public.Taggregates.BaseClasses.Public;
using Compze.Core.Tessaging.Teventive.Public.Taggregates.Tevents.Public;
using Compze.Core.Tessaging.Typermedia.Public;
using Compze.Tessaging.TyperMediaApi.EventStore;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable InconsistentNaming for testing
// ReSharper disable once UnusedAutoPropertyAccessor.Local

#pragma warning disable IDE1006 //Reviewed OK: Test Naming Styles
#pragma warning disable CA1724  // Type names should not match namespaces
#pragma warning disable CA1715  // Interfaces should start with I
#pragma warning  disable CA1812 // Avoid uninstantiated internal classes # used via reflection
#pragma warning  disable CA1812 // Avoid uninstantiated internal classes # used via reflection

namespace Compze.Tests.Common.Tessaging.ServiceBusSpecification.Given_a_backend_endpoint_with_a_tommand_tevent_and_tuery_handler;

public class MyTommandResult;

public class MyAtMostOnceTypermediaTommandWithResult : TessageTypes.Remotable.AtMostOnce.AtMostOnceTypermediaTommand<MyTommandResult>
{
   MyAtMostOnceTypermediaTommandWithResult() {}
   public static MyAtMostOnceTypermediaTommandWithResult Create() => new() { Id = new TessageId() };
}

public class MyTueryResult;
public class MyTuery : TessageTypes.Remotable.NonTransactional.Tueries.Tuery<MyTueryResult>;
class MyExactlyOnceTevent : TaggregateTevent, IMyExactlyOnceTevent;
interface IMyExactlyOnceTevent : ITaggregateTevent;
public class MyExactlyOnceTommand : TessageTypes.Remotable.ExactlyOnce.Tommand;

class MyUpdateTaggregateTommand : TessageTypes.Remotable.AtMostOnce.AtMostOnceTypermediaTommand
{
   [Obsolete("Used by serializer", error: true)]
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
   // ReSharper disable once UnusedMember.Global
   public MyUpdateTaggregateTommand() {}
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
   public TaggregateId TaggregateId { get; private set; }
}

public class MyCreateTaggregateTommand : TessageTypes.Remotable.AtMostOnce.AtMostOnceTypermediaTommand
{
   [Obsolete("Used by serializer", error: true)]
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
   // ReSharper disable once UnusedMember.Local
   MyCreateTaggregateTommand() {}
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

   MyCreateTaggregateTommand(TaggregateId taggregateId) => TaggregateId = taggregateId;

   public static MyCreateTaggregateTommand Create() => new(new TaggregateId());

   // ReSharper disable once MemberCanBeInternal — Serialized via Newtonsoft
   public TaggregateId TaggregateId { get; set; }
}

public class MyTaggregate : Taggregate<MyTaggregate, IMyTaggregateTevent, MyTaggregateTevent, IMyTaggregateTevent<IMyTaggregateTevent>, MyTaggregateTevent<MyTaggregateTevent>>
{
   MyTaggregate()
   {
      RegisterTeventAppliers()
        .IgnoreUnhandled<IMyTaggregateTevent>();
   }

   internal void Update() => Publish(new MyTaggregateTevent.Updated());

   internal static void Create(TaggregateId id, IInProcessTypermediaNavigator bus)
   {
      var created = new MyTaggregate();
      created.Publish(new MyTaggregateTevent.Created(id));
      bus.Execute(new TeventStoreApi().Tommands.Save(created));
   }
}

public interface IMyTaggregateTevent<out T> : ITaggregateIdentifyingTevent<T> where T : IMyTaggregateTevent;

public interface IMyTaggregateTevent : ITaggregateTevent
{
   public interface Created : IMyTaggregateTevent, ITaggregateCreatedTevent;
   public interface Updated : IMyTaggregateTevent;
}

public class MyTaggregateTevent<T>(T tevent) : TaggregateIdentifyingTevent<T>(tevent), IMyTaggregateTevent<T> where T : IMyTaggregateTevent;

public class MyTaggregateTevent : TaggregateTevent, IMyTaggregateTevent
{
   MyTaggregateTevent() {}
   MyTaggregateTevent(TaggregateId accountId) : base(accountId) {}

   internal class Created(TaggregateId accountId) : MyTaggregateTevent(accountId), IMyTaggregateTevent.Created;

   internal class Updated : MyTaggregateTevent, IMyTaggregateTevent.Updated;
}
