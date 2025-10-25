namespace Compze.Sql.Common.TeventStore.Abstractions;

public class TeventNeighborhood
{
   public ReadOrder EffectiveReadOrder { get; }
   public ReadOrder PreviousTeventReadOrder { get; }
   public ReadOrder NextTeventReadOrder { get; }

   public TeventNeighborhood(ReadOrder effectiveReadOrder, ReadOrder? previousTeventReadOrder, ReadOrder? nextTeventReadOrder)
   {
      EffectiveReadOrder = effectiveReadOrder;
      NextTeventReadOrder = nextTeventReadOrder ?? EffectiveReadOrder.NextIntegerOrder;
      PreviousTeventReadOrder = previousTeventReadOrder ?? ReadOrder.Zero;
   }
}