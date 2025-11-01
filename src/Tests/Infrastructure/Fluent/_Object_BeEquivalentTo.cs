using System.Runtime.CompilerServices;
using Compze.Tests.Infrastructure.Fluent.Serialization;
using Compze.Utilities.SystemCE;
using DiffPlex;
using DiffPlex.Renderer;
using Newtonsoft.Json;

namespace Compze.Tests.Infrastructure.Fluent;

public static class ObjectBeEquivalentTo
{
   public static IMust<TValue> BeEquivalentTo<TValue>(this IMust<TValue> must,
                                                      TValue expected,
                                                      [CallerArgumentExpression(nameof(expected))]
                                                      string expectedExpression = null!)
      => BeEquivalentToCore(must, expected, expectedExpression, TestingJsonSettings.AllMembers);

   public static IMust<TValue> BeEquivalentToInternal<TValue>(this IMust<TValue> must,
                                                              TValue expected,
                                                              [CallerArgumentExpression(nameof(expected))]
                                                              string expectedExpression = null!)
      => BeEquivalentToCore(must, expected, expectedExpression, TestingJsonSettings.InternalAndPublicMembers);

   public static IMust<TValue> BeEquivalentToPublic<TValue>(this IMust<TValue> must,
                                                            TValue expected,
                                                            [CallerArgumentExpression(nameof(expected))]
                                                            string expectedExpression = null!)
      => BeEquivalentToCore(must, expected, expectedExpression, TestingJsonSettings.PublicMembers);

   static IMust<TValue> BeEquivalentToCore<TValue>(IMust<TValue> must,
                                                   TValue expected,
                                                   string expectedExpression,
                                                   JsonSerializerSettings settings)
   {
      var actualJson = JsonConvert.SerializeObject(must.Actual, settings);
      var expectedJson = JsonConvert.SerializeObject(expected, settings);

      return must.Satisfy(it => actualJson == expectedJson,
                          () =>
                             $"""

                              expected the expression: 
                              {must.Separator}
                              {must.Expression.Indent()}
                              {must.Separator}
                              to BeEquivalentTo:
                              {must.Separator}
                              {expectedExpression.Indent()}
                              {must.Separator}

                              Actual.ToString():
                              {must.Separator}
                              {(must.Actual?.ToString() ?? "null").Indent()}
                              {must.Separator}

                              Expected.ToString():
                              {must.Separator}
                              {(expected?.ToString() ?? "null").Indent()}
                              {must.Separator}

                              JSON Diff:
                              {must.Separator}
                              {UnidiffRenderer.GenerateUnidiff(oldText: expectedJson, newText: actualJson, oldFileName: "expected", newFileName: "actual")}
                              {must.Separator}
                              """);
   }
}
