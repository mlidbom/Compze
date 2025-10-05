using Compze.Configuration.Abstractions;
using Compze.Hosting.Abstractions;

namespace Compze.Tessaging.Hosting.Implementation;

class RealEndpointConfiguration
{
   readonly EndpointConfiguration _conf;
   readonly IConfigurationParameterProvider _configurationParameterProvider;
   public RealEndpointConfiguration(EndpointConfiguration conf, IConfigurationParameterProvider configurationParameterProvider)
   {
      _conf = conf;
      _configurationParameterProvider = configurationParameterProvider;

      if(_conf.Mode.IsTesting)
      {
         Address = "tcp://localhost:0";
      } else
      {
         Address = IsPureClientEndpoint ? "invalid" : $"tcp://localhost:{EndpointConfigurationValue("Port")}";
      }
   }

   internal string Address { get; }

   internal string Name => _conf.Name;
   internal EndpointId Id => _conf.Id;
   internal bool IsPureClientEndpoint => _conf.IsPureClientEndpoint;

   string EndpointConfigurationValue(string name) => _configurationParameterProvider.GetString(ConfigurationParameterName(name)).Trim();
   string ConfigurationParameterName(string name) => $"HostedEndpoint.{Name}.{name}";
}
