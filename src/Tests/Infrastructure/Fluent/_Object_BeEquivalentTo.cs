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
using Compze.Utilities.SystemCE;

namespace Compze.Tests.Infrastructure.Fluent;

public class EquivalencyConfig<TValue>
{
   internal HashSet<MemberInfo> ExcludedMembers { get; } = new();

   public EquivalencyConfig<TValue> Excluding<TMember>(Expression<Func<TValue, TMember>> memberExpression) =>
      ExcludedMembers.Add(memberExpression.ExtractFinalMemberInfo())
                     .then(this);
}

public static class ObjectDeepEquality
{
   public static Must<TValue> DeepEqual<TValue>(this Must<TValue> must,
                                                     TValue expected,
                                                     [CallerArgumentExpression(nameof(expected))]
                                                     string expectedExpression = null!)
      => DeepEqualCore(must, expected, expectedExpression, TestingJsonSettings.AllMembers);

   public static Must<TValue> DeepEqualPrivate<TValue>(this Must<TValue> must,
                                                     TValue expected,
                                                     Func<EquivalencyConfig<TValue>, EquivalencyConfig<TValue>> config,
                                                     [CallerArgumentExpression(nameof(expected))]
                                                     string expectedExpression = null!)
   {
      var equivalencyConfig = config(new EquivalencyConfig<TValue>());
      var serializerSettings = TestingJsonSettings.CreateSettingsWithExclusions(TestingJsonSettings.AllMembers, equivalencyConfig.ExcludedMembers);
      return DeepEqualCore(must, expected, expectedExpression, serializerSettings);
   }

   public static Must<TValue> DeepEqualInternal<TValue>(this Must<TValue> must,
                                                             TValue expected,
                                                             [CallerArgumentExpression(nameof(expected))]
                                                             string expectedExpression = null!)
      => DeepEqualCore(must, expected, expectedExpression, TestingJsonSettings.InternalAndPublicMembers);

   public static Must<TValue> DeepEqualPublic<TValue>(this Must<TValue> must,
                                                           TValue expected,
                                                           [CallerArgumentExpression(nameof(expected))]
                                                           string expectedExpression = null!)
      => DeepEqualCore(must, expected, expectedExpression, TestingJsonSettings.PublicMembers);

   static Must<TValue> DeepEqualCore<TValue>(Must<TValue> must,
                                                  TValue expected,
                                                  string expectedExpression,
                                                  JsonSerializerSettings settings)
   {
      var actualJson = JsonConvert.SerializeObject(must.Actual, settings);
      var expectedJson = JsonConvert.SerializeObject(expected, settings);

      return must.Satisfy(it => actualJson == expectedJson,
                          messageOverride: _ =>
                             $"""
                              {must.Separator}
                              expected:
                              {must.Separator}
                              {must.Expression.Indent()}
                              {must.Separator}
                              to be deeply equal to:
                              {must.Separator}
                              {must.NormalizeExpressionIndentation(expectedExpression).Indent()}
                              {must.Separator}
                              But comparison of the objects serialized as JSON resulted in the Diff:
                              {must.Separator}
                              {DiffGenerator.CreateDiff(expected: expectedJson, actual:actualJson)}
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
