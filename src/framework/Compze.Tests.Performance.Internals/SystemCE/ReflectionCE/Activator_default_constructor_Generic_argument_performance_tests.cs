﻿using System;
using Compze.SystemCE;
using Compze.SystemCE.ReflectionCE;
using Compze.Testing;
using Compze.Testing.Performance;
using FluentAssertions;
using JetBrains.Annotations;
using NUnit.Framework;

#pragma warning disable CA1806 // Do not ignore method results

namespace Compze.Tests.Performance.Internals.SystemCE.ReflectionCE;

[TestFixture]public class Activator_default_constructor_Generic_argument_performance_tests : UniversalTestBase
{
   [UsedImplicitly] class Simple;

   [Test] public void Can_construct_instance() => Constructor.For<Simple>.DefaultConstructor.Instance().Should().NotBe(null);

   [Test] public void Constructs_1_000_000_instances_within_50_percent_of_default_constructor_time()
   {
      var constructions = 1_000_000.EnvDivide(instrumented:4.7);

      //warmup
      StopwatchCE.TimeExecution(DefaultConstructor, constructions);
      StopwatchCE.TimeExecution(DynamicModuleConstruct, constructions);


      var defaultConstructor = StopwatchCE.TimeExecution(DefaultConstructor, constructions).Total;
      var maxTime = defaultConstructor.MultiplyBy(1.50).EnvMultiply(unoptimized:1.3);
      TimeAsserter.Execute(DynamicModuleConstruct, constructions, maxTotal: maxTime);
   }

   [Test] public void Constructs_1_000_000_instances_50_percent_faster_than_via_new_constraint_constructor_time()
   {
      var constructions = 1_000_000.EnvDivide(instrumented:10);

      //warmup
      StopwatchCE.TimeExecution(NewConstraint, constructions);
      StopwatchCE.TimeExecution(DynamicModuleConstruct, constructions);


      var defaultConstructor = StopwatchCE.TimeExecution(NewConstraint, constructions).Total;
      var maxTime = defaultConstructor.DivideBy(1.5);
      TimeAsserter.Execute(DynamicModuleConstruct, constructions, maxTotal: maxTime.EnvMultiply(instrumented: 4, unoptimized:1.3));
   }

   [Test] public void Constructs_1_000_000_instances_50_percent_times_faster_than_via_activator_CreateInstance()
   {
      var constructions = 1_000_000.EnvDivide(instrumented:10);

      //warmup
      StopwatchCE.TimeExecution(ActivatorCreateInstance, constructions);
      StopwatchCE.TimeExecution(DynamicModuleConstruct, constructions);


      var activatorCreateInstanceTime = StopwatchCE.TimeExecution(ActivatorCreateInstance, constructions).Total;
      var maxTime = activatorCreateInstanceTime.DivideBy(1.5).EnvMultiply(instrumented: 4.2, unoptimized:1.3);
      TimeAsserter.Execute(DynamicModuleConstruct, constructions, maxTotal: maxTime);
   }

   static void DynamicModuleConstruct() => Constructor.For<Simple>.DefaultConstructor.Instance();

   // ReSharper disable once ObjectCreationAsStatement
   static void DefaultConstructor() => FakeActivator.CreateWithDefaultConstructor();

   static void ActivatorCreateInstance() => FakeActivator.CreateUsingActivatorCreateInstance();

   static void NewConstraint() => ActivateViaNewConstraint<Simple>.Create();

   static class FakeActivator
   {
      // ReSharper disable once ObjectCreationAsStatement
      internal static void CreateWithDefaultConstructor() => new Simple();
      internal static void CreateUsingActivatorCreateInstance() => Activator.CreateInstance(typeof(Simple), nonPublic: true);
   }

   static class ActivateViaNewConstraint<TInstance> where TInstance : new()
   {
      // ReSharper disable once ObjectCreationAsStatement
      internal static void Create() => new TInstance();
   }
}