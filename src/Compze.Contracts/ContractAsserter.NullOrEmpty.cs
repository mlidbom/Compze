using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Compze.Contracts;

public static class ContractAsserterNullOrEmptyExtensions
{
   extension(ContractAsserter @this)
   {
      public ContractAsserter NotNullOrDefault<TValue>([NotNull] TValue? value, [CallerArgumentExpression(nameof(value))] string valueString = "") where TValue : struct
      {
         if(value == null || Equals(value, default(TValue))) @this.ThrowFailed(valueString);
         return @this;
      }

      [return: NotNull] public TValue ReturnNotNull<TValue>([NotNull] TValue? value, [CallerArgumentExpression(nameof(value))] string valueString = "")
      {
         if(value is null) @this.ThrowNull(valueString);
         return value;
      }

      public TValue ReturnNotNullOrDefault<TValue>([NotNull] TValue? value, [CallerArgumentExpression(nameof(value))] string valueString = "") where TValue : struct
      {
         @this.NotNullOrDefault(value, valueString);
         return (TValue)value;
      }

      public TValue ReturnNotDefault<TValue>(TValue value, [CallerArgumentExpression(nameof(value))] string valueString = "")
         where TValue : struct
      {
         if(value.Equals(default(TValue))) @this.ThrowFailed(valueString);
         return value;
      }
   }
}
