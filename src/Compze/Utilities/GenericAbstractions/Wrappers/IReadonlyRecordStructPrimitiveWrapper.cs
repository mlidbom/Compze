using System;

namespace Compze.Utilities.GenericAbstractions.Wrappers;

public interface IReadonlyRecordStructPrimitiveWrapper<out TPrimitive> : IStructValueWrapper<TPrimitive>
   where TPrimitive : struct;

public interface IMessageId : IReadonlyRecordStructPrimitiveWrapper<Guid>;
public interface IEventId : IMessageId;
public readonly record struct MessageId(Guid Value) : IMessageId;


public readonly record struct EventId(Guid Value) : IEventId;
public class Event
{
   public EventId Id { get; set;  }

   public EventId? NullableId { get;  set; }
}
