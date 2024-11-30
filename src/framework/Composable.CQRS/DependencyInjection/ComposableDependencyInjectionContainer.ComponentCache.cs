using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Composable.Contracts;
using Composable.SystemCE;
using Composable.SystemCE.LinqCE;
using Composable.SystemCE.ThreadingCE.TasksCE;

namespace Composable.DependencyInjection;

partial class ComposableDependencyInjectionContainer
{
   class RootCache : IDisposable, IAsyncDisposable
   {
      readonly int[] _serviceTypeIndexToComponentIndex;
      readonly (ComponentRegistration[] Registrations, object Instance)[] _cache;

      internal RootCache(IReadOnlyList<ComponentRegistration> registrations)
      {
         var serviceCount = ServiceTypeIndex.ServiceCount;

         _serviceTypeIndexToComponentIndex = new int[serviceCount + 1];
         for(var index = 0; index < _serviceTypeIndexToComponentIndex.Length; index++)
         {
            _serviceTypeIndexToComponentIndex[index] = serviceCount;
         }

         _cache = new (ComponentRegistration[] Registrations, object Instance)[serviceCount + 1];

         registrations.SelectMany(registration => registration.ServiceTypes.Select(serviceType => new { registration, serviceType, typeIndex = ServiceTypeIndex.For(serviceType) }))
                      .GroupBy(registrationPerTypeIndex => registrationPerTypeIndex.typeIndex)
                      .ForEach(registrationsOnTypeIndex =>
                       {
                          //refactor: We don't support more than one registration. The whole DI container assumes a single registration. Why does this code not?
                          _cache[registrationsOnTypeIndex.Key].Registrations = registrationsOnTypeIndex.Select(regs => regs.registration).ToArray();
                       });

         foreach(var registration in registrations)
         {
            foreach(var serviceTypeIndex in registration.ServiceTypeIndexes)
            {
               if(_serviceTypeIndexToComponentIndex[serviceTypeIndex] != serviceCount)
               {
                  throw new Exception($"Already has a component registered for service: {ServiceTypeIndex.GetServiceForIndex(serviceTypeIndex)}");
               }

               _serviceTypeIndexToComponentIndex[serviceTypeIndex] = registration.ComponentIndex;
            }
         }
      }

      internal ScopeCache CreateScopeCache() => new(_serviceTypeIndexToComponentIndex);

      public void Set(object instance, ComponentRegistration registration) => _cache[registration.ComponentIndex].Instance = instance;

      internal (ComponentRegistration[] Registrations, object Instance) TryGet<TService>() => _cache[_serviceTypeIndexToComponentIndex[ServiceTypeIndex.ForService<TService>.Index]];

      public void Dispose()
      {
         var asyncDisposables = AsyncDisposableInstances().ToHashSet(ReferenceEqualityComparer.Instance);
         var disposables = DisposableInstances().ToHashSet(ReferenceEqualityComparer.Instance);

         asyncDisposables.ExceptWith(disposables);
         if(asyncDisposables.Any())
         {
            var invalidComponent = asyncDisposables.First().NotNull();
            throw new InvalidOperationException($"{invalidComponent.GetType().FullName} only supports DisposeAsync");
         }
         DisposeComponents(disposables.Cast<IDisposable>());
      }

      IEnumerable<IDisposable> DisposableInstances() => InstancesToDispose().OfType<IDisposable>();
      IEnumerable<IAsyncDisposable> AsyncDisposableInstances() => InstancesToDispose().OfType<IAsyncDisposable>();

      IEnumerable<object> InstancesToDispose()
      {
         return _cache.Where(it => it is { Registrations: not null, Instance: not null })
                      .Where(it => it.Registrations[0].InstantiationSpec.SingletonInstance == null) //We don't dispose instance registrations.
                      .Select(it => it.Instance);
      }

      public async ValueTask DisposeAsync()
      {
         var asyncDisposables = DisposableInstances().OfType<IAsyncDisposable>().ToHashSet<IAsyncDisposable>(ReferenceEqualityComparer.Instance);
         var disposables = DisposableInstances().ToHashSet(ReferenceEqualityComparer.Instance);

         disposables.ExceptWith(asyncDisposables);
         DisposeComponents(disposables.Cast<IDisposable>());

         await Task.WhenAll(asyncDisposables.Select(async it => await it.DisposeAsync().CaF())).CaF();
      }
   }

   internal class ScopeCache : IDisposable
   {
      bool _isDisposed;
      readonly int[] _serviceTypeIndexToComponentIndex;
      readonly object?[] _instances;
      readonly LinkedList<IDisposable> _disposables = [];

      public void Set(object instance, ComponentRegistration registration)
      {
         _instances[registration.ComponentIndex] = instance;
         if(instance is IDisposable disposable)
         {
            _disposables.AddLast(disposable);
         }
      }

      internal bool TryGet<TService>(out TService? service)
      {
         service = (TService?)_instances[_serviceTypeIndexToComponentIndex[ServiceTypeIndex.ForService<TService>.Index]];
         return !Equals(service, default);
      }

      internal ScopeCache(int[] serviceServiceTypeToComponentIndex)
      {
         _serviceTypeIndexToComponentIndex = serviceServiceTypeToComponentIndex;
         _instances = new object[serviceServiceTypeToComponentIndex.Length];
      }

      public void Dispose()
      {
         if(!_isDisposed)
         {
            var instances = _instances.Where(it => it != null).ToList();
            var asyncDisposables = instances.OfType<IAsyncDisposable>().ToHashSet(ReferenceEqualityComparer.Instance);
            var disposables = instances.OfType<IDisposable>().ToHashSet(ReferenceEqualityComparer.Instance);

            asyncDisposables.ExceptWith(disposables);
            if(asyncDisposables.Any())
            {
               var invalidComponent = asyncDisposables.First().NotNull();
               throw new InvalidOperationException($"{invalidComponent.GetType().FullName} only supports DisposeAsync");
            }

            var enumerable = disposables.Cast<IDisposable>();
            DisposeComponents(enumerable);
         }
      }
   }

   static void DisposeComponents(IEnumerable<IDisposable> disposables)
   {
      var exceptions = disposables
                      .Select(disposable => ExceptionCE.TryCatch(disposable.Dispose))
                      .Where(exception => exception != null)
                      .Cast<Exception>()
                      .ToList();

      if(exceptions.Any())
      {
         throw new AggregateException("Exceptions where thrown in Dispose methods of components", exceptions);
      }
   }
}
