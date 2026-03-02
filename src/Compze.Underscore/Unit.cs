using System;
using System.Threading.Tasks;

namespace Compze.Underscore;

///<summary>The functional programming unit concept.
/// Unifies <see cref="Func{TResult}"/> and <see cref="Action"/>.
/// Named unit, lowercase, in the hope that one day a language keyword will appear and this will ease migration.
/// Simply return unit.Value instead of void from methods with no return value,
/// or use <see cref="From"/> to avoid that pesky extra line:
/// <code>
///   public unit DoSomething() => unit.From(() =>
///   {
///      //Do something here
///   });
/// </code>
/// </summary>
#pragma warning disable CS8981 // The type name only contains lower-cased ascii characters. Such names may become reserved for the language.
// ReSharper disable once InconsistentNaming
public readonly struct unit : IEquatable<unit>
#pragma warning restore CS8981 // The type name only contains lower-cased ascii characters. Such names may become reserved for the language.
{
   // ReSharper disable once MemberCanBeInternal
   public static readonly unit Value = default;

   ///<summary>Executes the task and returns unit making for easily returning unit without extra lines:
   /// <code>
   ///   unit Method() => unit.From(() => DoSomething())
   /// </code>
   /// </summary>
   public static unit From(Action action)
   {
      action();
      return Value;
   }

   ///<summary>Creates a <see cref="Func{TResult}"/> returning <see cref="unit"/> from an <see cref="Action"/>, making it easy to call methods that take a <see cref="Func{TResult}"/> when what you have is a method group.
   /// <code>
   ///   ITakeFunc(unit.Func(anInstance.VoidMethod));
   /// </code>
   /// </summary>
   public static Func<unit> Func(Action action) =>
      () =>
      {
         action();
         return Value;
      };

   ///<inheritdoc cref="Func(Action)"/>
   public static Func<TParam, unit> Func<TParam>(Action<TParam> action) =>
      param =>
      {
         action(param);
         return Value;
      };

   ///<inheritdoc cref="Func(Action)"/>
   public static Func<TParam, TParam2, unit> Func<TParam, TParam2>(Action<TParam, TParam2> action) =>
      (param, param2) =>
      {
         action(param, param2);
         return Value;
      };

   ///<summary>Creates a <see cref="Func{TResult}"/> returning <see cref="Task{T}"/> of <see cref="unit"/> from a <see cref="Func{TResult}"/> returning <see cref="Task"/>, making it easy to call methods that take a typed async func when what you have is a void-returning async method group.
   /// <code>
   ///   ITakeAsyncFunc(unit.AsyncFunc(anInstance.AsyncVoidMethod));
   /// </code>
   /// </summary>
   public static Func<Task<unit>> AsyncFunc(Func<Task> action) =>
      async () =>
      {
         await action().ConfigureAwait(false);
         return Value;
      };

   ///<inheritdoc cref="AsyncFunc(System.Func{System.Threading.Tasks.Task})"/>
   public static Func<TParam, Task<unit>> AsyncFunc<TParam>(Func<TParam, Task> action) =>
      async param =>
      {
         await action(param).ConfigureAwait(false);
         return Value;
      };

   ///<inheritdoc cref="AsyncFunc(System.Func{System.Threading.Tasks.Task})"/>
   public static Func<TParam, TParam2, Task<unit>> AsyncFunc<TParam, TParam2>(Func<TParam, TParam2, Task> action) =>
      async (param, param2) =>
      {
         await action(param, param2).ConfigureAwait(false);
         return Value;
      };

   public override string ToString() => "()";

   //we manually implement equality, not because it's required for correctness, but for performance,
   //this should be a bit more performant than the built-in,
   //and the random hashcode better than the default for a zero value struct due to less risk of ending up in an over-populated bucket in hash based collections.
   public bool Equals(unit _) => true;
   public override bool Equals(object? obj) => obj is unit;
   // ReSharper disable UnusedParameter.Global
   public static bool operator ==(unit _, unit __) => true;
   public static bool operator !=(unit _, unit __) => false;
   // ReSharper restore UnusedParameter.Global

   public override int GetHashCode() => 392576489;
}
