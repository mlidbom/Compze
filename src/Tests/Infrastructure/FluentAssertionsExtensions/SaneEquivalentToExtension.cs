using System;
using System.Collections.Generic;
using Compze.Utilities.Functional;
using FluentAssertions;
using FluentAssertions.Collections;
using FluentAssertions.Equivalency;
using FluentAssertions.Primitives;

namespace Compze.Tests.Infrastructure.FluentAssertionsExtensions;

public static class BeEquivalentToExtensions
{
   public static AndConstraint<ObjectAssertions> BeStrictlyEquivalentTo<T>(this ObjectAssertions should,
                                                                           T expected,
                                                                           Func<EquivalencyOptions<T>, EquivalencyOptions<T>>? config = null,
                                                                           string because = "",
                                                                           params object[] becauseArgs) =>
      should.BeEquivalentTo(expected, AddStrictOptionsToCallerConfig(config), because, becauseArgs);

   public static AndConstraint<GenericCollectionAssertions<T>> BeStrictlyEquivalentTo<T>(this GenericCollectionAssertions<T> should,
                                                                                         IEnumerable<T> expected,
                                                                                         Func<EquivalencyOptions<T>, EquivalencyOptions<T>>? config = null,
                                                                                         string because = "",
                                                                                         params object[] becauseArgs) =>
      should.BeEquivalentTo(expected, AddStrictOptionsToCallerConfig(config), because, becauseArgs);

   static Func<EquivalencyOptions<T>, EquivalencyOptions<T>> AddStrictOptionsToCallerConfig<T>(Func<EquivalencyOptions<T>, EquivalencyOptions<T>>? config) =>
      options => options.PreferringRuntimeMemberTypes()
                        .WithStrictTyping()
                        .WithStrictOrdering()
                        ._(config ??= it => it);
}
