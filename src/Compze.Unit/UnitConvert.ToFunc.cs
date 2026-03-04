namespace Compze.Unit;

public static partial class UnitConvert
{
   ///<summary>Converts an <see cref="Action"/> to a <see cref="Func{TResult}"/> returning <see cref="Unit"/>.</summary>
   public static Func<Unit> ToFunc(this Action @this) =>
      () =>
      {
         @this();
         return Unit.Value;
      };

   ///<summary>Converts an <see cref="Action{T}"/> to a <see cref="Func{T, TResult}"/> returning <see cref="Unit"/>.</summary>
   public static Func<TParam, Unit> ToFunc<TParam>(this Action<TParam> @this) =>
      param => Invoke(() => @this(param));

   ///<summary>Converts an <see cref="Action{T1, T2}"/> to a <see cref="Func{T1, T2, TResult}"/> returning <see cref="Unit"/>.</summary>
   public static Func<TParam1, TParam2, Unit> ToFunc<TParam1, TParam2>(this Action<TParam1, TParam2> @this) =>
      (param, param2) => Invoke(() => @this(param, param2));
}
   