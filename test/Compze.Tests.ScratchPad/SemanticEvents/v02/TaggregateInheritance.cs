// ReSharper disable All

#pragma warning disable IDE0059 // Unnecessary assignment of a value
#pragma warning disable IDE0051 // Remove unused private members

//When persisting tevent we would only persist the wrapped part. Thus changing from unwrapped-uninheritable to inheritable does not break storage and maybe one could even move tevents between clases in the hierarchy?
namespace Compze.Tests.ScratchPad.SemanticEvents.v02;

interface ITevent {}

interface IExactlyOnceTevent : ITevent
{
   Guid TeventId { get; }
}

interface ITaggregateTevent : IExactlyOnceTevent
{
   Guid TaggregateId { get; }
}

interface ITevent<out TTeventInterface> : ITevent
{
   TTeventInterface Tevent { get; }
}

interface IExactlyOnceTevent<out TTeventInterface> : ITevent<TTeventInterface> where TTeventInterface : IExactlyOnceTevent {}
interface ITaggregateTevent<out TTaggregateTeventInterface> : IExactlyOnceTevent<TTaggregateTeventInterface> where TTaggregateTeventInterface : ITaggregateTevent {}

interface IUserTevent<out TUserTeventInterface> : ITaggregateTevent<TUserTeventInterface> where TUserTeventInterface : IUserTevent {}
interface IManagerTevent<out TIBirdTeventInterface> : IUserTevent<TIBirdTeventInterface> where TIBirdTeventInterface : IUserTevent {}


interface IUserTevent : ITaggregateTevent {}
interface IUserRegisteredTevent : IUserTevent {}
interface IManagerTevent : IUserTevent {}
interface IManagerHiredTevent : IManagerTevent {}

public class TaggregateInheritance
{
   public void DemonstrateSemanticRelationships()
   {
      IUserTevent<IUserTevent> wrapperUserTevent = null!;
      IUserTevent<IUserRegisteredTevent> wrapperUserRegisteredTevent = null!;

      IManagerTevent<IUserTevent> wrapperManagerTevent = null!;
      IManagerTevent<IUserRegisteredTevent> wrapperManagerUserRegisteredTevent = null!;
      IManagerTevent<IManagerHiredTevent> wrappedManagerHiredEven = null!;
      wrapperUserTevent = wrapperUserRegisteredTevent = wrapperManagerUserRegisteredTevent;
      wrapperUserTevent = wrapperManagerTevent = wrapperManagerUserRegisteredTevent;
      wrapperUserTevent = wrappedManagerHiredEven;
   }
}

interface IAddressTevent {}
interface IAddressUpdatedTevent : IAddressTevent {}
interface IMovedTevent : IAddressUpdatedTevent {}

//Should it be a specific IUserAddressTevent<T> or IUserComponent<T> for all component tevents? Or should IUserAddressTevent<T> inherit IUserComponent<T>?  
interface IUserAddressTevent<out TAddressTeventInterface> : ITevent<TAddressTeventInterface>, IUserTevent {}
interface IManagerAddressTevent<out TAddressTeventInterface> : IUserAddressTevent<TAddressTeventInterface> {}

public class ReUsableTaggregateComponentsInInheritableTaggregates
{
   static void DemonstrateSemanticRelationships()
   {
      IUserTevent<IUserAddressTevent<IAddressTevent>> userAddressTevent = null!;
      IUserTevent<IUserAddressTevent<IAddressUpdatedTevent>> userAddressUpdatedTevent = null!;
      IUserTevent<IUserAddressTevent<IMovedTevent>> userMovedTevent = null!;

      IManagerTevent<IManagerAddressTevent<IAddressTevent>> managerAddressTevent = null!;
      IManagerTevent<IManagerAddressTevent<IAddressUpdatedTevent>> managerAddressUpdatedTevent = null!;
      IManagerTevent<IManagerAddressTevent<IMovedTevent>> managerMovedTevent = null!;

      //Semantic relationships are maintained.
      userAddressTevent = userAddressUpdatedTevent = userMovedTevent = managerMovedTevent;
      managerAddressTevent = managerAddressUpdatedTevent = managerMovedTevent;

      userAddressTevent = managerAddressTevent = managerAddressUpdatedTevent = managerMovedTevent;
      userAddressUpdatedTevent = managerAddressUpdatedTevent;
   }
}
