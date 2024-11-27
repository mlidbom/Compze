using System;
using System.Collections.Generic;
using Composable.SystemCE.ConfigurationCE;

namespace Composable.Messaging.Buses.Implementation;

class AppConfigEndpointRegistry(IConfigurationParameterProvider settingsProvider) : IEndpointRegistry
{
#pragma warning disable CA1823
   // ReSharper disable once UnusedMember.Local
   readonly IConfigurationParameterProvider _settingsProvider = settingsProvider;
#pragma warning restore CA1823

   public IEnumerable<EndPointAddress> ServerEndpoints => throw new NotImplementedException();
   // var configurationValue = _settingsProvider.GetString("ServerEndpoints");
   // var addresses = configurationValue.Split(';')
   //                                   .Select(stringValue => stringValue.Trim())
   //                                   .Where(stringValue => !string.IsNullOrEmpty(stringValue))
   //                                   .Select(stringValue => new EndPointAddress(stringValue)).ToList();
   // return addresses;
}