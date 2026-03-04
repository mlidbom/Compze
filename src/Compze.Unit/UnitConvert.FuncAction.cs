namespace Compze.Unit;

public static partial class UnitConvert
{
   //ToFunc

   public static Func<Unit> ToFunc(this Action @this) =>
      () =>
      {
         @this();
         return Unit.Value;
      };

   public static Func<TParam, Unit> ToFunc<TParam>(this Action<TParam> @this) =>
      param => Unit.Invoke(() => @this(param));

   public static Func<TParam1, TParam2, Unit> ToFunc<TParam1, TParam2>(this Action<TParam1, TParam2> @this) =>
      (param, param2) => Unit.Invoke(() => @this(param, param2));

   //ToAction

   public static Action ToAction(this Func<Unit> @this) =>
      () => @this();

   public static Action<TParam> ToAction<TParam>(this Func<TParam, Unit> @this) =>
      param => @this(param);

   public static Action<TParam1, TParam2> ToAction<TParam1, TParam2>(this Func<TParam1, TParam2, Unit> @this) =>
      (param, param2) => @this(param, param2);

}
