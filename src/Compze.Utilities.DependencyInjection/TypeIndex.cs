using System;
using System.Collections.Generic;
using Compze.Threading;
using Compze.Threading.ResourceAccess;

// ReSharper disable StaticMemberInGenericType

namespace Compze.Utilities.DependencyInjection;

public abstract class TypeIndex<TInheritor> where TInheritor : TypeIndex<TInheritor>
{
   static readonly IMonitor MonitorCE = IMonitor.WithDefaultTimeout();
   private static int ServiceCount { get; set; }
   static IReadOnlyDictionary<Type, int> _map = new Dictionary<Type, int>();

   static Type[] _backMap = [];

   private static int For(Type type)
   {
      if(_map.TryGetValue(type, out var value))
         return value;

      using(MonitorCE.TakeLock())
      {
         if(_map.TryGetValue(type, out var value2))
            return value2;

         OnlyWithinLocksThreadingHelpers.AddToCopyAndReplace(ref _backMap, type);
         OnlyWithinLocksThreadingHelpers.AddToCopyAndReplace(ref _map, type, ServiceCount++);
         return ServiceCount - 1;
      }
   }

   public static class ForService<TType>
   {
      public static readonly int Index = ServiceTypeIndex.For(typeof(TType));
   }
}