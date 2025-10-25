namespace Compze.Abstractions.Tessaging.Hosting.Public;

public class EndpointConfiguration
{
   internal string Name { get; }
   internal EndpointId Id { get; }
   public string ConnectionStringName { get; }
   internal bool IsPureClientEndpoint { get; }


   internal EndpointConfiguration(string name, EndpointId id, bool isPureClientEndpoint)
   {
      Name = name;
      Id = id;
      IsPureClientEndpoint = isPureClientEndpoint;
      ConnectionStringName = $"HostedEndpoint.{Name}.ConnectionString";
   }
}
