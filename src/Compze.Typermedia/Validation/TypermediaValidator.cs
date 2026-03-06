using System.Transactions;
using Compze.Abstractions.Tessaging.Public;

namespace Compze.Typermedia.Validation;

public static class TypermediaValidator
{
   public static void AssertValidToExecuteLocally(ITessage tessage)
   {
      TessageTypeInspector.AssertValid(tessage.GetType());
      if(tessage is ITommand tommand) TommandValidator.AssertTommandIsValid(tommand);

      if(tessage is IMustBeSentTransactionally && Transaction.Current == null)
         throw new MissingTransactionException(tessage);
   }

   public class MissingTransactionException(ITessage tessage) :
      Exception($"{tessage.GetType().FullName} is {typeof(IMustBeSentTransactionally).FullName} but there is no transaction.");
}
