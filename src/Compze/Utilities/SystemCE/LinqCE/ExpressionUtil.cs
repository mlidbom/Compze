using System;
using System.Linq.Expressions;
using System.Reflection;
using static Compze.Utilities.Contracts.Assert;

namespace Compze.Utilities.SystemCE.LinqCE;

///<summary>Extracts member names from expressions</summary>
static class ExpressionUtil
{
   public static string ExtractMemberPath<TValue>(Expression<Func<TValue>> func)
   {
      Argument.NotNull(func);
      return ExtractMemberPath((LambdaExpression)func);
   }

   static string ExtractMemberPath(LambdaExpression lambda)
   {
      Argument.NotNull(lambda);
      var memberExpression = lambda.Body is UnaryExpression unaryExpression
                                ? (MemberExpression)unaryExpression.Operand
                                : (MemberExpression)lambda.Body;

      return $"{memberExpression.Member.DeclaringType!.FullName}.{memberExpression.Member.Name}";
   }

   public static MemberInfo ExtractFinalMemberInfo(this Expression expression) =>
      expression.ExtractFinalMemberAccessExpression().Member;

   public static MemberExpression ExtractFinalMemberAccessExpression(this Expression expression)
   {
      // Walk through the expression tree to find the final member access
      // This handles: obj.Property, obj.Method().Property, obj[0].Property, etc.
      var current = expression;

      while(current != null)
      {
         switch(current)
         {
            case MemberExpression memberExpr:
               if(memberExpr.Member.DeclaringType == null)
                  throw new ArgumentException("Member must have a declaring type", nameof(expression));

               return memberExpr;

            case UnaryExpression unaryExpr: // boxing, casting etc
               current = unaryExpr.Operand;
               continue;

            case MethodCallExpression methodCall:

               if(methodCall.Method.Name == "get_Item" && methodCall.Object != null) // Check if this is an indexer call (get_Item)
               {
                  current = methodCall.Object; // Continue from the object being indexed
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
