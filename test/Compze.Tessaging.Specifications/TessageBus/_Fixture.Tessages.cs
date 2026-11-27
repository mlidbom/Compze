using Compze.Tessaging.TessageTypes;

namespace Compze.Tessaging.Specifications.TessageBus;

///<summary>An exactly-once tommand whose handler waits for the token it is handed to be cancelled.</summary>
public class TommandWhoseHandlerWaitsForItsCancellation : Remotable.ExactlyOnce.Tommand;

#pragma warning disable CA1040 // Marker interfaces used for type-routing — a tevent's interface hierarchy IS its routing
///<summary>A tevent whose handler waits for the token it is handed to be cancelled.</summary>
public interface ITeventWhoseHandlerWaitsForItsCancellation : ITevent;
#pragma warning restore CA1040

public class TeventWhoseHandlerWaitsForItsCancellation : ITeventWhoseHandlerWaitsForItsCancellation;

///<summary>An exactly-once tommand no endpoint handles: a send of it waits for a receiver to bind it to.</summary>
public class TommandNoEndpointHandles : Remotable.ExactlyOnce.Tommand;

///<summary>A step of a flow, handled by a deferred handler: its number, and how many steps its handler sends after it, one at<br/>
/// a time, each to the endpoint itself.</summary>
public class DeferredStep : Remotable.ExactlyOnce.Tommand
{
   // ReSharper disable once UnusedMember.Global Used by the serializer.
   public DeferredStep() {}
   public DeferredStep(int number, int stepsAfterIt = 0)
   {
      Number = number;
      StepsAfterIt = stepsAfterIt;
   }

   // ReSharper disable PropertyCanBeMadeInitOnly.Global serialization...
   public int Number { get; set; }
   public int StepsAfterIt { get; set; }
   // ReSharper restore PropertyCanBeMadeInitOnly.Global
}

///<summary>A tessage of the handling order's specifications, numbered in the order the specification sends or publishes it.</summary>
public interface INumberedTessage
{
   int Number { get; }
}

///<summary>A step whose deferred handler gives up its place in the handling order.</summary>
public class StepGivingUpItsPlace : Remotable.ExactlyOnce.Tommand, INumberedTessage
{
   // ReSharper disable once UnusedMember.Global Used by the serializer.
   public StepGivingUpItsPlace() {}
   public StepGivingUpItsPlace(int number) => Number = number;

   // ReSharper disable once PropertyCanBeMadeInitOnly.Global serialization...
   public int Number { get; set; }
}

///<summary>A step whose deferred handler keeps its place in the handling order.</summary>
public class StepKeepingItsPlace : Remotable.ExactlyOnce.Tommand, INumberedTessage
{
   // ReSharper disable once UnusedMember.Global Used by the serializer.
   public StepKeepingItsPlace() {}
   public StepKeepingItsPlace(int number) => Number = number;

   // ReSharper disable once PropertyCanBeMadeInitOnly.Global serialization...
   public int Number { get; set; }
}

///<summary>An exactly-once tommand whose handler, which isn't deferred, gives up its place in the handling order.</summary>
public class TommandGivingUpItsPlace : Remotable.ExactlyOnce.Tommand, INumberedTessage
{
   // ReSharper disable once UnusedMember.Global Used by the serializer.
   public TommandGivingUpItsPlace() {}
   public TommandGivingUpItsPlace(int number) => Number = number;

   // ReSharper disable once PropertyCanBeMadeInitOnly.Global serialization...
   public int Number { get; set; }
}

///<summary>An exactly-once tevent every handler of which gives up its place in the handling order.</summary>
public interface ITeventWhoseHandlersAllGiveUpTheirPlace : IExactlyOnceTevent, INumberedTessage;

public class TeventWhoseHandlersAllGiveUpTheirPlace : ITeventWhoseHandlersAllGiveUpTheirPlace
{
   // ReSharper disable once UnusedMember.Global Used by the serializer.
   public TeventWhoseHandlersAllGiveUpTheirPlace() {}
   public TeventWhoseHandlersAllGiveUpTheirPlace(int number) => Number = number;

   // ReSharper disable PropertyCanBeMadeInitOnly.Global serialization...
   public TessageId Id { get; set; } = TessageId.New();
   public int Number { get; set; }
   // ReSharper restore PropertyCanBeMadeInitOnly.Global
}

