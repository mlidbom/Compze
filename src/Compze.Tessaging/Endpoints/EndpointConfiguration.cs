namespace Compze.Tessaging.Endpoints;

///<summary>An endpoint's identity and naming, fixed at registration: the human-readable <see cref="Name"/>, the durable <see cref="Id"/>, and the configuration key its connection string is read from.</summary>
public class EndpointConfiguration(string name, EndpointId id)
{
   public string Name { get; } = name;
   public EndpointId Id { get; } = id;
   public string ConnectionStringName { get; } = $"HostedEndpoint.{name}.ConnectionString";
}
