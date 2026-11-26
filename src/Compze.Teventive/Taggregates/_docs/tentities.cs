using Compze.Abstractions;
using Compze.Teventive;
using Compze.Teventive.Taggregates.BaseClasses;
using Compze.Teventive.Taggregates.Tevents;

#pragma warning disable // Documentation example code: a small example domain, shown in regions.
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedMember.Global

// ReSharper disable once CheckNamespace
namespace Website.Teventive.Taggregates.Tentities;

#region tevents
interface IUserTevent : ITaggregateTevent
{
   interface IPropertyUpdated : IUserTevent
   {
      interface IEmail : IPropertyUpdated
      {
         string Email { get; }
      }
   }

   interface IRegistered : IUserTevent, ITaggregateCreatedTevent, IPropertyUpdated.IEmail;

   interface IAddress : IUserTevent
   {
      Guid AddressId { get; }

      interface IPropertyUpdated : IAddress
      {
         interface IStreet : IPropertyUpdated
         {
            string Street { get; }
         }
      }

      interface IAdded : IAddress, IPropertyUpdated.IStreet;
      interface IMoved : IAddress, IPropertyUpdated.IStreet;
      interface IRemoved : IAddress;
   }
}

interface IUserTevent<out T> : ITaggregateTevent<T> where T : IUserTevent;
#endregion

#region tevent-classes
abstract class UserTevent : TaggregateTevent, IUserTevent
{
   protected UserTevent() {}
   UserTevent(TaggregateId userId) : base(userId) {}

   internal class Registered(TaggregateId userId, string email) : UserTevent(userId), IUserTevent.IRegistered
   {
      public string Email { get; } = email;
   }

   internal abstract class Address : UserTevent, IUserTevent.IAddress
   {
      public Guid AddressId { get; private set; }

      public class IdGetterSetter : IGetSetTaggregateEntityTeventEntityId<Guid, Address, IUserTevent.IAddress>
      {
         public void SetEntityId(Address tevent, Guid id) => tevent.AddressId = id;
         public Guid GetId(IUserTevent.IAddress tevent) => tevent.AddressId;
      }

      internal class Added : Address, IUserTevent.IAddress.IAdded
      {
         public Added(Guid addressId, string street)
         {
            AddressId = addressId;
            Street = street;
         }

         public string Street { get; }
      }

      internal class Moved(string street) : Address, IUserTevent.IAddress.IMoved
      {
         public string Street { get; } = street;
      }

      internal class Removed : Address, IUserTevent.IAddress.IRemoved;
   }
}

class UserTevent<T>(T tevent) : TaggregateTevent<T>(tevent), IUserTevent<T> where T : IUserTevent;
#endregion

#region address
class Address : User.RemovableEntity<Address,
                                     Guid,
                                     IUserTevent.IAddress,
                                     UserTevent.Address,
                                     IUserTevent.IAddress.IAdded,
                                     IUserTevent.IAddress.IRemoved,
                                     UserTevent.Address.IdGetterSetter>
{
   static readonly TeventRouting<Address, IUserTevent.IAddress> Appliers = DeclareAppliers(it => it
     .For<IUserTevent.IAddress.IPropertyUpdated.IStreet>(static (address, updated) => address.Street = updated.Street));

   protected override TeventRouting TeventAppliers => Appliers;

   public Address(User user) : base(user) {}

   public string Street { get; private set; } = "";

   public void MoveTo(string street) => Publish(new UserTevent.Address.Moved(street));
   public void Remove() => Publish(new UserTevent.Address.Removed());
}
#endregion

#region user
class User : Taggregate<User, IUserTevent, UserTevent, IUserTevent<IUserTevent>, UserTevent<UserTevent>>
{
   static readonly TeventRouting<User, IUserTevent> Appliers = DeclareAppliers(it => it
     .For<IUserTevent.IPropertyUpdated.IEmail>(static (user, updated) => user.Email = updated.Email)
     .Entities(static user => user._addresses));

   protected override TeventRouting TeventAppliers => Appliers;

   readonly Address.ICollectionManager _addresses;

   User() => _addresses = Address.CreateSelfManagingCollection(this);

   public string Email { get; private set; } = "";
   public IReadOnlyEntityCollection<Address, Guid> Addresses => _addresses.Entities;

   public Address AddAddress(string street) => _addresses.AddByPublishing(new UserTevent.Address.Added(Guid.NewGuid(), street));

   public static User Register(string email)
   {
      var user = new User();
      user.Publish(new UserTevent.Registered(TaggregateId.New(), email));
      return user;
   }
}
#endregion

static class TentitiesExample
{
   internal static (User user, string streetAfterMoving, int addressesBeforeRemoving) Run()
   {
      #region using
      var user = User.Register("ada@example.com");
      var home = user.AddAddress("12 Marylebone Road");
      home.MoveTo("1 St James's Square");
      //home.Street is now "1 St James's Square", and user.Addresses[home.Id] is home.
      home.Remove();
      //user.Addresses is empty again.
      #endregion
      return (user, home.Street, 1);
   }
}
