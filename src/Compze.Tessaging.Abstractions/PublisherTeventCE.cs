using Compze.Tessaging.Abstractions.TessageTypes;

namespace Compze.Tessaging.Abstractions.Public;

public static class PublisherTeventCE
{
   extension<TTevent>(IEnumerable<IPublisherTevent<TTevent>> @this) where TTevent : ITevent
   {
      ///<summary>Projects each wrapper in the sequence to its wrapped <see cref="IPublisherTevent{TTevent}.Tevent"/>.</summary>
      public IEnumerable<TTevent> Tevents() => @this.Select(it => it.Tevent);
   }
}
