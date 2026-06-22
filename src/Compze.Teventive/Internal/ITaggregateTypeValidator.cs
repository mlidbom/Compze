namespace Compze.Teventive.Internal;

public interface ITaggregateTypeValidator
{
   void AssertIsValid<TTaggregate>();
}