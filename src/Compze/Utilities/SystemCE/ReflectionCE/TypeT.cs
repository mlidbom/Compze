using System;

namespace Compze.Utilities.SystemCE.ReflectionCE;

public static class TypeTExtensions
{
   public static Type<T> DeclaredType<T>(this T @this) => Type<T>.Instance;
}

public class Type<T>
{
   public static readonly Type<T> Instance = new();
   Type() {}
   public TypeOperators Operators => TypeOperators.Instance;

   public class TypeOperators
   {
      // ReSharper disable once MemberHidesStaticFromOuterClass
      public static readonly TypeOperators Instance = new();
      TypeOperators() {}
      public readonly Func<T, T, bool>? Equality = TryGetBooleanOperator("op_Equality");
      public readonly Func<T, T, bool>? InEquality = TryGetBooleanOperator("op_Inequality");
      public readonly Func<T, T, bool>? LessThan = TryGetBooleanOperator("op_LessThan");
      public readonly Func<T, T, bool>? GreaterThan = TryGetBooleanOperator("op_GreaterThan");
      public readonly Func<T, T, bool>? LessThanOrEqual = TryGetBooleanOperator("op_LessThanOrEqual");
      public readonly Func<T, T, bool>? GreaterThanOrEqual = TryGetBooleanOperator("op_GreaterThanOrEqual");

      static Func<T, T, bool>? TryGetBooleanOperator(string operatorName)
      {
         var method = typeof(T).GetMethod(operatorName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static, null, [typeof(T), typeof(T)], null);

         // Only use operators that actually return bool (not SqlBoolean or other "boolean-like" types)
         if(method == null || method.ReturnType != typeof(bool))
            return null;

         return (left, right) => (bool)method.Invoke(null, [left, right])!;
      }
   }
}
