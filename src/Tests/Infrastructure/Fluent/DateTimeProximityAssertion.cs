using System;
using System.Runtime.CompilerServices;
// ReSharper disable PrimaryConstructorParameterCaptureDisallowed

namespace Compze.Tests.Infrastructure.Fluent;

public static class DateTimeProximityAssertions
{
   public static DateTimeProximityAssertion BeWithin(this Must<DateTime> must, TimeSpan tolerance, [CallerArgumentExpression(nameof(tolerance))] string toleranceExpression = null!) =>
      new(must, tolerance, toleranceExpression);
}

public class DateTimeProximityAssertion(Must<DateTime> must, TimeSpan tolerance, string toleranceExpression)
{
   readonly string _toleranceExpression = toleranceExpression;
   readonly Must<DateTime> _must = must;

   public Must<DateTime> Before(DateTime mustBeBefore, [CallerArgumentExpression(nameof(mustBeBefore))] string referenceExpression = null!)
   {
      return _must.Satisfy(
         it => it >= mustBeBefore - tolerance && it <= mustBeBefore,
         usedArguments:
         [
            new(nameof(tolerance), _toleranceExpression, tolerance),
            new(nameof(mustBeBefore), referenceExpression, mustBeBefore)
         ]);
   }

   public Must<DateTime> After(DateTime mustBeAfter, [CallerArgumentExpression(nameof(mustBeAfter))] string referenceExpression = null!)
   {
      return _must.Satisfy(
         it => it >= mustBeAfter && it <= mustBeAfter + tolerance,
         usedArguments:
         [
            new(nameof(tolerance), _toleranceExpression, tolerance),
            new(nameof(mustBeAfter), referenceExpression, mustBeAfter)
         ]);
   }
}
