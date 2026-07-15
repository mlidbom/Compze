namespace Compze.Abstractions.Hosting.Public;

///<summary>An endpoint's way of announcing where it listens, so that other processes can find it — the write-side counterpart<br/>
/// of <see cref="IEndpointRegistry"/>, through which senders read the announced addresses.</summary>
///<remarks>An endpoint declares who it announces to on a distributed communication style's feature<br/>
/// (<c>AddTransientTessaging().AnnounceAddressTo(...)</c> — or through <c>AddExactlyOnceTessaging()</c>, which delegates); declaring none — a testing host with a static registry, a<br/>
/// configuration-file deployment — means nothing is announced. The announced address is the endpoint's one transport-server<br/>
/// address, serving every distributed capability the endpoint speaks. The endpoint announces once every endpoint in the host has<br/>
/// finished starting to listen — the host's sending phase — so an announced address is always one that is actually listening and<br/>
/// fully ready; it retracts as the first act of the host's stopping, before anything goes deaf.</remarks>
public interface IEndpointAddressAnnouncer
{
   ///<summary>Announces that the endpoint listens at <paramref name="address"/>, replacing any address it announced before.</summary>
   void AnnounceEndpointAddress(EndpointId endpointId, EndpointAddress address);

   ///<summary>Retracts the endpoint's announced address — what an endpoint does when it stops listening.</summary>
   void RetractEndpointAddress(EndpointId endpointId);
}
