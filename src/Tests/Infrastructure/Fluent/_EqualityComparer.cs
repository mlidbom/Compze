using Compze.Tests.Infrastructure.Fluent.Serialization;
using DiffPlex.Renderer;
using Newtonsoft.Json;
using System.Runtime.CompilerServices;

namespace Compze.Tests.Infrastructure.Fluent;

public static class ObjectEqualityAssertions
{
   public static Must<TValue>? Be<TValue>(this Must<TValue> must, TValue expected, [CallerArgumentExpression(nameof(expected))] string expectedExpression = null!)
      => must.Satisfy(it => Equals(it, expected),
                      () =>
                      {
                         var actualJson = JsonConvert.SerializeObject(must.Actual, TestingJsonSettings.AllMembers);
                         var expectedJson = JsonConvert.SerializeObject(expected, TestingJsonSettings.AllMembers);
                         return $"""
                                 {must.Separator}
                                 expected the object returned by the expression: 
                                 {must.Separator}
                                 {must.Expression}
                                 {must.Separator}
                                 to be the equal to the the object returned by the expression:
                                 {must.Separator}
                                 {must.NormalizeExpressionIndentation(expectedExpression)}
                                 {must.Separator}
                                 but it was not and a diff of the instances is:
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
                                 """;
                      });

   public static Must<TValue>? Be_transitively_equal_to_according_to_every_supported_comparison_method<TValue>(this Must<TValue> must, TValue expected, [CallerArgumentExpression(nameof(expected))] string expectedExpression = null!)
   {

         //using Satisfy, passing expectedExpression along, Implement the equivalent of making all the below assertions, and their inversion (with _first and _second reversed)
         //Also do the same for IComparable<T> and the < > <= >= operators

        //[XF] public void IEquatable_equals_returns_true() => _first.Equals(_second).Must().BeTrue();
        //[XF] public void Object_equals_returns_true() => Equals(_first, _second).Must().BeTrue();
        //[XF] public void equals_operator_returns_true() => (_first == _second).Must().BeTrue();
        //[XF] public void not_equals_operator_returns_false() => (_first != _second).Must().BeFalse();



        return must;
   }
}
