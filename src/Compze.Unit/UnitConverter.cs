// ReSharper disable MemberCanBeInternal

namespace Compze.Unit;

public static class UnitConverterExtensions
{
   extension(Unit)
   {
      ///<summary>Executes the action and returns Unit
      /// <code>
      ///   Unit Method() => Unit.Invoke(() => AVoidMethod())
      /// </code>
      /// </summary>
      public static Unit Invoke(Action action)
      {
         action();
         return Unit.Value;
      }

      ///<summary>Creates a <see cref="Func{TResult}"/> returning <see cref="Unit"/> from an <see cref="Action"/>.
      /// <code>
      ///   MethodThatTakesFunc(Unit.Func(anInstance.VoidMethod));
      /// </code>
      /// </summary>
      public static Func<Unit> Func(Action action) => action.ToFunc();

      ///<inheritdoc cref="Func(System.Action)"/>
      public static Func<TParam, Unit> Func<TParam>(Action<TParam> action) => action.ToFunc();

      ///<inheritdoc cref="Func(System.Action)"/>
      public static Func<TParam, TParam2, Unit> Func<TParam, TParam2>(Action<TParam, TParam2> action) => action.ToFunc();

      ///<summary>Creates a <see cref="Func{TResult}"/> returning <see cref="Task"/> of <see cref="Unit"/> from a task-returning async method group.
      /// <code>
      ///   MethodThatTakesAsyncFunc(Unit.AsyncFunc(anInstance.TaskReturningMethod));
      /// </code>
      /// </summary>
      public static Func<Task<Unit>> AsyncFunc(Func<Task> action) => action.ToAsyncFunc();

      ///<inheritdoc cref="AsyncFunc(System.Func{System.Threading.Tasks.Task})"/>
      public static Func<TParam, Task<Unit>> AsyncFunc<TParam>(Func<TParam, Task> action) => action.ToAsyncFunc();

      ///<inheritdoc cref="AsyncFunc(System.Func{System.Threading.Tasks.Task})"/>
      public static Func<TParam, TParam2, Task<Unit>> AsyncFunc<TParam, TParam2>(Func<TParam, TParam2, Task> action) => action.ToAsyncFunc();

      ///<summary>Creates an <see cref="Action"/> from a <see cref="Func{TResult}"/> returning <see cref="Unit"/>.
      /// <code>
      ///   MethodThatTakesAction(Unit.Action(anInstance.FuncReturningUnit));
      /// </code>
      /// </summary>
      public static Action Action(Func<Unit> func) => func.ToAction();

      ///<inheritdoc cref="Action(System.Func{Unit})"/>
      public static Action<TParam> Action<TParam>(Func<TParam, Unit> func) => func.ToAction();

      ///<inheritdoc cref="Action(System.Func{Unit})"/>
      public static Action<TParam, TParam2> Action<TParam, TParam2>(Func<TParam, TParam2, Unit> func) => func.ToAction();

      ///<summary>Creates a <see cref="Func{TResult}"/> returning <see cref="Task"/> from a <see cref="Func{TResult}"/> returning <see cref="Task"/> of <see cref="Unit"/>.
      /// <code>
      ///   MethodThatTakesAsyncAction(Unit.AsyncAction(anInstance.AsyncFuncReturningTaskOfUnit));
      /// </code>
      /// </summary>
      public static Func<Task> AsyncAction(Func<Task<Unit>> func) => func.ToAsyncAction();

      ///<inheritdoc cref="AsyncAction(System.Func{System.Threading.Tasks.Task{Unit}})"/>
      public static Func<TParam, Task> AsyncAction<TParam>(Func<TParam, Task<Unit>> func) => func.ToAsyncAction();

      ///<inheritdoc cref="AsyncAction(System.Func{System.Threading.Tasks.Task{Unit}})"/>
      public static Func<TParam, TParam2, Task> AsyncAction<TParam, TParam2>(Func<TParam, TParam2, Task<Unit>> func) => func.ToAsyncAction();
   }
}
