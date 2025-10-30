using System;
using System.Collections.Generic;
using System.Transactions;
using Compze.Core.Tessaging.Public;
using Compze.Core.Tessaging.Typermedia.Infrastructure.Validation;
using Compze.Utilities.SystemCE.LinqCE;

namespace Compze.Core.Tessaging.Teventive.Infrastructure.Validation;

static partial class TessageInspector
{
   internal static void AssertValid(IReadOnlyList<Type> teventTypesToInspect) => teventTypesToInspect.ForEach(TessageTypeInspector.AssertValid);

   internal static void AssertValidForSubscription<TTessage>() => TessageTypeInspector.AssertValidForSubscription(typeof(TTessage));

   internal static void AssertValid<TTessage>() => TessageTypeInspector.AssertValid(typeof(TTessage));

   internal static void AssertValidToSendRemote(ITessage tessage)
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
         case IAtMostOnceTessage atMostOnce when atMostOnce.Id == null:
            throw new MissingTessageIdException(tessage);
      }
#pragma warning restore IDE0010
   }

   internal static void AssertValidToExecuteLocally(ITessage tessage)
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
