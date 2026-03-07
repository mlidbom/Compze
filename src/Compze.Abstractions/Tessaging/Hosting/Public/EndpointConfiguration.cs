namespace Compze.Abstractions.Tessaging.Hosting.Public;

public class EndpointConfiguration(string name, EndpointId id)
{
   public string Name { get; } = name;
   public EndpointId Id { get; } = id;
   public string ConnectionStringName { get; } = $"HostedEndpoint.{name}.ConnectionString";
}
