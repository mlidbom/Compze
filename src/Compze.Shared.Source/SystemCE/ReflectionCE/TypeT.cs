using System;

namespace Compze.Utilities.SystemCE.ReflectionCE;

internal static class TypeTExtensions
{
   public static Type<T> DeclaredType<T>(this T @this) => Type<T>.Instance;
}

internal class Type<T>
{
   public static readonly Type<T> Instance = new();
   Type() {}
   public TypeOperators Operators => TypeOperators.Instance;

   internal class TypeOperators
   {
      // ReSharper disable once MemberHidesStaticFromOuterClass
      public static readonly TypeOperators Instance = new();
      TypeOperators() {}
      public Func<T, T, bool>? Equality { get; } = TryGetBooleanOperator("op_Equality");
      public Func<T, T, bool>? InEquality { get; } = TryGetBooleanOperator("op_Inequality");
      public Func<T, T, bool>? LessThan { get; } = TryGetBooleanOperator("op_LessThan");
      public Func<T, T, bool>? GreaterThan { get; } = TryGetBooleanOperator("op_GreaterThan");
      public Func<T, T, bool>? LessThanOrEqual { get; } = TryGetBooleanOperator("op_LessThanOrEqual");
      public Func<T, T, bool>? GreaterThanOrEqual { get; } = TryGetBooleanOperator("op_GreaterThanOrEqual");

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
