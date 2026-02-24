using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Compze.Contracts;

public partial class ContractAsserter
{
   public ContractAsserter Fulfills([DoesNotReturnIf(false)] bool assert1,
                                    [CallerArgumentExpression(nameof(assert1))] string expression1 = "")

   {
      if(!assert1) throw _createException(expression1);
      return this;
   }

   public ContractAsserter Fulfills([DoesNotReturnIf(false)] bool assert1,
                                    [DoesNotReturnIf(false)] bool assert2,
                                    [CallerArgumentExpression(nameof(assert1))] string expression1 = "",
                                    [CallerArgumentExpression(nameof(assert2))] string expression2 = "")

   {
      if(!assert1) throw _createException(expression1);
      if(!assert2) throw _createException(expression2);
      return this;
   }
}
