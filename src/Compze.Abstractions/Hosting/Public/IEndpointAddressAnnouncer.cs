namespace Compze.Abstractions.Hosting.Public;

///<summary>An endpoint's way of announcing where it listens, so that other processes can find it — the write-side counterpart<br/>
/// of <see cref="IEndpointRegistry"/>, through which senders read the announced addresses.</summary>
///<remarks>An endpoint declares who it announces to in its composition (<c>AnnounceAddressTo(...)</c>, or <c>ParticipateIn(...)</c><br/>
/// for a registry with both faces); declaring none — a deployment whose<br/>
/// endpoints are found through a fixed address list — means nothing is announced. The announced address is the endpoint's one transport-server<br/>
/// address, serving every distributed capability the endpoint speaks. The endpoint announces in its announcing phase — after<br/>
/// its listening phase and before its sending phase — so an announced address is always one that is actually listening; it<br/>
/// retracts in the mirror phase, before its sending stops, so the address stops being advertised before anything goes deaf.</remarks>
public interface IEndpointAddressAnnouncer
{
   ///<summary>Announces that the endpoint listens at <paramref name="address"/>, replacing any address it announced before.</summary>
   void AnnounceEndpointAddress(EndpointId endpointId, EndpointAddress address);

   ///<summary>Retracts the endpoint's announced address — what an endpoint does when it stops listening.</summary>
   void RetractEndpointAddress(EndpointId endpointId);
}
