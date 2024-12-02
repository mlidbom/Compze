using System;
using System.Collections.Generic;
using Compze.DependencyInjection;
using Compze.SystemCE.ThreadingCE;
using Compze.SystemCE.ThreadingCE.ResourceAccess;

// ReSharper disable StaticMemberInGenericType

namespace Compze.SystemCE.ReflectionCE;

class TypeIndex<TInheritor> where TInheritor : TypeIndex<TInheritor>
{
   static readonly MonitorCE Monitor = MonitorCE.WithDefaultTimeout();
   internal static int ServiceCount { get; private set; }
   static IReadOnlyDictionary<Type, int> _map = new Dictionary<Type, int>();

   static Type[] _backMap = [];

   internal static int For(Type type)
   {
      if(_map.TryGetValue(type, out var value))
         return value;

      using(Monitor.TakeUpdateLock())
      {
         if(_map.TryGetValue(type, out var value2))
            return value2;

         ThreadSafe.AddToCopyAndReplace(ref _backMap, type);
         ThreadSafe.AddToCopyAndReplace(ref _map, type, ServiceCount++);
         return ServiceCount - 1;
      }
   }

   internal static class ForService<TType>
   {
      internal static readonly int Index = ServiceTypeIndex.For(typeof(TType));
   }

   public static Type GetServiceForIndex(int serviceTypeIndex) => _backMap[serviceTypeIndex];
}