using Compze.Abstractions.Public;
using Compze.Abstractions.Tessaging.Public;
using Compze.Teventive.Taggregates.Tevents.Public;

// ReSharper disable ClassNeverInstantiated.Global

namespace Compze.Tests.Integration.InProcess;

#pragma warning disable CA1040 // Marker interfaces used for type-routing — a tevent's interface hierarchy IS its routing
public interface IMyGreetingRequestedTevent : ITevent;

public interface IMySpecialGreetingRequestedTevent : IMyGreetingRequestedTevent;

public interface IMyUnrelatedTevent : ITevent;
#pragma warning restore CA1040

class MySpecialGreetingRequestedTevent : IMySpecialGreetingRequestedTevent;

public class MyGreeting
{
   public string Message { get; set; } = "";
}

public class MyStrictlyLocalGreetingTuery : TessageTypes.StrictlyLocal.Tueries.StrictlyLocalTuery<MyStrictlyLocalGreetingTuery, MyGreeting>
{
   public string Name { get; set; } = "";
}

public class MyStrictlyLocalRegisterGreeterTommand : TessageTypes.StrictlyLocal.Tommands.StrictlyLocalTommand
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
