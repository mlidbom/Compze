using System.Diagnostics;
using Compze.Internals.Logging;
using Compze.Internals.SystemCE;
using Compze.Internals.SystemCE.ReflectionCE;
using Compze.Threading;
using Compze.DbPool.SystemCE.Private;

namespace Compze.DbPool.SystemCE;

public static class StrictlyManagedResources
{
   // ReSharper disable once FieldCanBeMadeReadOnly.Global
   internal static bool CollectStackTracesByDefault = false;
   internal static bool LoggingTemporarilySuppressed = false;

   public static void SuppressLoggingWhileExecuting(Action action)
   {
      using(ScopedChange.Enter(() => LoggingTemporarilySuppressed = true, () => LoggingTemporarilySuppressed = false))
      {
         action();
      }
   }
}

///<summary>
/// A strictly managed resource logs an Exception of type <see cref="StrictlyManagedResourceWasFinalizedException"/> if the finalizer is ever called.
/// <para>Implementing this interface MUST be done by inheriting from <see cref="StrictlyManagedResourceBase{TInheritor}"/> or having a readonly field of type <see cref="StrictlyManagedResource{TManagedResource}"/>.
///  This guarantees the expected behavior including the ability to enable and disable the collection of stacktraces for the allocations.</para>
/// </summary>
public interface IStrictlyManagedResource : IDisposable;

///<summary>
/// Helper class for implementing <see cref="IStrictlyManagedResource"/>
/// </summary>
/// <example>
/// Typical usage is to implement <see cref="IStrictlyManagedResource"/> by having a <see cref="StrictlyManagedResource{TManagedResource}"/> instance field:
/// <code>
///class AnotherStrictlyManagedResource : SomeBaseClass, IStrictlyManagedResource
///{
///    readonly StrictlyManagedResource _leakDetector =  new StrictlyManagedResource();
///    public void Dispose()
///    {
///        GC.SuppressFinalize(this);
///        _leakDetector.Dispose();
///    }
///}
/// </code>
///</example>
public sealed class StrictlyManagedResource<TManagedResource> : IStrictlyManagedResource where TManagedResource : class, IStrictlyManagedResource
{
   // ReSharper disable once StaticMemberInGenericType
   static readonly IMonitor StaticMonitor = IMonitor.New();
   readonly bool _collectStackTraces;
   // ReSharper disable once StaticMemberInGenericType
   static bool _collectStackTracesByDefault = StrictlyManagedResources.CollectStackTracesByDefault;

   public StrictlyManagedResource(bool forceStackTraceCollection = false, bool needsFileInfo = false, TManagedResource? instance = null)
   {
      _instance = instance;
      _collectStackTraces = forceStackTraceCollection || StrictlyManagedResources.CollectStackTracesByDefault;
      if(_collectStackTraces)
      {
         ReservationCallStack = new StackTrace(fNeedFileInfo: needsFileInfo).ToString();
      }
   }

   string? ReservationCallStack { get; }

   bool _disposed;
   readonly TManagedResource? _instance;

   public void Dispose()
   {
      GC.SuppressFinalize(this);
      _disposed = true;
   }

   Type ManagedType => _instance?.GetType() ?? GetType();

   ~StrictlyManagedResource()
   {
      if(!_disposed)
      {
         try
         {
#pragma warning disable CA1065 // Do not raise exceptions in unexpected locations
            throw new StrictlyManagedResourceWasFinalizedException(ManagedType, ReservationCallStack);
#pragma warning restore CA1065 // Do not raise exceptions in unexpected locations
         }
         catch(StrictlyManagedResourceWasFinalizedException exception)
         {
            try
            {
               UncatchableExceptionsGatherer.Register(exception);
               if(!StrictlyManagedResources.LoggingTemporarilySuppressed)
               {
                  this.Log().Error(exception, $"{ManagedType.GetFullNameCompilable()} was finalized without being disposed.");

                  if(!_collectStackTraces)
                  {
                     using(StaticMonitor.TakeLock())
                     {
                        if(!_collectStackTracesByDefault)
                        {
                           this.Log().Warning($"Enabling collection of stacktraces for {ManagedType.GetFullNameCompilable()} since it is not always disposed.");
                           _collectStackTracesByDefault = true;
                        }
                     }
                  }
               }
            }

            // ReSharper disable once EmptyGeneralCatchClause
#pragma warning disable CA1031 //Don't even think about letting exceptions escape on the finalizer thread again.The day I spent trying to understand why test processes simply died without explanation was no fun. Once was plenty.
            catch {}
#pragma warning restore CA1031
         }
      }
   }
}

///<summary>
/// Inheriting from this class is the simplest way to implement <see cref="IStrictlyManagedResource"/>
///</summary>
///<example>
///<code>
///class SomeStrictlyManagedResource : StrictlyManagedResourceBase
///{
///    ResourceThatMustBeDisposed _resourceThatMustBeDisposed = new ResourceThatMustBeDisposed();
///    bool _disposed;
///    protected override void InternalDispose()
///    {
///        if (!_disposed)
///        {
///           _disposed = true;
///           _resourceThatMustBeDisposed.Dispose();
///        }
///    }
///}
///</code>
///</example>
public abstract class StrictlyManagedResourceBase<TInheritor> : IStrictlyManagedResource
   where TInheritor : StrictlyManagedResourceBase<TInheritor>
{
   protected bool Disposed{ get; private set; }
   readonly StrictlyManagedResource<StrictlyManagedResourceBase<TInheritor>> _strictlyManagedResource;

   protected StrictlyManagedResourceBase(bool forceStackTraceAllocation = false, bool needsFileInfo = false) =>
      _strictlyManagedResource = new StrictlyManagedResource<StrictlyManagedResourceBase<TInheritor>>(forceStackTraceAllocation, needsFileInfo, instance:this);

   public virtual void Dispose()
   {
      Disposed = true;
      _strictlyManagedResource.Dispose();
   }
}
