namespace Compze.Core.Tessaging.Teventive.Internal;

public interface ITaggregateTypeValidator
{
   void AssertIsValid<TTaggregate>();
}