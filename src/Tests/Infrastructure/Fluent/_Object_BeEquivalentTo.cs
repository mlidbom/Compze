using Compze.Tests.Infrastructure.Fluent.Serialization;
using Compze.Utilities.SystemCE.LinqCE;
using DiffPlex.Renderer;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using Compze.Utilities.Functional;

namespace Compze.Tests.Infrastructure.Fluent;

public class EquivalencyConfig<TValue>
{
   internal HashSet<MemberInfo> ExcludedMembers { get; } = new();

   public EquivalencyConfig<TValue> Excluding<TMember>(Expression<Func<TValue, TMember>> memberExpression) =>
      ExcludedMembers.Add(memberExpression.ExtractFinalMemberInfo())
                     .then(this);
}

public static class ObjectBeEquivalentTo
{
   public static Must<TValue> BeEquivalentTo<TValue>(this Must<TValue> must,
                                                     TValue expected,
                                                     [CallerArgumentExpression(nameof(expected))]
                                                     string expectedExpression = null!)
      => BeEquivalentToCore(must, expected, expectedExpression, TestingJsonSettings.AllMembers);

   public static Must<TValue> BeEquivalentTo<TValue>(this Must<TValue> must,
                                                     TValue expected,
                                                     Func<EquivalencyConfig<TValue>, EquivalencyConfig<TValue>> config,
                                                     [CallerArgumentExpression(nameof(expected))]
                                                     string expectedExpression = null!)
   {
      var equivalencyConfig = config(new EquivalencyConfig<TValue>());
      var serializerSettings = TestingJsonSettings.CreateSettingsWithExclusions(TestingJsonSettings.AllMembers, equivalencyConfig.ExcludedMembers);
      return BeEquivalentToCore(must, expected, expectedExpression, serializerSettings);
   }

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
                              {must.NormalizeExpressionIndentation(expectedExpression)}
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
