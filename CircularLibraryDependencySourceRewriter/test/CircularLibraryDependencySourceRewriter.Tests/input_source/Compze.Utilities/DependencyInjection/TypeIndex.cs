using System;
using System.Collections.Generic;
using Compze.Utilities.SystemCE.ThreadingCE;
using Compze.Utilities.SystemCE.ThreadingCE.ResourceAccess;

// ReSharper disable StaticMemberInGenericType

namespace Compze.Utilities.DependencyInjection;

public abstract class TypeIndex<TInheritor> where TInheritor : TypeIndex<TInheritor>
{
   static readonly IMonitorCE MonitorCE = IMonitorCE.WithDefaultTimeout();
   public static int ServiceCount { get; private set; }
   static IReadOnlyDictionary<Type, int> _map = new Dictionary<Type, int>();

   static Type[] _backMap = [];

   public static int For(Type type)
   {
      if(_map.TryGetValue(type, out var value))
         return value;

      using(MonitorCE.TakeUpdateLock())
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