using System;
using System.Runtime.CompilerServices;
using Compze.Utilities.SystemCE.ReflectionCE;

namespace Compze.Tests.Infrastructure.Fluent;

public class ActionSpec(Action action, string expression)
{
   readonly Action _action = action;
   readonly string _expression = expression;
   public IMust<Action> Must() => new Must<Action>(_action, _expression);
}

public static class MustActions
{
   public static ActionSpec Invoking(Action action, [CallerArgumentExpression(nameof(action))] string expression = null!) => 
      new ActionSpec(action, expression);


   public static TException Throw<TException>(this IMust<Action> must)
      where TException : Exception
   {
      try
      {
         must.Actual();
      }
      catch(TException caught)
      {
         return caught;
      }
      catch(Exception unexpected)
      {
         throw new AssertionFailedException($"Expected {must.Expression} to throw {typeof(TException).Name} but instead a {unexpected.GetType().GetFullNameCompilable()} was thrown");
      }

      throw new AssertionFailedException($"Expected {must.Expression} to throw {typeof(TException).Name} but no exception was thrown");
   }
}