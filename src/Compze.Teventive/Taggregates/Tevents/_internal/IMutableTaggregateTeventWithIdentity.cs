using Compze.Abstractions;
using Compze.Tessaging;

namespace Compze.Teventive.Taggregates.Tevents._internal;

///<summary>As <see cref="IMutableTaggregateTevent"/>, for a tevent with identity: also the id a tevent store gives a tevent it reads back.</summary>
interface IMutableTaggregateTeventWithIdentity : IMutableTaggregateTevent, ITaggregateTeventWithIdentity
{
   void SetTessageIdInternal(TessageId tessageId);
}
