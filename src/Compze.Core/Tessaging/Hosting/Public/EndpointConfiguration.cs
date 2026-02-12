namespace Compze.Core.Tessaging.Hosting.Public;

public class EndpointConfiguration
{
   internal string Name { get; }
   internal EndpointId Id { get; }
   public string ConnectionStringName { get; }
   //todo: find cleaner way of getting a TyperMedia navigator than pretending to be an endpoint.
   internal bool IsPureClientEndpoint { get; }


   internal EndpointConfiguration(string name, EndpointId id, bool isPureClientEndpoint)
   {
      Name = name;
      Id = id;
      IsPureClientEndpoint = isPureClientEndpoint;
      ConnectionStringName = $"HostedEndpoint.{Name}.ConnectionString";
   }
}
