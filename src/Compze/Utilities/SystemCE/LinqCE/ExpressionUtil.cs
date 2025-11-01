using System;
using System.Linq.Expressions;
using System.Reflection;

namespace Compze.Utilities.SystemCE.LinqCE;

///<summary>Extracts member names from expressions</summary>
static class ExpressionUtil
{
   public static MemberInfo ExtractFinalMemberInfo(this LambdaExpression lambda) =>
      lambda.Body.ExtractFinalMemberInfo();

   public static MemberInfo ExtractFinalMemberInfo(this Expression expression) =>
      expression.ExtractFinalMemberAccessExpression().Member;

   public static MemberExpression ExtractFinalMemberAccessExpression(this Expression expression)
   {
      try
      {
         return expression is UnaryExpression unaryExpression
                   ? (MemberExpression)unaryExpression.Operand
                   : (MemberExpression)expression;
      }
      catch(InvalidCastException ex)
      {
         throw new ArgumentException("The expression must end with accessing a property or field", nameof(expression), ex);
      }
   }
}
