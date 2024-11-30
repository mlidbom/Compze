using System;
using System.Threading.Tasks;
using Composable.DependencyInjection;
using Composable.DependencyInjection.Microsoft;
using Composable.Testing;
using NUnit.Framework;

namespace Composable.Tests.DependencyInjection;

[TestFixture] class ComposableDependencyInjectionContainerSpecification : UniversalTestBase
{
   [TestFixture] public class When_a_component_that_only_implements_IAsyncDisposable_is_registered : UniversalTestBase
   {
      [TestFixture] class As_singleton : UniversalTestBase
      {
         [TestFixture] public class And_the_component_has_been_resolved : UniversalTestBase
         {
            [Test] public void Dispose_throws_InvalidOperationExceptions()
            {
               var container = new ComposableDependencyInjectionContainer(new RunMode(isTesting: true));
               container.Register(Singleton.For<OnlyAsyncDispose>().CreatedBy(_ => new OnlyAsyncDispose()));
               var onlyAsyncDispose = container.ServiceLocator.Resolve<OnlyAsyncDispose>();
               AssertThrows.Exception<InvalidOperationException>(() => container.Dispose());
            }
         }
      }

      [TestFixture] class As_Scoped : UniversalTestBase
      {
         [TestFixture] public class And_the_component_has_been_resolved_within_a_scope : UniversalTestBase
         {
            // [Test] public void Calling_Dispose_on_the_scope_throws_InvalidOperationExceptions()
            // {
            //    var container = new ComposableDependencyInjectionContainer(new RunMode(isTesting: true));
            //    container.Register(Scoped.For<OnlyAsyncDispose>().CreatedBy(_ => new OnlyAsyncDispose()));
            //    var serviceLocator = container.ServiceLocator;
            //    var scope = serviceLocator.BeginScope();
            //    var onlyAsyncDispose = container.ServiceLocator.Resolve<OnlyAsyncDispose>();
            //    AssertThrows.Exception<InvalidOperationException>(() => scope.Dispose());
            // }
            
            [Test] public void MS_Calling_Dispose_on_the_scope_throws_InvalidOperationExceptions()
            {
               var container = new MicrosoftDependencyInjectionContainer(new RunMode(isTesting: true));
               container.Register(Scoped.For<OnlyAsyncDispose>().CreatedBy(_ => new OnlyAsyncDispose()));
               var serviceLocator = container.ServiceLocator;
               var scope = serviceLocator.BeginScope();
               var onlyAsyncDispose = container.ServiceLocator.Resolve<OnlyAsyncDispose>();
               AssertThrows.Exception<InvalidOperationException>(() => scope.Dispose());
            }
         }
      }

      class OnlyAsyncDispose : IAsyncDisposable
      {
         public ValueTask DisposeAsync() => ValueTask.CompletedTask;
      }
   }
}
