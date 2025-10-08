using Compze.Utilities.DependencyInjection.Abstractions;

namespace Compze.Tessaging.Hosting.Abstractions;

public class EndpointConfiguration
{
   internal readonly IRunMode Mode;

   internal string Name { get; }
   internal EndpointId Id { get; }
   public string ConnectionStringName { get; }
   internal bool IsPureClientEndpoint { get; }


   internal EndpointConfiguration(string name, EndpointId id, IRunMode mode, bool isPureClientEndpoint)
   {
      Mode = mode;
      Name = name;
      Id = id;
      IsPureClientEndpoint = isPureClientEndpoint;
      ConnectionStringName = $"HostedEndpoint.{Name}.ConnectionString";
   }
}
