namespace Compze.Abstractions.Tessaging.Public;

public static class PublisherIdentifyingTeventCE
{
   extension<TTevent>(IEnumerable<IPublisherIdentifyingTevent<TTevent>> @this) where TTevent : ITevent
   {
      ///<summary>Projects each wrapper in the sequence to its wrapped <see cref="IPublisherIdentifyingTevent{TTevent}.Tevent"/>.</summary>
      public IEnumerable<TTevent> Tevents() => @this.Select(it => it.Tevent);
   }
}
