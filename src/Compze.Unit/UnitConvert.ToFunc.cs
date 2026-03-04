namespace Compze.Unit;

public static partial class UnitConvert
{
   public static Func<Unit> ToFunc(this Action @this) =>
      () =>
      {
         @this();
         return Unit.Value;
      };

   public static Func<TParam, Unit> ToFunc<TParam>(this Action<TParam> @this) =>
      param => Invoke(() => @this(param));

   public static Func<TParam1, TParam2, Unit> ToFunc<TParam1, TParam2>(this Action<TParam1, TParam2> @this) =>
      (param, param2) => Invoke(() => @this(param, param2));
}
