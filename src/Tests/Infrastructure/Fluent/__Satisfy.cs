using System;
using System.Runtime.CompilerServices;
using Compze.Tests.Infrastructure.Fluent.Serialization;
using Compze.Utilities.SystemCE;
using Newtonsoft.Json;

namespace Compze.Tests.Infrastructure.Fluent;

public static class _Satisfy
{
   public static IMust<T> Satisfy<T>(this IMust<T> context,
                                     Func<T, bool> predicate,
                                     Func<string>? messageOverride = null,
                                     [CallerArgumentExpression(nameof(predicate))]
                                     string predicateExpression = null!)
   {
      if(!predicate(context.Actual))
      {
         if(messageOverride != null)
         {
            throw new AssertionFailedException(messageOverride.Invoke());
         }

         var actualJson = JsonConvert.SerializeObject(context.Actual, TestingJsonSettings.AllMembers);

         var message = $"""
                        expected the expression:
                        {context.Separator}
                        {context.Expression.Indent()}
                        {context.Separator}
                        to Satisfy:
                        {context.Separator}
                        {predicateExpression.Indent()}
                        {context.Separator}

                        Actual.ToString():
                        {context.Separator}
                        {(context.Actual?.ToString() ?? "null").Indent()}
                        {context.Separator}

                        Actual JSON:
                        {context.Separator}
                        {actualJson}
                        {context.Separator}
                        """;

         throw new AssertionFailedException(message);
      }

      return context;
   }
}
