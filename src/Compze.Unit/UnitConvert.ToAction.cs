namespace Compze.Unit;

public static partial class UnitConvert
{
   public static Action ToAction(this Func<Unit> @this) =>
      () => @this();

   public static Action<TParam> ToAction<TParam>(this Func<TParam, Unit> @this) =>
      param => @this(param);

   public static Action<TParam1, TParam2> ToAction<TParam1, TParam2>(this Func<TParam1, TParam2, Unit> @this) =>
      (param, param2) => @this(param, param2);

}
