using System;
using System.Threading.Tasks;

namespace Compze.Unit;

///<summary>The functional programming unit concept.
/// Unifies <see cref="Func{TResult}"/> and <see cref="Action"/>.
/// Simply return Unit.Value instead of void from methods with no return value,
/// or use <see cref="From"/> to avoid that pesky extra line:
/// <code>
///   public Unit DoSomething() => Unit.From(() =>
///   {
///      //Do something here
///   });
/// </code>
/// We personally prefer using a global using alias that maps this to the lowercase name:
/// <c>global using unit = Compze.Unit.Unit;</c>
/// </summary>
public readonly struct Unit : IEquatable<Unit>
{
   public static readonly Unit Value = default;

   ///<summary>Executes the action and returns Unit making for easily returning Unit without extra lines:
   /// <code>
   ///   Unit Method() => Unit.From(() => DoSomething())
   /// </code>
   /// </summary>
   public static Unit From(Action action)
   {
      action();
      return Value;
   }

   ///<summary>Creates a <see cref="Func{TResult}"/> returning <see cref="Unit"/> from an <see cref="Action"/>, making it easy to call methods that take a <see cref="Func{TResult}"/> when what you have is a method group.
   /// <code>
   ///   ITakeFunc(Unit.Func(anInstance.VoidMethod));
   /// </code>
   /// </summary>
   public static Func<Unit> Func(Action action) => action.ToFunc();

   ///<inheritdoc cref="Func(Action)"/>
   public static Func<TParam, Unit> Func<TParam>(Action<TParam> action) => action.ToFunc();

   ///<inheritdoc cref="Func(Action)"/>
   public static Func<TParam, TParam2, Unit> Func<TParam, TParam2>(Action<TParam, TParam2> action) => action.ToFunc();

   ///<summary>Creates a <see cref="Func{TResult}"/> returning <see cref="Task{T}"/> of <see cref="Unit"/> from a <see cref="Func{TResult}"/> returning <see cref="Task"/>, making it easy to call methods that take a typed async func when what you have is a void-returning async method group.
   /// <code>
   ///   ITakeAsyncFunc(Unit.AsyncFunc(anInstance.AsyncVoidMethod));
   /// </code>
   /// </summary>
   public static Func<Task<Unit>> AsyncFunc(Func<Task> action) => action.ToFunc();

   ///<inheritdoc cref="AsyncFunc(System.Func{System.Threading.Tasks.Task})"/>
   public static Func<TParam, Task<Unit>> AsyncFunc<TParam>(Func<TParam, Task> action) => action.ToFunc();

   ///<inheritdoc cref="AsyncFunc(System.Func{System.Threading.Tasks.Task})"/>
   public static Func<TParam, TParam2, Task<Unit>> AsyncFunc<TParam, TParam2>(Func<TParam, TParam2, Task> action) => action.ToFunc();

    public override string ToString() => "()";

   //we manually implement equality, not because it's required for correctness, but for performance,
   //this should be a bit more performant than the built-in,
   //and the random hashcode better than the default for a zero value struct due to less risk of ending up in an over-populated bucket in hash based collections.
   public bool Equals(Unit _) => true;
   public override bool Equals(object? obj) => obj is Unit;
   // ReSharper disable UnusedParameter.Global
   public static bool operator ==(Unit _, Unit __) => true;
   public static bool operator !=(Unit _, Unit __) => false;
   // ReSharper restore UnusedParameter.Global

   public override int GetHashCode() => 392576489;
}
