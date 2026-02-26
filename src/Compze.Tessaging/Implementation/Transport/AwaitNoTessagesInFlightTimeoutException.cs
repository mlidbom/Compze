using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Compze.Utilities.SystemCE.ThreadingCE.ResourceAccess;

namespace Compze.Tessaging.Implementation.Transport;

public class AwaitNoTessagesInFlightTimeoutException(
   AwaitingConditionTimeoutException innerException,
   IReadOnlyList<TessagesInFlightTracker.InFlightTessage> undeliveredTessages,
   IReadOnlyList<Exception> busExceptions)
   : AwaitingConditionTimeoutException(innerException, FormatMessage(undeliveredTessages, busExceptions))
{
   public IReadOnlyList<TessagesInFlightTracker.InFlightTessage> UndeliveredTessages { get; } = undeliveredTessages;
   public IReadOnlyList<Exception> BusExceptions { get; } = busExceptions;

   static string FormatMessage(
      IReadOnlyList<TessagesInFlightTracker.InFlightTessage> undeliveredTessages,
      IReadOnlyList<Exception> busExceptions)
   {
      var sb = new StringBuilder();
      sb.AppendLine("AwaitNoTessagesInFlight timed out.");

      if(undeliveredTessages.Count > 0)
      {
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
      } else
      {
         sb.AppendLine("""

                       No undelivered tessages (all tessages were delivered before the diagnostic report was created).
                       """);
      }

      if(busExceptions.Count > 0)
      {
         sb.AppendLine(CultureInfo.InvariantCulture,
                       $"""

                        ========== BUS EXCEPTIONS ({busExceptions.Count}) ==========
                        """);

         for(var i = 0; i < busExceptions.Count; i++)
         {
            sb.AppendLine(CultureInfo.InvariantCulture,
                          $"""

                           --- Exception {i + 1} of {busExceptions.Count} ---
                           {busExceptions[i]}
                           """);
         }
      }

      return sb.ToString();
   }
}
