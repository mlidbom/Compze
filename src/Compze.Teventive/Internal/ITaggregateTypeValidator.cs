namespace Compze.Tessaging.Teventive.Internal;

public interface ITaggregateTypeValidator
{
   void AssertIsValid<TTaggregate>();
}