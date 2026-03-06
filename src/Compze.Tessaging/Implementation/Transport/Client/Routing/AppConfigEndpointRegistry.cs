using Compze.Abstractions.Configuration.Internal;
using Compze.Core.Tessaging.Transport.Internal;
using Compze.Tessaging.Implementation.Transport.Client.Routing.Abstractions;

namespace Compze.Tessaging.Implementation.Transport.Client.Routing;

class AppConfigEndpointRegistry(IConfigurationParameterProvider settingsProvider) : IEndpointRegistry
{
#pragma warning disable CA1823
   // ReSharper disable once UnusedMember.Local
   readonly IConfigurationParameterProvider _settingsProvider = settingsProvider;
#pragma warning restore CA1823

   public IEnumerable<EndPointAddress> ServerEndpointAddresses => throw new NotSupportedException();
   // var configurationValue = _settingsProvider.GetString("ServerEndpoints");
   // var addresses = configurationValue.Split(';')
   //                                   .Select(stringValue => stringValue.Trim())
   //                                   .Where(stringValue => !string.IsNullOrEmpty(stringValue))
   //                                   .Select(stringValue => new EndPointAddress(stringValue)).ToList();
   // return addresses;
}