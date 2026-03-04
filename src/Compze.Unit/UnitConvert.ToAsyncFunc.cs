namespace Compze.Unit;

public static partial class UnitConvert
{
   public static Func<Task<Unit>> ToAsyncFunc(this Func<Task> @this) =>
      async () =>
      {
         await @this().ConfigureAwait(false);
         return Unit.Value;
      };

   public static Func<TParam, Task<Unit>> ToAsyncFunc<TParam>(this Func<TParam, Task> @this) =>
      async param =>
      {
         await @this(param).ConfigureAwait(false);
         return Unit.Value;
      };

   public static Func<TParam1, TParam2, Task<Unit>> ToAsyncFunc<TParam1, TParam2>(this Func<TParam1, TParam2, Task> @this) =>
      async (param, param2) =>
      {
         await @this(param, param2).ConfigureAwait(false);
         return Unit.Value;
      };
}
