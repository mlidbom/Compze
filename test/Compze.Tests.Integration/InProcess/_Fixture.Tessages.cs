using Compze.Abstractions.Public;
using Compze.Tessaging;
using Compze.Tessaging.TessageBus;
using Compze.Tessaging.TessageTypes;
using Compze.Teventive.Taggregates.Tevents.Public;

// ReSharper disable ClassNeverInstantiated.Global

namespace Compze.Tests.Integration.InProcess;

#pragma warning disable CA1040 // Marker interfaces used for type-routing — a tevent's interface hierarchy IS its routing
public interface IMyGreetingRequestedTevent : ITevent;

public interface IMySpecialGreetingRequestedTevent : IMyGreetingRequestedTevent;

public interface IMyUnrelatedTevent : ITevent;
#pragma warning restore CA1040

class MySpecialGreetingRequestedTevent : IMySpecialGreetingRequestedTevent;

///<summary>A tevent carrying its publish's sequence number — for pinning that observation dispatch preserves publish order (per-observer FIFO).</summary>
public interface IMyNumberedTevent : ITevent
{
   int SequenceNumber { get; }
}

class MyNumberedTevent(int sequenceNumber) : IMyNumberedTevent
{
   public int SequenceNumber { get; } = sequenceNumber;
}

public class MyGreeting
{
   public string Message { get; set; } = "";
}

public class MyStrictlyLocalGreetingTuery : StrictlyLocal.Tueries.StrictlyLocalTuery<MyStrictlyLocalGreetingTuery, MyGreeting>
{
   public string Name { get; set; } = "";
}

public class MyStrictlyLocalRegisterGreeterTommand : StrictlyLocal.Tommands.StrictlyLocalTommand
{
   public string Name { get; set; } = "";
}

///<summary>A hand-built <see cref="ITaggregateTevent"/>, for publishing through the <see cref="IUnitOfWorkTeventPublisher"/> without a taggregate or tevent store in the composition.</summary>
class MyTaggregateTevent : ITaggregateTevent
{
   public TessageId Id { get; } = new();
   public int TaggregateVersion => 1;
   public TaggregateId TaggregateId { get; } = new();
   public DateTime UtcTimeStamp { get; } = DateTime.UtcNow;
}
