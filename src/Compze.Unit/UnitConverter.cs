// ReSharper disable MemberCanBeInternal

namespace Compze.Unit;

public static class UnitConverterExtensions
{
   extension(Unit)
   {
      ///<summary>Executes the action and returns Unit
      /// <code>
      ///   Unit Method() => Unit.From(() => AVoidMethod())
      /// </code>
      /// </summary>
      public static Unit From(Action action)
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

      ///<inheritdoc cref="Func(Action)"/>
      public static Func<TParam, Unit> Func<TParam>(Action<TParam> action) => action.ToFunc();

      ///<inheritdoc cref="Func(Action)"/>
      public static Func<TParam, TParam2, Unit> Func<TParam, TParam2>(Action<TParam, TParam2> action) => action.ToFunc();

      ///<summary>Creates a <see cref="Func{TResult}"/> returning <see cref="Task"/> of <see cref="Unit"/> from a task-returning async method group.
      /// <code>
      ///   MethodThatTakesAsyncFunc(Unit.AsyncFunc(anInstance.TaskReturningMethod));
      /// </code>
      /// </summary>
      public static Func<Task<Unit>> AsyncFunc(Func<Task> action) => action.ToFunc();

      ///<inheritdoc cref="AsyncFunc(System.Func{System.Threading.Tasks.Task})"/>
      public static Func<TParam, Task<Unit>> AsyncFunc<TParam>(Func<TParam, Task> action) => action.ToFunc();

      ///<inheritdoc cref="AsyncFunc(System.Func{System.Threading.Tasks.Task})"/>
      public static Func<TParam, TParam2, Task<Unit>> AsyncFunc<TParam, TParam2>(Func<TParam, TParam2, Task> action) => action.ToFunc();
   }
}
