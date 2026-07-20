
using Compze.Tessaging.TessageTypes;

namespace Compze.Tessaging.TessageBus;

public static class PublisherTeventCE
{
   extension<TTevent>(IEnumerable<IPublisherTevent<TTevent>> @this) where TTevent : ITevent
   {
      ///<summary>Projects each wrapper in the sequence to its wrapped <see cref="IPublisherTevent{TTevent}.Tevent"/>.</summary>
      public IEnumerable<TTevent> Tevents() => @this.Select(it => it.Tevent);
   }
}
