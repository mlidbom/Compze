using Compze.Tessaging.Internal.Transport.Advertisement;
using Compze.Tessaging.TessageTypes;

namespace Compze.Tessaging.Private.Transport.Advertisement;

///<summary>The endpoint-discovery query "who are you, and which remotable tessage types do you serve?" — the one question a<br/>
/// connecting endpoint's router asks to learn the identity behind an address and build its routes, for every tessage kind at<br/>
/// once. Every transport-speaking endpoint serves it.</summary>
class EndpointInformationQuery : ITuery<EndpointInformation>;
