using System.Globalization;
using System.Text;
using Compze.Threading.Exceptions;
using Compze.Tessaging._internal.TessagesInFlight;

namespace Compze.Tessaging._private.TessagesInFlight;

class AwaitNoTessagesInFlightTimeoutException(AwaitingConditionTimeoutException innerException, IReadOnlyList<TessagesInFlightTracker.InFlightTessage> undeliveredTessages, IReadOnlyDictionary<string, int> pendingObservations)
   : Exception(FormatMessage(undeliveredTessages, pendingObservations), innerException)
{
   static string FormatMessage(IReadOnlyList<TessagesInFlightTracker.InFlightTessage> undeliveredTessages, IReadOnlyDictionary<string, int> pendingObservations)
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

      if(pendingObservations.Count > 0)
      {
         sb.AppendLine(CultureInfo.InvariantCulture,
                       $"""

                        ========== PENDING TEVENT OBSERVATIONS ({pendingObservations.Values.Sum()}) ==========
                        """);
         foreach(var pendingObservation in pendingObservations)
         {
            sb.AppendLine(CultureInfo.InvariantCulture, $"{pendingObservation.Key}: {pendingObservation.Value} queued but not yet dispatched");
         }
      }

      return sb.ToString();
   }
}
