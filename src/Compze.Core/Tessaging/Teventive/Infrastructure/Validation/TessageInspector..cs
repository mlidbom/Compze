using System.Transactions;
using Compze.Abstractions.Tessaging.Public;
using Compze.Core.Tessaging.Typermedia.Infrastructure.Validation;

namespace Compze.Core.Tessaging.Teventive.Infrastructure.Validation;

public static partial class TessageInspector
{
   internal static void AssertValid(IReadOnlyList<Type> teventTypesToInspect) => teventTypesToInspect.ForEach(TessageTypeInspector.AssertValid);

   public static void AssertValidForSubscription<TTessage>() => TessageTypeInspector.AssertValidForSubscription(typeof(TTessage));

   public static void AssertValid<TTessage>() => TessageTypeInspector.AssertValid(typeof(TTessage));

   public static void AssertValidToSendRemote(ITessage tessage)
   {
      CommonAssertions(tessage);

#pragma warning disable IDE0010
      switch(tessage)
      {
         case IStrictlyLocalTessage strictlyLocalTessage:
            throw new AttemptToSendStrictlyLocalTessageRemotelyException(strictlyLocalTessage);
         case IMustBeSentTransactionally when Transaction.Current == null:
            throw new MissingTransactionException(tessage);
         case ICannotBeSentRemotelyFromWithinTransaction when Transaction.Current != null:
            throw new TransactionPresentException(tessage);
      }
#pragma warning restore IDE0010
   }

   public static void AssertValidToExecuteLocally(ITessage tessage)
   {
      CommonAssertions(tessage);

      if(tessage is IMustBeSentTransactionally && Transaction.Current == null)
         throw new MissingTransactionException(tessage);
   }

   static void CommonAssertions(ITessage tessage)
   {
      TessageTypeInspector.AssertValid(tessage.GetType());
      if(tessage is ITommand tommand) TommandValidator.AssertTommandIsValid(tommand);
   }
}