///<summary>An exactly-once tevent one of whose handlers keeps its place in the handling order, and the other gives it up.</summary>
public interface ITeventOneOfWhoseHandlersKeepsItsPlace : IExactlyOnceTevent, INumberedTessage;

public class TeventOneOfWhoseHandlersKeepsItsPlace : ITeventOneOfWhoseHandlersKeepsItsPlace
{
   // ReSharper disable once UnusedMember.Global Used by the serializer.
   public TeventOneOfWhoseHandlersKeepsItsPlace() {}
   public TeventOneOfWhoseHandlersKeepsItsPlace(int number) => Number = number;

   // ReSharper disable PropertyCanBeMadeInitOnly.Global serialization...
   public TessageId Id { get; set; } = TessageId.New();
   public int Number { get; set; }
   // ReSharper restore PropertyCanBeMadeInitOnly.Global
}

///<summary>A tessage of the transaction timeout's specifications, named by the specification so that its handler can tell how<br/>
/// many tries it has had.</summary>
public interface INamedTessage
{
   string Name { get; }
}

///<summary>A step whose deferred handler's registration chooses a transaction timeout of a second.</summary>
public class StepWithATransactionTimeoutOfASecond : Remotable.ExactlyOnce.Tommand, INamedTessage
{
   // ReSharper disable once UnusedMember.Global Used by the serializer.
   public StepWithATransactionTimeoutOfASecond() {}
   public StepWithATransactionTimeoutOfASecond(string name) => Name = name;

   // ReSharper disable once PropertyCanBeMadeInitOnly.Global serialization...
   public string Name { get; set; } = "";
}

///<summary>An exactly-once tommand whose handler, which isn't deferred, has a registration that chooses a transaction timeout<br/>
/// of a second.</summary>
public class TommandWithATransactionTimeoutOfASecond : Remotable.ExactlyOnce.Tommand, INamedTessage
{
   // ReSharper disable once UnusedMember.Global Used by the serializer.
   public TommandWithATransactionTimeoutOfASecond() {}
   public TommandWithATransactionTimeoutOfASecond(string name) => Name = name;

   // ReSharper disable once PropertyCanBeMadeInitOnly.Global serialization...
   public string Name { get; set; } = "";
}

///<summary>An exactly-once tevent every handler of which has a registration that chooses a transaction timeout of a second.</summary>
public interface ITeventWhoseHandlersAllHaveATransactionTimeoutOfASecond : IExactlyOnceTevent, INamedTessage;

public class TeventWhoseHandlersAllHaveATransactionTimeoutOfASecond : ITeventWhoseHandlersAllHaveATransactionTimeoutOfASecond
{
   // ReSharper disable once UnusedMember.Global Used by the serializer.
   public TeventWhoseHandlersAllHaveATransactionTimeoutOfASecond() {}
   public TeventWhoseHandlersAllHaveATransactionTimeoutOfASecond(string name) => Name = name;

   // ReSharper disable PropertyCanBeMadeInitOnly.Global serialization...
   public TessageId Id { get; set; } = TessageId.New();
   public string Name { get; set; } = "";
   // ReSharper restore PropertyCanBeMadeInitOnly.Global
}

///<summary>An exactly-once tevent one of whose handlers has a registration that chooses a transaction timeout of a second, and<br/>
/// the other a registration that chooses none.</summary>
public interface ITeventOneOfWhoseHandlersHasATransactionTimeoutOfASecond : IExactlyOnceTevent, INamedTessage;

public class TeventOneOfWhoseHandlersHasATransactionTimeoutOfASecond : ITeventOneOfWhoseHandlersHasATransactionTimeoutOfASecond
{
   // ReSharper disable once UnusedMember.Global Used by the serializer.
   public TeventOneOfWhoseHandlersHasATransactionTimeoutOfASecond() {}
   public TeventOneOfWhoseHandlersHasATransactionTimeoutOfASecond(string name) => Name = name;

   // ReSharper disable PropertyCanBeMadeInitOnly.Global serialization...
   public TessageId Id { get; set; } = TessageId.New();
   public string Name { get; set; } = "";
   // ReSharper restore PropertyCanBeMadeInitOnly.Global
}
