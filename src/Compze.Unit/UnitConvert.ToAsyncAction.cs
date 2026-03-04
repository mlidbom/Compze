namespace Compze.Unit;

public static partial class UnitConvert
{
   ///<summary>Converts a <see cref="Func{TResult}"/> returning <see cref="Task{TResult}"/> of <see cref="Unit"/> to one returning <see cref="Task"/>.</summary>
   public static Func<Task> ToAsyncAction(this Func<Task<Unit>> @this) =>
      async () => await @this().ConfigureAwait(false);

   ///<summary>Converts a <see cref="Func{T, TResult}"/> returning <see cref="Task{TResult}"/> of <see cref="Unit"/> to one returning <see cref="Task"/>.</summary>
   public static Func<TParam, Task> ToAsyncAction<TParam>(this Func<TParam, Task<Unit>> @this) =>
      async param => await @this(param).ConfigureAwait(false);

   ///<summary>Converts a <see cref="Func{T1, T2, TResult}"/> returning <see cref="Task{TResult}"/> of <see cref="Unit"/> to one returning <see cref="Task"/>.</summary>
   public static Func<TParam1, TParam2, Task> ToAsyncAction<TParam1, TParam2>(this Func<TParam1, TParam2, Task<Unit>> @this) =>
      async (param, param2) => await @this(param, param2).ConfigureAwait(false);
}
