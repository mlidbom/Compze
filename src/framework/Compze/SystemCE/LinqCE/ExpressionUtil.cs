using System;
using System.Linq.Expressions;

namespace Compze.SystemCE.LinqCE;

///<summary>Extracts member names from expressions</summary>
static class ExpressionUtil
{
   public static string ExtractMemberPath<TValue>(Expression<Func<TValue>> func)
   {
      Contracts.Assert.Argument.NotNull(func);
      return ExtractMemberPath((LambdaExpression)func);
   }

   static string ExtractMemberPath(LambdaExpression lambda)
   {
      Contracts.Assert.Argument.NotNull(lambda);
      var memberExpression = lambda.Body is UnaryExpression unaryExpression
                                ? (MemberExpression)unaryExpression.Operand
                                : (MemberExpression)lambda.Body;

      return $"{memberExpression.Member.DeclaringType!.FullName}.{memberExpression.Member.Name}";
   }
}
