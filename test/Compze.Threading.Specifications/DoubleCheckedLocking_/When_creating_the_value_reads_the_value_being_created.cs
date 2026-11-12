using Compze.Must;
using Compze.Tests.Infrastructure;
using Compze.Threading.Specifications.IAwaitableCriticalSection_.Infrastructure;
using Compze.Threading.Specifications.ICriticalSection_.Infrastructure;
using Compze.Threading.Specifications.TestInfrastructure;
using Xunit;
using static Compze.Must.MustActions;

namespace Compze.Threading.Specifications.DoubleCheckedLocking_;

///<summary>Every critical section lets the thread holding its lock enter it again, so a value creation that reads the value it is<br/>
/// creating runs again and again. Were nothing to stop it, the stack would overflow and end the test process.</summary>
[Collection(nameof(NonParallelCollection))]
public class When_creating_the_value_reads_the_value_being_created : UniversalTestBase
{
   string? _field;
   IReadOnlyDictionary<string, string> _cache = new Dictionary<string, string>();

   public class On_ICriticalSection : When_creating_the_value_reads_the_value_being_created
   {
      readonly ICriticalSectionMatrixAttribute.Factory<On_ICriticalSection> _factory = new();

      protected override void DisposeInternal() => _factory.Dispose();

      [ICriticalSectionMatrix] public void the_single_field_overload_throws_an_InsufficientExecutionStackException_instead_of_overflowing_the_stack()
      {
         var criticalSection = _factory.Create();

         Invoking(ReadTheField).Must().Throw<InsufficientExecutionStackException>();
         return;

         string ReadTheField() => criticalSection.DoubleCheckedLocking(ref _field, ReadTheField);
      }

      [ICriticalSectionMatrix] public void the_overload_with_tryRead_throws_an_InsufficientExecutionStackException_instead_of_overflowing_the_stack()
      {
         var criticalSection = _factory.Create();

         Invoking(ReadTheCachedEntry).Must().Throw<InsufficientExecutionStackException>();
         return;

         string ReadTheCachedEntry() => criticalSection.DoubleCheckedLocking(tryRead: it => it.GetValueOrDefault("key"),
                                                                             field: ref _cache,
                                                                             createUpdatedFieldValue: it => new Dictionary<string, string>(it) { ["key"] = ReadTheCachedEntry() });
      }
   }

   public class On_IAwaitableCriticalSection : When_creating_the_value_reads_the_value_being_created
   {
      readonly IAwaitableCriticalSectionMatrixAttribute.Factory<On_IAwaitableCriticalSection> _factory = new();

      protected override void DisposeInternal() => _factory.Dispose();

      [IAwaitableCriticalSectionMatrix] public void the_single_field_overload_throws_an_InsufficientExecutionStackException_instead_of_overflowing_the_stack()
      {
         var criticalSection = _factory.Create();

         Invoking(ReadTheField).Must().Throw<InsufficientExecutionStackException>();
         return;

         string ReadTheField() => criticalSection.DoubleCheckedLocking(ref _field, ReadTheField);
      }

      [IAwaitableCriticalSectionMatrix] public void the_overload_with_tryRead_throws_an_InsufficientExecutionStackException_instead_of_overflowing_the_stack()
      {
         var criticalSection = _factory.Create();

         Invoking(ReadTheCachedEntry).Must().Throw<InsufficientExecutionStackException>();
         return;

         string ReadTheCachedEntry() => criticalSection.DoubleCheckedLocking(tryRead: it => it.GetValueOrDefault("key"),
                                                                             field: ref _cache,
                                                                             createUpdatedFieldValue: it => new Dictionary<string, string>(it) { ["key"] = ReadTheCachedEntry() });
      }
   }
}
