using Composable.Contracts;
using Composable.DDD;

namespace Composable.Messaging.Buses.Implementation;

public class EndPointAddress : ValueObject<EndPointAddress>
{
   internal string NetMqAddress { get; }
   public string AspNetAddress { get; }
   internal EndPointAddress(string netMqAddress, string aspNetAddress)
   {
      Contract.ArgumentNotNullEmptyOrWhitespace(netMqAddress, nameof(netMqAddress));
      NetMqAddress = netMqAddress;
      AspNetAddress = aspNetAddress;
   }
}