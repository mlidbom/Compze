using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using Compze.Tests.Infrastructure.Fluent.Serialization;
using Compze.Utilities.SystemCE;
using DiffPlex.Renderer;
using Newtonsoft.Json;

namespace Compze.Tests.Infrastructure.Fluent;

public class EquivalencyConfig<TValue>
{
   internal HashSet<(Type DeclaringType, string MemberName)> ExcludedMembers { get; } = new();

   public EquivalencyConfig<TValue> Excluding<TMember>(Expression<Func<TValue, TMember>> memberExpression)
   {
      var (declaringType, memberName) = ExtractMemberInfo(memberExpression.Body);
      ExcludedMembers.Add((declaringType, memberName));
      return this;
   }

   static (Type DeclaringType, string MemberName) ExtractMemberInfo(Expression expression)
   {
      // Walk through the expression tree to find the final member access
      // This handles: obj.Property, obj.Method().Property, obj[0].Property, etc.
      Expression? current = expression;
      
      while (current != null)
      {
         switch (current)
         {
            case MemberExpression memberExpr:
               // This is a member access - return its declaring type and name
               var declaringType = memberExpr.Member.DeclaringType 
                  ?? throw new ArgumentException("Member must have a declaring type", nameof(expression));
               return (declaringType, memberExpr.Member.Name);
               
            case UnaryExpression unaryExpr:
               // Handle conversions (e.g., boxing, casting)
               current = unaryExpr.Operand;
               continue;
               
            case MethodCallExpression methodCall:
               // Check if this is an indexer call (get_Item)
               if (methodCall.Method.Name == "get_Item" && methodCall.Object != null)
               {
                  // Continue from the object being indexed
                  current = methodCall.Object;
                  continue;
               }
               
               // For other method calls, the expression must continue to a member access
               throw new ArgumentException(
                  "Expression must end with a property or field access, not a method call. " +
                  "Example: obj => obj.Method().Property (not obj => obj.Method())",
                  nameof(expression));
               
            default:
               throw new ArgumentException(
                  $"Expression must end with a property or field access. Unsupported expression type: {current.GetType().Name}",
                  nameof(expression));
         }
      }
      
      throw new ArgumentException("Expression must end with a property or field access", nameof(expression));
   }
}

public static class ObjectBeEquivalentTo
{
   public static Must<TValue> BeEquivalentTo<TValue>(this Must<TValue> must,
                                                      TValue expected,
                                                      [CallerArgumentExpression(nameof(expected))]
                                                      string expectedExpression = null!)
      => BeEquivalentToCore(must, expected, expectedExpression, TestingJsonSettings.AllMembers);

   public static Must<TValue> BeEquivalentTo<TValue>(this Must<TValue> must,
                                                      TValue expected,
                                                      Func<EquivalencyConfig<TValue>, EquivalencyConfig<TValue>> config,
                                                      [CallerArgumentExpression(nameof(expected))]
                                                      string expectedExpression = null!)
   {
      var equivalencyConfig = config(new EquivalencyConfig<TValue>());
      var settings = TestingJsonSettings.CreateSettingsWithExclusions(TestingJsonSettings.AllMembers, equivalencyConfig.ExcludedMembers);
      return BeEquivalentToCore(must, expected, expectedExpression, settings);
   }

   public static Must<TValue> BeEquivalentToInternal<TValue>(this Must<TValue> must,
                                                              TValue expected,
                                                              [CallerArgumentExpression(nameof(expected))]
                                                              string expectedExpression = null!)
      => BeEquivalentToCore(must, expected, expectedExpression, TestingJsonSettings.InternalAndPublicMembers);

   public static Must<TValue> BeEquivalentToPublic<TValue>(this Must<TValue> must,
                                                            TValue expected,
                                                            [CallerArgumentExpression(nameof(expected))]
                                                            string expectedExpression = null!)
      => BeEquivalentToCore(must, expected, expectedExpression, TestingJsonSettings.PublicMembers);

   static Must<TValue> BeEquivalentToCore<TValue>(Must<TValue> must,
                                                   TValue expected,
                                                   string expectedExpression,
                                                   JsonSerializerSettings settings)
   {
      var actualJson = JsonConvert.SerializeObject(must.Actual, settings);
      var expectedJson = JsonConvert.SerializeObject(expected, settings);

      return must.Satisfy(it => actualJson == expectedJson,
                          () =>
                             $"""
                              {must.Separator}
                              expected the object returned by the expression: 
                              {must.Separator}
                              {must.Expression}
                              {must.Separator}
                              to be equivalent to the object returned by the expression:
                              {must.Separator}
                              {must.NormalizeExpressionIndentation(expectedExpression)}
                              {must.Separator}
                              But it resulted in the Diff:
                              {must.Separator}
                              {UnidiffRenderer.GenerateUnidiff(oldText: expectedJson, newText: actualJson, oldFileName: "expected", newFileName: "actual")}
                              {must.Separator}
                              Actual was:
                              {must.Separator}
                              {actualJson}
                              {must.Separator}
                              Expected was:
                              {must.Separator}
                              {expectedJson}
                              {must.Separator}
                              """);
   }
}
