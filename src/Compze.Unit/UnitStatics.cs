namespace Compze.Unit;

///<summary>
/// Use <code>>global using static Compze.Unit.UnitStatics;</code> to make unit available as a value globally.
///then you can write just <code>return unit;</code> instead of <code>return Unit.Value;</code>
/// </summary>
public static class UnitStatics
{
   // ReSharper disable once InconsistentNaming
   /// <summary>alias for Unit.Value intended to be used via <code>>global using static Compze.Unit.UnitStatics;</code></summary>
   public static readonly Unit unit = Unit.Value;
}
