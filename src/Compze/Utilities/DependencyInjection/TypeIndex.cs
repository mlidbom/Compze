using System;
using System.Collections.Generic;
using Compze.Utilities.SystemCE.ThreadingCE;
using Compze.Utilities.SystemCE.ThreadingCE.ResourceAccess;

// ReSharper disable StaticMemberInGenericType

namespace Compze.Utilities.DependencyInjection;

class TypeIndex<TInheritor> where TInheritor : TypeIndex<TInheritor>
{
   static readonly ILock Lock = ILock.WithDefaultTimeout();
   internal static int ServiceCount { get; private set; }
   static IReadOnlyDictionary<Type, int> _map = new Dictionary<Type, int>();

   static Type[] _backMap = [];

   internal static int For(Type type)
   {
      if(_map.TryGetValue(type, out var value))
         return value;

      using(Lock.TakeUpdateLock())
      {
         if(_map.TryGetValue(type, out var value2))
            return value2;

         OnlyWithinLocksThreadingHelpers.AddToCopyAndReplace(ref _backMap, type);
         OnlyWithinLocksThreadingHelpers.AddToCopyAndReplace(ref _map, type, ServiceCount++);
         return ServiceCount - 1;
      }
   }

   internal static class ForService<TType>
   {
      internal static readonly int Index = ServiceTypeIndex.For(typeof(TType));
   }
}