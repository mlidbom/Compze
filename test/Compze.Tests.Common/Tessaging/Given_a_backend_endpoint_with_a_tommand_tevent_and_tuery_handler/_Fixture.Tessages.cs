using Compze.Abstractions.Public;
using Compze.Abstractions.Tessaging.Public;
using Compze.Teventive;
using Compze.Teventive.Taggregates.BaseClasses;
using Compze.Teventive.Taggregates.Tevents.Public;
using Compze.Teventive.TeventStore.Typermedia;
using Compze.Typermedia;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable InconsistentNaming for testing
// ReSharper disable once UnusedAutoPropertyAccessor.Local

#pragma warning disable IDE1006 //Reviewed OK: Test Naming Styles
#pragma warning disable CA1724  // Type names should not match namespaces
#pragma warning disable CA1715  // Interfaces should start with I
#pragma warning  disable CA1812 // Avoid uninstantiated internal classes # used via reflection
#pragma warning  disable CA1812 // Avoid uninstantiated internal classes # used via reflection

namespace Compze.Tests.Common.Tessaging.Given_a_backend_endpoint_with_a_tommand_tevent_and_tuery_handler;

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

///<summary>An exactly-once tommand whose handler lives on the Remote endpoint (<see cref="MyExactlyOnceTommand"/>'s lives on the<br/>
/// Backend), so specifications can send from the Backend toward a handler endpoint that may be down or replaced.</summary>
public class MyExactlyOnceTommandHandledByTheRemoteEndpoint : TessageTypes.Remotable.ExactlyOnce.Tommand;

///<summary>An exactly-once tommand no endpoint registers a handler for: sending it must fail loud — a tommand routes to<br/>
/// whichever endpoint advertises its type, and nothing advertises this one.</summary>
public class MyUnhandledExactlyOnceTommand : TessageTypes.Remotable.ExactlyOnce.Tommand;

///<summary>A best-effort tevent: plain <see cref="IRemotableTevent"/> IS the best-effort delivery tier — best-effort across the wire,<br/>
/// no store, no dedup, no retry (see <c>src/Compze.Tessaging/dev_docs/tevent-delivery-model.md</c>).</summary>
public interface IMyBestEffortTevent : IRemotableTevent
{
   ///<summary>Which publish this was, in publish order — lets specifications assert that best-effort tevents arrive in the order they were published.</summary>
   int SequenceNumber { get; }
}

public class MyBestEffortTevent : IMyBestEffortTevent
{
   public int SequenceNumber { get; set; }
}

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
   MyTaggregate() : base(TeventDispatcherConfig.IgnoreAllUnhandled) {} //This test taggregate maintains no state, so no tevent has an applier.

   internal void Update() => Publish(new MyTaggregateTevent.Updated());

   internal static void Create(TaggregateId id, ILocalTypermediaNavigatorSession bus)
   {
      var created = new MyTaggregate();
      created.Publish(new MyTaggregateTevent.Created(id));
      bus.Execute(new TeventStoreApi().Tommands.Save(created));
   }
}

public interface IMyTaggregateTevent<out T> : ITaggregateTevent<T> where T : IMyTaggregateTevent;

public interface IMyTaggregateTevent : ITaggregateTevent
{
   public interface Created : IMyTaggregateTevent, ITaggregateCreatedTevent;
   public interface Updated : IMyTaggregateTevent;
}

public class MyTaggregateTevent<T>(T tevent) : TaggregateTevent<T>(tevent), IMyTaggregateTevent<T> where T : IMyTaggregateTevent;

public class MyTaggregateTevent : TaggregateTevent, IMyTaggregateTevent
{
   MyTaggregateTevent() {}
   MyTaggregateTevent(TaggregateId accountId) : base(accountId) {}

   internal class Created(TaggregateId accountId) : MyTaggregateTevent(accountId), IMyTaggregateTevent.Created;

   internal class Updated : MyTaggregateTevent, IMyTaggregateTevent.Updated;
}
