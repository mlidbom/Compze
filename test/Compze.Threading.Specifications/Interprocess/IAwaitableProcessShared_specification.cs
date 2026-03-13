using Compze.Must;
using Compze.Tests.Infrastructure;
using Compze.Threading.Specifications.TestInfrastructure;
using Xunit;
// ReSharper disable InconsistentNaming

namespace Compze.Threading.Specifications.Interprocess;

[Collection(nameof(NonParallelCollection))]
public class IAwaitableProcessShared_specification : UniversalTestBase
{
   readonly IAwaitableProcessSharedMatrixAttribute.Factory<IAwaitableProcessShared_specification> _factory = new();

   protected override void DisposeInternal() => _factory.Dispose();

   public class Mutex_property : IAwaitableProcessShared_specification
   {
      [IAwaitableProcessSharedMatrix] public void IsGlobal_matches_factory_method_scope()
      {
         var shared = _factory.Create(new SharedTestValue());
         var expectedGlobal = _factory.CurrentImplementationIsGlobal;
         shared.Mutex.IsGlobal.Must().Be(expectedGlobal);
      }

      [IAwaitableProcessSharedMatrix] public void Name_contains_Global_or_Local_prefix_matching_scope()
      {
         var shared = _factory.Create(new SharedTestValue());
         var expectedPrefix = _factory.CurrentImplementationIsGlobal ? @"Global\" : @"Local\";
         shared.Mutex.Name.Must().StartWith(expectedPrefix);
      }

      [IAwaitableProcessSharedMatrix] public void LockTimeout_matches_specified_timeout()
      {
         var shared = _factory.Create(new SharedTestValue(), lockTimeout: LockTimeout.Seconds(7));
         shared.Mutex.LockTimeout.Must().Be(LockTimeout.Seconds(7));
      }

      [IAwaitableProcessSharedMatrix] public void WaitTimeout_matches_specified_timeout()
      {
         var shared = _factory.Create(new SharedTestValue(), waitTimeout: WaitTimeout.Seconds(7));
         shared.Mutex.WaitTimeout.Must().Be(WaitTimeout.Seconds(7));
      }
   }

   public class IDisposable : IAwaitableProcessShared_specification
   {
      [IAwaitableProcessSharedMatrix] public void can_be_disposed_without_error()
      {
         var shared = _factory.Create(new SharedTestValue());
         shared.Dispose();
      }
   }
}
