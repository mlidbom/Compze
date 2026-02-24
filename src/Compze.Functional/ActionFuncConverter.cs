using System;
using System.Threading.Tasks;

namespace Compze.Functional;

public static class ActionFuncConverter
{
   //<action-to-func>

   public static Func<unit> AsFunc(this Action @this) =>
      () =>
      {
         @this();
         return unit.Value;
      };

   public static Func<TParam, unit> AsFunc<TParam>(this Action<TParam> @this) =>
      param => unit.From(() => @this(param));

   public static Func<TParam, TParam2, unit> AsFunc<TParam, TParam2>(this Action<TParam, TParam2> @this) =>
      (param, param2) => unit.From(() => @this(param, param2));

   //</action-to-func>

   //<async-action-to-func>

   public static Func<Task<unit>> AsFunc(this Func<Task> @this) =>
      async () =>
      {
         await @this().ConfigureAwait(false);
         return unit.Value;
      };

   public static Func<TParam, Task<unit>> AsFunc<TParam>(this Func<TParam, Task> @this) =>
      async param =>
      {
         await @this(param).ConfigureAwait(false);
         return unit.Value;
      };

   public static Func<TParam, TParam2, Task<unit>> AsFunc<TParam, TParam2>(this Func<TParam, TParam2, Task> @this) =>
      async (param, param2) =>
      {
         await @this(param, param2).ConfigureAwait(false);
         return unit.Value;
      };

   //</async-action-to-func>

   //<func-to-action>

   public static Action AsAction(this Func<unit> @this) =>
      () => @this();

   public static Action<TParam> AsAction<TParam>(this Func<TParam, unit> @this) =>
      param => @this(param);

   public static Action<TParam, TParam2> AsAction<TParam, TParam2>(this Func<TParam, TParam2, unit> @this) =>
      (param, param2) => @this(param, param2);

   //<func-to-action>
}
