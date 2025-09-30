using System;
using System.Linq.Expressions;
using static Compze.Contracts.Assert;

namespace Compze.SystemCE.LinqCE;

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
}
