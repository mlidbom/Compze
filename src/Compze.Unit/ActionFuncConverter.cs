using System;
using System.Threading.Tasks;

namespace Compze.Unit;

public static class ActionFuncConverter
{
   //<action-to-func>

   public static Func<Unit> AsFunc(this Action @this) =>
      () =>
      {
         @this();
         return Unit.Value;
      };

   public static Func<TParam, Unit> AsFunc<TParam>(this Action<TParam> @this) =>
      param => Unit.From(() => @this(param));

   public static Func<TParam, TParam2, Unit> AsFunc<TParam, TParam2>(this Action<TParam, TParam2> @this) =>
      (param, param2) => Unit.From(() => @this(param, param2));

   //</action-to-func>

   //<async-action-to-func>

   public static Func<Task<Unit>> AsFunc(this Func<Task> @this) =>
      async () =>
      {
         await @this().ConfigureAwait(false);
         return Unit.Value;
      };

   public static Func<TParam, Task<Unit>> AsFunc<TParam>(this Func<TParam, Task> @this) =>
      async param =>
      {
         await @this(param).ConfigureAwait(false);
         return Unit.Value;
      };

   public static Func<TParam, TParam2, Task<Unit>> AsFunc<TParam, TParam2>(this Func<TParam, TParam2, Task> @this) =>
      async (param, param2) =>
      {
         await @this(param, param2).ConfigureAwait(false);
         return Unit.Value;
      };

   //</async-action-to-func>

   //<func-to-action>

   public static Action AsAction(this Func<Unit> @this) =>
      () => @this();

   public static Action<TParam> AsAction<TParam>(this Func<TParam, Unit> @this) =>
      param => @this(param);

   public static Action<TParam, TParam2> AsAction<TParam, TParam2>(this Func<TParam, TParam2, Unit> @this) =>
      (param, param2) => @this(param, param2);

   //</func-to-action>
}
