using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using Compze.Contracts;

// ReSharper disable PossibleMultipleEnumeration

namespace Compze.Internals.SystemCE.ComponentModelCE.DataAnnotationsCE;

///<summary>Extensions for <see cref="IValidatableObject"/> intended to make type safe implementations easy.</summary>
public static class ValidatableObjectCE
{
   static string ExtractMemberName(Expression<Func<object>> accessor) => Argument.NotNull(accessor).__(() =>
   {
      var expr = accessor.Body;
      while(expr.NodeType is ExpressionType.Convert or ExpressionType.ConvertChecked)
         expr = ((UnaryExpression)expr).Operand;

      if(expr is not MemberExpression expression)
         throw new ArgumentException("Arguments must be of the form '() => SomeMember'.");
      return expression.Member.Name;
   });

   ///<summary>Creates an <see cref="ValidationResult"/> by extracting the invalid member(s) name from the supplied expression(s)</summary>///<summary>Enumerates the lines in a stream reader.</summary>
   static ValidationResult CreateValidationResult(this IValidatableObject me, string message, IEnumerable<Expression<Func<object>>> members) =>
      Argument.NotNull3(me, message, members)
              .__(new ValidationResult(message, members.Select(ExtractMemberName).ToList()));

   ///<summary>Creates an <see cref="ValidationResult"/> by extracting the invalid member(s) name from the supplied expression(s)</summary>///<summary>Enumerates the lines in a stream reader.</summary>
   public static ValidationResult CreateValidationResult(this IValidatableObject me, string message, params Expression<Func<object>>[] members) =>
      Argument.NotNull3(me, message, members)
              .__(me.CreateValidationResult(message, (IEnumerable<Expression<Func<object>>>)members));
}
