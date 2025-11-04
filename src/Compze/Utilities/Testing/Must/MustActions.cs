using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Compze.Utilities.SystemCE.ReflectionCE;
using Compze.Utilities.SystemCE.ThreadingCE.TasksCE;

namespace Compze.Utilities.Testing.Must;

// ReSharper disable InconsistentNaming
public class ActionSpec(Action action, string expression)
{
   readonly Action _action = action;
   readonly string _expression = expression;
   public IMust<Action> Must() => new Must<Action>(_action, _expression);
}

public class AsyncActionSpec(Func<Task> action, string expression)
{
   readonly Func<Task> _action = action;
   readonly string _expression = expression;
   public IMust<Func<Task>> Must() => new Must<Func<Task>>(_action, _expression);
}

public static class MustActions
{
   public static IMust<Action> Must<T>(this Func<T> func, [CallerArgumentExpression(nameof(func))] string expression = null!) => Invoking(func, expression).Must();
   public static IMust<Action> Must(this Action action, [CallerArgumentExpression(nameof(action))] string expression = null!) => Invoking(action, expression).Must();

   public static ActionSpec Invoking<T>(Func<T> action, [CallerArgumentExpression(nameof(action))] string expression = null!) => new(() => action(), expression);
   public static ActionSpec Invoking(Action action, [CallerArgumentExpression(nameof(action))] string expression = null!) => new(action, expression);

   public static ActionSpec Invoking<T>(this T subject, Action<T> action, [CallerArgumentExpression(nameof(action))] string expression = null!)
      => Invoking(() => action(subject), expression);

   public static ActionSpec Invoking<T, TResult>(this T subject, Func<T, TResult> func, [CallerArgumentExpression(nameof(func))] string expression = null!)
      => Invoking(() => func(subject), expression);

   public static IMust<Func<Task>> Must(this Func<Task> action, [CallerArgumentExpression(nameof(action))] string expression = null!) => InvokingAsync(action, expression).Must();
   public static IMust<Func<Task>> Must<T>(this Func<Task<T>> func, [CallerArgumentExpression(nameof(func))] string expression = null!) => InvokingAsync(func, expression).Must();

   public static AsyncActionSpec InvokingAsync(Func<Task> action, [CallerArgumentExpression(nameof(action))] string expression = null!) => new(action, expression);

   public static AsyncActionSpec InvokingAsync<T>(this T subject, Func<T, Task> action, [CallerArgumentExpression(nameof(action))] string expression = null!)
      => InvokingAsync(() => action(subject), expression);

   public static CaughtException<TException> Throw<TException>(this IMust<Action> must)
      where TException : Exception
   {
      try
      {
         must.Actual();
      }
      catch(TException caught)
      {
         return new CaughtException<TException>(caught);
      }
      catch(Exception unexpected)
      {
         throw new AssertionFailedException($"""
                                             {must.ThrowAssertionFailureHeading(typeof(TException))}
                                             Expected a {typeof(TException).GetFullNameCompilable()} 
                                             but got a {unexpected.GetType().GetFullNameCompilable()}
                                             {must.Separator}
                                             """,
                                            unexpected);
      }

      throw new AssertionFailedException($"""
                                          {must.ThrowAssertionFailureHeading(typeof(TException))}
                                          Expected a {typeof(TException).GetFullNameCompilable()}, but no exception was thrown
                                          {must.Separator}
                                          """);
   }

   public static async Task<CaughtException<TException>> ThrowAsync<TException>(this IMust<Func<Task>> must)
      where TException : Exception
   {
      try
      {
         await must.Actual().caf();
      }
      catch(TException caught)
      {
         return new CaughtException<TException>(caught);
      }
      catch(Exception unexpected)
      {
         throw new AssertionFailedException($"""
                                             {must.ThrowAssertionFailureHeading(typeof(TException))}
                                             Expected a {typeof(TException).GetFullNameCompilable()}
                                             but got a {unexpected.GetType().GetFullNameCompilable()}
                                             {must.Separator}
                                             """,
                                            unexpected);
      }

      throw new AssertionFailedException($"""
                                          {must.ThrowAssertionFailureHeading(typeof(TException))}
                                          Expected a {typeof(TException).GetFullNameCompilable()}, but no exception was thrown
                                          {must.Separator}
                                          """);
   }
}

#pragma warning disable CA1711 //I don't much care that the class name ends with Exception
public class CaughtException<TException>(TException exception)
   where TException : Exception
{
   readonly TException _exception = exception;
   public TException Which => _exception;
   public TException That => _exception;
}

static class InvokingMustThrowExtensions
{
   public static string ThrowAssertionFailureHeading(this IMust<Func<Task>> must, Type expectedException)
   {
      return $"""
              {Must.Separator}
              Failing assertion:
              {Must.Separator}
              InvokingAsync({must.Expression}).Must().Throw<{expectedException.Name}>()
              {Must.Separator}
              """;
   }

   public static string ThrowAssertionFailureHeading(this IMust<Action> must, Type expectedException)
   {
      return $"""
              {Must.Separator}
              Failing assertion:
              {Must.Separator}
              Invoking({must.Expression}).Must().Throw<{expectedException.Name}>()
              {Must.Separator}
              """;
   }
}
