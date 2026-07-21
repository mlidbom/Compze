using System.Runtime.CompilerServices;
using Compze.Internals.SystemCE.ReflectionCE;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using JetBrains.Annotations;
using Compze.Must.Private;

namespace Compze.Must;

// ReSharper disable InconsistentNaming
/// <summary>A captured synchronous action awaiting a throw-assertion, produced by <see cref="MustActions.Invoking(System.Action, string)"/>.</summary>
/// <param name="action">The action to invoke when the assertion runs.</param>
/// <param name="expression">The captured source text of the action.</param>
public class ActionSpec(Action action, string expression)
{
   readonly Action _action = action;
   readonly string _expression = expression;
   /// <summary>Begins an assertion chain over the captured action, e.g. <c>.Must().Throw&lt;TException&gt;()</c>.</summary>
   public IAssertionContext<Action> Must() => new AssertionContext<Action>(_action, _expression);
}

/// <summary>A captured asynchronous action awaiting a throw-assertion, produced by <see cref="MustActions.InvokingAsync(System.Func{System.Threading.Tasks.Task}, string)"/>.</summary>
/// <param name="action">The async action to invoke when the assertion runs.</param>
/// <param name="expression">The captured source text of the action.</param>
public class AsyncActionSpec(Func<Task> action, string expression)
{
   readonly Func<Task> _action = action;
   readonly string _expression = expression;
   /// <summary>Begins an assertion chain over the captured async action, e.g. <c>.Must().ThrowAsync&lt;TException&gt;()</c>.</summary>
   public IAssertionContext<Func<Task>> Must() => new AssertionContext<Func<Task>>(_action, _expression);
}

/// <summary>Entry points for asserting that a delegate throws (or does not throw): <see cref="Invoking(System.Action, string)"/> and <see cref="InvokingAsync(System.Func{System.Threading.Tasks.Task}, string)"/>.</summary>
public static class MustActions
{
   /// <summary>Captures the value-returning <paramref name="action"/> so it can be asserted to throw.</summary>
   public static ActionSpec Invoking<T>([InstantHandle] Func<T> action, [CallerArgumentExpression(nameof(action))] string expression = null!) => new(() => action(), expression);
   /// <summary>Captures <paramref name="action"/> so it can be asserted to throw.</summary>
   public static ActionSpec Invoking([InstantHandle] Action action, [CallerArgumentExpression(nameof(action))] string expression = null!) => new(action, expression);

   /// <summary>Captures the invocation of <paramref name="action"/> on <paramref name="subject"/> so it can be asserted to throw.</summary>
   public static ActionSpec Invoking<T>(this T subject, [InstantHandle] Action<T> action, [CallerArgumentExpression(nameof(action))] string expression = null!)
      => Invoking(() => action(subject), expression);

   /// <summary>Captures the invocation of <paramref name="func"/> on <paramref name="subject"/> so it can be asserted to throw.</summary>
   public static ActionSpec Invoking<T, TResult>(this T subject, Func<T, TResult> func, [CallerArgumentExpression(nameof(func))] string expression = null!)
      => Invoking(() => func(subject), expression);

    /// <summary>Begins an assertion chain over an async <paramref name="action"/>, e.g. <c>.Must().ThrowAsync&lt;TException&gt;()</c>.</summary>
    public static IAssertionContext<Func<Task>> Must([InstantHandle] this Func<Task> action, [CallerArgumentExpression(nameof(action))] string expression = null!) => InvokingAsync(action, expression).Must();
    /// <summary>Begins an assertion chain over an async <paramref name="func"/>, e.g. <c>.Must().ThrowAsync&lt;TException&gt;()</c>.</summary>
    public static IAssertionContext<Func<Task>> Must<T>([InstantHandle] this Func<Task<T>> func, [CallerArgumentExpression(nameof(func))] string expression = null!) => InvokingAsync(func, expression).Must();

    /// <summary>Captures the async <paramref name="action"/> so it can be asserted to throw.</summary>
    public static AsyncActionSpec InvokingAsync([InstantHandle] Func<Task> action, [CallerArgumentExpression(nameof(action))] string expression = null!) => new(action, expression);

   /// <summary>Captures the async invocation of <paramref name="action"/> on <paramref name="subject"/> so it can be asserted to throw.</summary>
   public static AsyncActionSpec InvokingAsync<T>(this T subject, [InstantHandle] Func<T, Task> action, [CallerArgumentExpression(nameof(action))] string expression = null!)
      => InvokingAsync(() => action(subject), expression);

   /// <summary>Asserts that invoking the captured action throws <typeparamref name="TException"/>, returning the caught exception for further assertions.</summary>
   public static CaughtException<TException> Throw<TException>(this IAssertionContext<Action> context)
      where TException : Exception
   {
      try
      {
         context.Actual();
      }
      catch(TException caught)
      {
         return new CaughtException<TException>(caught);
      }
      catch(Exception unexpected)
      {
         throw new AssertionFailedException($"""
                                             {context.ThrowAssertionFailureHeading(typeof(TException))}
                                             Expected a {typeof(TException).GetFullNameCompilable()} 
                                             but got a {unexpected.GetType().GetFullNameCompilable()}
                                             {AssertionContext.Separator}
                                             """,
                                            unexpected);
      }

      throw new AssertionFailedException($"""
                                          {context.ThrowAssertionFailureHeading(typeof(TException))}
                                          Expected a {typeof(TException).GetFullNameCompilable()}, but no exception was thrown
                                          {AssertionContext.Separator}
                                          """);
   }

   /// <summary>Asserts that awaiting the captured async action throws <typeparamref name="TException"/>, returning the caught exception for further assertions.</summary>
   public static async Task<CaughtException<TException>> ThrowAsync<TException>(this IAssertionContext<Func<Task>> context)
      where TException : Exception
   {
      try
      {
         await context.Actual().caf();
      }
      catch(TException caught)
      {
         return new CaughtException<TException>(caught);
      }
      catch(Exception unexpected)
      {
         throw new AssertionFailedException($"""
                                             {context.ThrowAssertionFailureHeading(typeof(TException))}
                                             Expected a {typeof(TException).GetFullNameCompilable()}
                                             but got a {unexpected.GetType().GetFullNameCompilable()}
                                             {AssertionContext.Separator}
                                             """,
                                            unexpected);
      }

      throw new AssertionFailedException($"""
                                          {context.ThrowAssertionFailureHeading(typeof(TException))}
                                          Expected a {typeof(TException).GetFullNameCompilable()}, but no exception was thrown
                                          {AssertionContext.Separator}
                                          """);
   }
}

#pragma warning disable CA1711 //I don't much care that the class name ends with Exception
/// <summary>The exception captured by <see cref="MustActions.Throw{TException}(IAssertionContext{System.Action})"/> / <see cref="MustActions.ThrowAsync{TException}(IAssertionContext{System.Func{System.Threading.Tasks.Task}})"/>, exposing it for further assertions.</summary>
/// <param name="exception">The caught exception.</param>
public class CaughtException<TException>(TException exception)
   where TException : Exception
{
   /// <summary>The caught exception, for asserting on its properties.</summary>
   public TException Which { get; } = exception;
}
