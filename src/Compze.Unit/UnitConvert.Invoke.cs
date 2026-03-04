namespace Compze.Unit;

public static partial class UnitConvert
{
   public static Unit Invoke(Action @this)
   {
      @this();
      return Unit.Value;
   }
}
