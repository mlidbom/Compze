namespace Compze.Teventive.Internal;

interface ITaggregateTypeValidator
{
   void AssertIsValid<TTaggregate>();
}
