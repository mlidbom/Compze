using System.Runtime.CompilerServices;
using Compze.Tests.Infrastructure.Fluent.Serialization;
using Compze.Utilities.SystemCE;
using DiffPlex.Renderer;
using Newtonsoft.Json;

namespace Compze.Tests.Infrastructure.Fluent;

public static class ObjectBeEquivalentTo
{
   public static Must<TValue> BeEquivalentTo<TValue>(this Must<TValue> must,
                                                      TValue expected,
                                                      [CallerArgumentExpression(nameof(expected))]
                                                      string expectedExpression = null!)
      => BeEquivalentToCore(must, expected, expectedExpression, TestingJsonSettings.AllMembers);

   public static Must<TValue> BeEquivalentToInternal<TValue>(this Must<TValue> must,
                                                              TValue expected,
                                                              [CallerArgumentExpression(nameof(expected))]
                                                              string expectedExpression = null!)
      => BeEquivalentToCore(must, expected, expectedExpression, TestingJsonSettings.InternalAndPublicMembers);

   public static Must<TValue> BeEquivalentToPublic<TValue>(this Must<TValue> must,
                                                            TValue expected,
                                                            [CallerArgumentExpression(nameof(expected))]
                                                            string expectedExpression = null!)
      => BeEquivalentToCore(must, expected, expectedExpression, TestingJsonSettings.PublicMembers);

   static Must<TValue> BeEquivalentToCore<TValue>(Must<TValue> must,
                                                   TValue expected,
                                                   string expectedExpression,
                                                   JsonSerializerSettings settings)
   {
      var actualJson = JsonConvert.SerializeObject(must.Actual, settings);
      var expectedJson = JsonConvert.SerializeObject(expected, settings);

      return must.Satisfy(it => actualJson == expectedJson,
                          () =>
                             $"""
                              {must.Separator}
                              expected the object returned by the expression: 
                              {must.Separator}
                              {must.Expression}
                              {must.Separator}
                              to be equivalent to the object returned by the expression:
                              {must.Separator}
                              {expectedExpression}
                              {must.Separator}
                              But it resulted in the Diff:
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
                              """);
   }
}
