namespace Compze.Core.Tessaging.Hosting.Public;

public class EndpointConfiguration
{
   public string Name { get; }
   public EndpointId Id { get; }
   public string ConnectionStringName { get; }

   public EndpointConfiguration(string name, EndpointId id)
   {
      Name = name;
      Id = id;
      ConnectionStringName = $"HostedEndpoint.{Name}.ConnectionString";
   }
}
