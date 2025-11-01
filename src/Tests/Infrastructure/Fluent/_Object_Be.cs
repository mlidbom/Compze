using Compze.Tests.Infrastructure.Fluent.Serialization;
using Compze.Utilities.SystemCE;
using DiffPlex.Renderer;
using Newtonsoft.Json;
using System.Runtime.CompilerServices;

namespace Compze.Tests.Infrastructure.Fluent;

public static class ObjectBe
{
   public static IMust<TValue>? Be<TValue>(this IMust<TValue> must, TValue expected, [CallerArgumentExpression(nameof(expected))] string expectedExpression = null!)
      => must.Satisfy(it => Equals(it, expected),
                      () =>
                      {
                         var actualJson = JsonConvert.SerializeObject(must.Actual, TestingJsonSettings.AllMembers);
                         var expectedJson = JsonConvert.SerializeObject(expected, TestingJsonSettings.AllMembers);
                         return $"""
                                 expected the object returned by the expression: 
                                 {must.Separator}
                                 {must.Expression.Indent()}
                                 {must.Separator}
                                 to be the equal to the the object returned by the expression:
                                 {must.Separator}
                                 {expectedExpression}
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
}
