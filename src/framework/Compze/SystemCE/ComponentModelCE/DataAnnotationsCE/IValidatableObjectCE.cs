using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Linq.Expressions;

namespace Compze.SystemCE.ComponentModelCE.DataAnnotationsCE;

///<summary>Extensions for <see cref="IValidatableObject"/> intended to make type safe implementations easy.</summary>
public static class ValidatableObjectCE
{
   static string ExtractMemberName(Expression<Func<object>> accessor)
   {
      Contracts.Assert.Argument.NotNull(accessor);

      var expr = accessor.Body;
      while (expr.NodeType is ExpressionType.Convert or ExpressionType.ConvertChecked)
         expr = ((UnaryExpression)expr).Operand;

      if (expr is not MemberExpression expression)
         throw new ArgumentException("Arguments must be of the form '() => SomeMember'.");
      return expression.Member.Name;
   }

   ///<summary>Creates an <see cref="ValidationResult"/> by extracting the invalid member(s) name from the supplied expression(s)</summary>///<summary>Enumerates the lines in a streamreader.</summary>
   static ValidationResult CreateValidationResult(this IValidatableObject me, string message, IEnumerable<Expression<Func<object>>> members)
   {
      Contracts.Assert.Argument.NotNull(me).NotNull(message).NotNull(members);
      return new ValidationResult(message, members.Select(ExtractMemberName).ToList());
   }

   ///<summary>Creates an <see cref="ValidationResult"/> by extracting the invalid member(s) name from the supplied expression(s)</summary>///<summary>Enumerates the lines in a streamreader.</summary>
   public static ValidationResult CreateValidationResult(this IValidatableObject me, string message, params Expression<Func<object>>[] members)
   {
      Contracts.Assert.Argument.NotNull(me).NotNull(message).NotNull(members);
      return me.CreateValidationResult(message, (IEnumerable<Expression<Func<object>>>)members);
   }
}