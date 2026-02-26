using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Compze.Utilities.SystemCE.ThreadingCE.ResourceAccess;

namespace Compze.Tessaging.Implementation.Transport;

public class AwaitNoTessagesInFlightTimeoutException(AwaitingConditionTimeoutException innerException, IReadOnlyList<TessagesInFlightTracker.InFlightTessage> undeliveredTessages)
   : AwaitingConditionTimeoutException(innerException, FormatMessage(undeliveredTessages))
{
   public IReadOnlyList<TessagesInFlightTracker.InFlightTessage> UndeliveredTessages { get; } = undeliveredTessages;

   static string FormatMessage(IReadOnlyList<TessagesInFlightTracker.InFlightTessage> undeliveredTessages)
   {
      var sb = new StringBuilder();
      sb.AppendLine("AwaitNoTessagesInFlight timed out.");

      sb.Append(CultureInfo.InvariantCulture,
                $"""

                 ========== UNDELIVERED TESSAGES ({undeliveredTessages.Count}) ==========
                 """);

      foreach(var tessage in undeliveredTessages)
      {
         var pendingEndpoints = string.Join(", ", tessage.EndpointDeliveryStatus.Where(kvp => !kvp.Value).Select(kvp => kvp.Key));
         sb.AppendLine(CultureInfo.InvariantCulture,
                       $"""

                        --- TessageId: {tessage.TessageId} ---
                        Type: {tessage.TypeName}
                        Pending endpoints: {pendingEndpoints}
                        Body:
                        {tessage.Body}
                        """);
      }

      return sb.ToString();
   }
}
