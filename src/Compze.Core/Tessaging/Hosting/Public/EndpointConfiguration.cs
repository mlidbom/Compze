namespace Compze.Core.Tessaging.Hosting.Public;

public class EndpointConfiguration
{
   public string Name { get; }
   public EndpointId Id { get; }
   public string ConnectionStringName { get; }
   //todo: find cleaner way of getting a TyperMedia navigator than pretending to be an endpoint.
   public bool IsPureClientEndpoint { get; }


   public EndpointConfiguration(string name, EndpointId id, bool isPureClientEndpoint)
   {
      Name = name;
      Id = id;
      IsPureClientEndpoint = isPureClientEndpoint;
      ConnectionStringName = $"HostedEndpoint.{Name}.ConnectionString";
   }
}
