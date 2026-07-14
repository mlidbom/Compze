namespace Compze.Abstractions.Hosting.Public;

///<summary>An endpoint's way of announcing where it listens, so that other processes can find it — the write-side counterpart<br/>
/// of <see cref="IEndpointRegistry"/>, through which senders read the announced addresses.</summary>
///<remarks>An endpoint declares who it announces to on its transport feature (<c>AddDistributedTessaging().AnnounceAddressTo(...)</c>);<br/>
/// declaring none — a testing host with a static registry, a configuration-file deployment — means nothing is announced. The endpoint's<br/>
/// transport component announces as the final act of starting to listen and retracts as the first act of stopping, so an announced<br/>
/// address is always one that is actually listening.</remarks>
public interface IEndpointAddressAnnouncer
{
   ///<summary>Announces that the endpoint listens at <paramref name="address"/>, replacing any address it announced before.</summary>
   void AnnounceEndpointAddress(EndpointId endpointId, EndpointAddress address);

   ///<summary>Retracts the endpoint's announced address — what an endpoint does when it stops listening.</summary>
   void RetractEndpointAddress(EndpointId endpointId);
}
