using System;
using System.Transactions;
using Compze.Utilities.SystemCE.TransactionsCE;

namespace Compze.Utilities.SystemCE.TransactionsCE.Testing;

public static class TransactionInterceptorExtensions
{
   public static void FailOnPrepare(this Transaction @this, Exception? exception = null) =>
      @this.AddPrepareTasks(() =>
      {
         if(exception != null) throw exception;
         else throw new Exception($"{nameof(TransactionInterceptorExtensions)}.{nameof(FailOnPrepare)}");
      });
}
