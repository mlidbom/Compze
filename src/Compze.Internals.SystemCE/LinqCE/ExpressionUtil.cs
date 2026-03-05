using System.Linq.Expressions;
using System.Reflection;

namespace Compze.Internals.SystemCE.LinqCE;

///<summary>Extracts member names from expressions</summary>
public static class ExpressionUtil
{
   public static MemberInfo ExtractFinalMemberInfo(this LambdaExpression lambda) =>
      lambda.Body.ExtractFinalMemberInfo();

   static MemberInfo ExtractFinalMemberInfo(this Expression expression) =>
      expression.ExtractFinalMemberAccessExpression().Member;

   static MemberExpression ExtractFinalMemberAccessExpression(this Expression expression)
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
