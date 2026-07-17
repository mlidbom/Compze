using System.Globalization;
using System.Text;
using Compze.Threading.Exceptions;

namespace Compze.Tessaging.Implementation;

class AwaitNoTessagesInFlightTimeoutException(AwaitingConditionTimeoutException innerException, IReadOnlyList<TessagesInFlightTracker.InFlightTessage> undeliveredTessages)
   : Exception(FormatMessage(undeliveredTessages), innerException)
{
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
