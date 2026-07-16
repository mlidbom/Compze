namespace Compze.Abstractions.Hosting.Public;

///<summary>An endpoint's way of announcing where it listens, so that other processes can find it — the write-side counterpart<br/>
/// of <see cref="IEndpointRegistry"/>, through which senders read the announced addresses.</summary>
///<remarks>An endpoint declares who it announces to on a distributed communication style's feature<br/>
/// (<c>AddDistributedTessaging().AnnounceAddressTo(...)</c> — or through <c>AddExactlyOnceTessaging()</c>, which delegates); declaring none — a deployment whose<br/>
/// endpoints are found through a fixed address list — means nothing is announced. The announced address is the endpoint's one transport-server<br/>
/// address, serving every distributed capability the endpoint speaks. The endpoint announces in the host's announcing phase —<br/>
/// after every endpoint in the host has finished starting to listen and before any endpoint starts sending — so an announced<br/>
/// address is always one that is actually listening, and a router's first look at a registry sees every endpoint the host<br/>
/// announced; it retracts in the mirror phase, before any sending stops, so the address stops being advertised before anything<br/>
/// goes deaf.</remarks>
public interface IEndpointAddressAnnouncer
{
   ///<summary>Announces that the endpoint listens at <paramref name="address"/>, replacing any address it announced before.</summary>
   void AnnounceEndpointAddress(EndpointId endpointId, EndpointAddress address);

   ///<summary>Retracts the endpoint's announced address — what an endpoint does when it stops listening.</summary>
   void RetractEndpointAddress(EndpointId endpointId);
}
