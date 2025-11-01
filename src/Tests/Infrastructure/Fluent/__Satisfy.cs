using Compze.Tests.Infrastructure.Fluent.Serialization;
using Compze.Utilities.SystemCE;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using static Compze.Tests.Infrastructure.Fluent.ObjectEqualityAssertions;

namespace Compze.Tests.Infrastructure.Fluent;

public static class _Satisfy
{
   const string RemoveLine = nameof(RemoveLine);

   public static Must<T> Satisfy<T>(this Must<T> context,
                                     Func<T, bool> predicate,
                                     Func<string>? messageOverride = null,
                                     [CallerArgumentExpression(nameof(predicate))]
                                     string predicateExpression = null!,
                                     Func<T,string>? failureMessage = null,
                                     IEnumerable<AssertionArgumentInfo>? usedArguments = null)
   {
      if(!predicate(context.Actual))
      {
         if(messageOverride != null)
         {
            throw new AssertionFailedException(messageOverride.Invoke());
         }

         var message = $"""
                        {context.Separator}
                        expected the object returned by the expression:
                        {context.Separator}
                        {context.Expression.Indent()}
                        {context.Separator}
                        to Satisfy:
                        {context.Separator}
                        {predicateExpression.Indent()}
                        {context.Separator}
                        {failureMessage?.Invoke(context.Actual) ?? "but it did not"}
                        {context.Separator}
                        The value of: 
                        {context.Expression.Indent()}
                        Was:
                        {context.Separator}
                        ToString():
                        {context.Separator}
                        {context.Actual?.ToString() ?? "null"}
                        {context.Separator}
                        JSON:
                        {context.Separator}
                        {JsonConvert.SerializeObject(context.Actual, TestingJsonSettings.AllMembers)}
                        {context.Separator}
                        """;

         throw new AssertionFailedException(message);
      }

      return context;
   }
}
