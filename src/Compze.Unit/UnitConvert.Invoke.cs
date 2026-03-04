namespace Compze.Unit;

///<summary>Provides conversions between <see cref="Action"/> and <see cref="Func{TResult}"/> delegate families
/// using <see cref="Unit"/> as the return type, bridging the void / value-type gap in the .NET type system.
///</summary>
public static partial class UnitConvert
{
   ///<summary>Executes <paramref name="this"/> and returns <see cref="Unit.Value"/>.</summary>
   public static Unit Invoke(Action @this)
   {
      @this();
      return Unit.Value;
   }
}
