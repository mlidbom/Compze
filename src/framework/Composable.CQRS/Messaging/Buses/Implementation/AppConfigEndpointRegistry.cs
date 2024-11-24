using System;
using System.Collections.Generic;
using Composable.SystemCE.ConfigurationCE;

namespace Composable.Messaging.Buses.Implementation;

class AppConfigEndpointRegistry : IEndpointRegistry
{
#pragma warning disable IDE0052
   // ReSharper disable once NotAccessedField.Local
   readonly IConfigurationParameterProvider _settingsProvider;
#pragma warning restore IDE0052
   public AppConfigEndpointRegistry(IConfigurationParameterProvider settingsProvider) => _settingsProvider = settingsProvider;

   public IEnumerable<EndPointAddress> ServerEndpoints => throw new NotImplementedException();
   // var configurationValue = _settingsProvider.GetString("ServerEndpoints");
   // var addresses = configurationValue.Split(';')
   //                                   .Select(stringValue => stringValue.Trim())
   //                                   .Where(stringValue => !string.IsNullOrEmpty(stringValue))
   //                                   .Select(stringValue => new EndPointAddress(stringValue)).ToList();
   // return addresses;
}