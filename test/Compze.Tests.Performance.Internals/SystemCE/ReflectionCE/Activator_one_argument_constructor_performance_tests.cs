using System.Reflection;
using Compze.Tessaging.Hosting.Testing;
using Compze.Tessaging.Hosting.Testing.Performance;
using Compze.Tests.Infrastructure;
using Compze.Internals.SystemCE;
using Compze.Internals.SystemCE.ReflectionCE;
using Compze.Must;
using Compze.xUnitBDD;
using JetBrains.Annotations;

#pragma warning disable IDE1006 //Reviewed OK: Test Naming Styles
#pragma warning disable CA1806  // Do not ignore method results

namespace Compze.Tests.Performance.Internals.SystemCE.ReflectionCE;

public class Activator_one_argument_constructor_performance_tests : UniversalTestBase
{
   const string Argument = "AnArgument";

#pragma warning disable CS9113
   [UsedImplicitly] class Simple(string arg1)
   {
#pragma warning disable CA1823  //Reviewed OK: unused parameter is intentional
#pragma warning disable CA1801  // Review unused parameters
#pragma warning restore CA1801  // Review unused parameters
#pragma warning restore IDE0060 // Remove unused parameter
   }

   [XF] public void Can_create_instance() => Constructor.For<Simple>.WithArguments<string>.Instance(Argument).Must().NotBeNull();

   [XF] public void _005_Constructs_1_00_000_instances_within_150_percent_of_normal_constructor_call()
   {
      var constructions = 1_00_000.EnvDivide(instrumented:4.7);

      //warmup
      StopwatchCE.TimeExecution(DefaultConstructor, constructions);
      StopwatchCE.TimeExecution(DynamicModuleConstruct, constructions);


      var defaultConstructor = StopwatchCE.TimeExecution(DefaultConstructor, constructions).Total;
      var maxTime = defaultConstructor.MultiplyBy(2.50).EnvMultiply(unoptimized:1.2);
      TimeAsserter.Execute(DynamicModuleConstruct, constructions, maxTotal: maxTime);
   }

   [XF] public void _005_Constructs_1_00_000_instances_15_times_faster_than_via_activator_create_instance()
   {
      var constructions = 1_00_000.EnvDivide(instrumented:20);

      //warmup
      StopwatchCE.TimeExecution(ActivatorCreateInstance, constructions);
      StopwatchCE.TimeExecution(DynamicModuleConstruct, constructions);


      var defaultConstructor = StopwatchCE.TimeExecution(ActivatorCreateInstance, constructions).Total;
      var maxTime = defaultConstructor.DivideBy(15);
      TimeAsserter.Execute(DynamicModuleConstruct, constructions, maxTotal: maxTime.EnvMultiply(instrumented: 25, unoptimized:1.3));
   }

   static void DynamicModuleConstruct() => Constructor.For<Simple>.WithArguments<string>.Instance(Argument);

   // ReSharper disable once ObjectCreationAsStatement
   static void DefaultConstructor() => FakeActivator.CreateWithDefaultConstructor();

   static void ActivatorCreateInstance() => FakeActivator.CreateUsingActivatorCreateInstance();


   static class FakeActivator
   {
      // ReSharper disable once ObjectCreationAsStatement
      internal static void CreateWithDefaultConstructor() => new Simple(Argument);

      internal static void CreateUsingActivatorCreateInstance() => Activator.CreateInstance(
         type: typeof(Simple),
         bindingAttr: BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public,
         binder: null,
         args: [Argument],
         culture: null);
   }
}