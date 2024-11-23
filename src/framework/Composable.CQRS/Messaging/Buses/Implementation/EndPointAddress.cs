using Composable.Contracts;
using Composable.DDD;

namespace Composable.Messaging.Buses.Implementation;

public class EndPointAddress : ValueObject<EndPointAddress>
{
   internal string NetMqAddress { get; private set; }
   internal EndPointAddress(string netMqAddress)
   {
      Contract.ArgumentNotNullEmptyOrWhitespace(netMqAddress, nameof(netMqAddress));
      NetMqAddress = netMqAddress;
   }
}