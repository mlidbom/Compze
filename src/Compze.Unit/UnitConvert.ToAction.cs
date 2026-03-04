namespace Compze.Unit;

public static partial class UnitConvert
{
   ///<summary>Converts a <see cref="Func{TResult}"/> returning <see cref="Unit"/> to an <see cref="Action"/>.</summary>
   public static Action ToAction(this Func<Unit> @this) =>
      () => @this();

   ///<summary>Converts a <see cref="Func{T, TResult}"/> returning <see cref="Unit"/> to an <see cref="Action{T}"/>.</summary>
   public static Action<TParam> ToAction<TParam>(this Func<TParam, Unit> @this) =>
      param => @this(param);

   ///<summary>Converts a <see cref="Func{T1, T2, TResult}"/> returning <see cref="Unit"/> to an <see cref="Action{T1, T2}"/>.</summary>
   public static Action<TParam1, TParam2> ToAction<TParam1, TParam2>(this Func<TParam1, TParam2, Unit> @this) =>
      (param, param2) => @this(param, param2);

}
