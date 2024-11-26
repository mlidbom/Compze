using Composable.Contracts;
using Composable.DDD;

namespace Composable.Messaging.Buses.Implementation;

public class EndPointAddress : ValueObject<EndPointAddress>
{
   public string AspNetAddress { get; }
   internal EndPointAddress(string aspNetAddress)
   {
      Contract.ArgumentNotNullEmptyOrWhitespace(aspNetAddress, nameof(aspNetAddress));
      AspNetAddress = aspNetAddress;
   }
}