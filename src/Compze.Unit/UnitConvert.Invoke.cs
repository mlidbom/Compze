namespace Compze.Unit;

public static partial class UnitConvert
{
   public static Unit Invoke(this Action @this)
   {
      @this();
      return Unit.Value;
   }
}
