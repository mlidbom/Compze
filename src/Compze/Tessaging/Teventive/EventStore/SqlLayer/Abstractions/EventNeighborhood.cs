namespace Compze.Tessaging.Teventive.EventStore.SqlLayer.Abstractions;

public class EventNeighborhood
{
   public ReadOrder EffectiveReadOrder { get; }
   public ReadOrder PreviousEventReadOrder { get; }
   public ReadOrder NextEventReadOrder { get; }

   public EventNeighborhood(ReadOrder effectiveReadOrder, ReadOrder? previousEventReadOrder, ReadOrder? nextEventReadOrder)
   {
      EffectiveReadOrder = effectiveReadOrder;
      NextEventReadOrder = nextEventReadOrder ?? EffectiveReadOrder.NextIntegerOrder;
      PreviousEventReadOrder = previousEventReadOrder ?? ReadOrder.Zero;
   }
}