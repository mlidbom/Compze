using System;
using System.Diagnostics.CodeAnalysis;

namespace Compze.Contracts;

static class NullOrDefaultTester<TType>
{
   static readonly Func<TType, bool> IsNullOrDefaultInternal = CreateNullOrDefaultTester();
   static Func<TType, bool> CreateNullOrDefaultTester()
   {
      var type = typeof(TType);

      if (type.IsInterface || type == typeof(object))
      {
         return obj => obj is null || obj.GetType().IsValueType && Equals(obj, Activator.CreateInstance(obj.GetType()));
      }

      if (type.IsClass)
      {
         return obj => obj is null;
      }

      if (type.IsValueType)
      {
         var defaultValue = Activator.CreateInstance(type);
         return obj => Equals(obj, defaultValue);
      }

      throw new Exception("WTF");
   }

   [return: NotNull]public static TType AssertNotNullOrDefault([NotNull]TType? obj, Func<Exception> exceptionBuilder)
   {
      if(IsNullOrDefaultInternal(obj!))
      {
         throw exceptionBuilder();
      }

#pragma warning disable CS8777 // Parameter must have a non-null value when exiting.
      return obj!;
#pragma warning restore CS8777 // Parameter must have a non-null value when exiting.
   }
}