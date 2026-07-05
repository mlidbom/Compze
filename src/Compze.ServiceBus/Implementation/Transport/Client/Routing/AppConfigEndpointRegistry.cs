using Compze.Abstractions.Configuration.Internal;
using Compze.Abstractions.Hosting.Public;

namespace Compze.ServiceBus.Implementation.Transport.Client.Routing;

class AppConfigEndpointRegistry(IConfigurationParameterProvider settingsProvider) : IEndpointRegistry
{
#pragma warning disable CA1823
   // ReSharper disable once UnusedMember.Local
   readonly IConfigurationParameterProvider _settingsProvider = settingsProvider;
#pragma warning restore CA1823

   public IEnumerable<EndpointAddress> ServerEndpointAddresses => throw new NotSupportedException();
   // var configurationValue = _settingsProvider.GetString("ServerEndpoints");
   // var addresses = configurationValue.Split(';')
   //                                   .Select(stringValue => stringValue.Trim())
   //                                   .Where(stringValue => !string.IsNullOrEmpty(stringValue))
   //                                   .Select(stringValue => new EndpointAddress(stringValue)).ToList();
   // return addresses;
}
