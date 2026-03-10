namespace Compze.SystemCE;

///<summary>
/// Use <code>>global using static Compze.Unit.UnitStatics;</code> to make Unit available as a value globally.
///then you can write just <code>return unit;</code> instead of <code>return unit;</code>
/// </summary>
public static class UnitStatics
{
   // ReSharper disable once InconsistentNaming
   /// <summary>alias for Unit.Value intended to be used via <code>>global using static Compze.Unit.UnitStatics;</code></summary>
   public static readonly Unit unit = Unit.Value;
}
