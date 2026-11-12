using Compze.Must;
using Compze.Tests.Infrastructure;
using Compze.Threading.Specifications.IAwaitableCriticalSection_.Infrastructure;
using Compze.Threading.Specifications.ICriticalSection_.Infrastructure;
using Compze.Threading.Specifications.TestInfrastructure;
using Xunit;
using static Compze.Must.MustActions;

namespace Compze.Threading.Specifications.DoubleCheckedLocking_;

[Collection(nameof(NonParallelCollection))]
public class When_createFieldValue_throws : UniversalTestBase
{
   string? _field;
   int _createFieldValueRuns;

   string CreateFieldValueThatThrowsOnItsFirstRun() => ++_createFieldValueRuns == 1
                                                          ? throw new InvalidOperationException("The first run fails.")
                                                          : "created by the second run";

   public class On_ICriticalSection : When_createFieldValue_throws
   {
      readonly ICriticalSectionMatrixAttribute.Factory<On_ICriticalSection> _factory = new();

      protected override void DisposeInternal() => _factory.Dispose();

      [ICriticalSectionMatrix] public void the_call_throws_its_exception_and_leaves_the_field_unset()
      {
         var criticalSection = _factory.Create();

         Invoking(() => criticalSection.DoubleCheckedLocking(ref _field, CreateFieldValueThatThrowsOnItsFirstRun)).Must().Throw<InvalidOperationException>();

         _field.Must().BeNull();
      }

      [ICriticalSectionMatrix] public void the_next_call_runs_createFieldValue_again_and_returns_what_it_returns()
      {
         var criticalSection = _factory.Create();
         Invoking(() => criticalSection.DoubleCheckedLocking(ref _field, CreateFieldValueThatThrowsOnItsFirstRun)).Must().Throw<InvalidOperationException>();

         criticalSection.DoubleCheckedLocking(ref _field, CreateFieldValueThatThrowsOnItsFirstRun).Must().Be("created by the second run");
      }
   }

   public class On_IAwaitableCriticalSection : When_createFieldValue_throws
   {
      readonly IAwaitableCriticalSectionMatrixAttribute.Factory<On_IAwaitableCriticalSection> _factory = new();

      protected override void DisposeInternal() => _factory.Dispose();

      [IAwaitableCriticalSectionMatrix] public void the_call_throws_its_exception_and_leaves_the_field_unset()
      {
         var criticalSection = _factory.Create();

         Invoking(() => criticalSection.DoubleCheckedLocking(ref _field, CreateFieldValueThatThrowsOnItsFirstRun)).Must().Throw<InvalidOperationException>();

         _field.Must().BeNull();
      }

      [IAwaitableCriticalSectionMatrix] public void the_next_call_runs_createFieldValue_again_and_returns_what_it_returns()
      {
         var criticalSection = _factory.Create();
         Invoking(() => criticalSection.DoubleCheckedLocking(ref _field, CreateFieldValueThatThrowsOnItsFirstRun)).Must().Throw<InvalidOperationException>();

         criticalSection.DoubleCheckedLocking(ref _field, CreateFieldValueThatThrowsOnItsFirstRun).Must().Be("created by the second run");
      }
   }
}
