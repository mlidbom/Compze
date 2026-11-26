using Compze.Abstractions;
using Compze.Teventive;
using Compze.Teventive.Taggregates.BaseClasses;
using Compze.Teventive.Taggregates.Tevents;

#pragma warning disable // Documentation example code: a small example domain, shown in regions.
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedMember.Global

// ReSharper disable once CheckNamespace
namespace Website.Teventive.Taggregates.Tomponents;

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

   interface IProfile : IUserTevent
   {
      interface IPropertyUpdated : IProfile
      {
         interface IName : IPropertyUpdated
         {
            string Name { get; }
         }
      }

      interface IRenamed : IProfile, IPropertyUpdated.IName;
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

   internal abstract class Profile : UserTevent, IUserTevent.IProfile
   {
      internal class Renamed(string name) : Profile, IUserTevent.IProfile.IRenamed
      {
         public string Name { get; } = name;
      }
   }
}

class UserTevent<T>(T tevent) : TaggregateTevent<T>(tevent), IUserTevent<T> where T : IUserTevent;
#endregion

#region profile
class Profile : User.Component<Profile, IUserTevent.IProfile, UserTevent.Profile>
{
   static readonly TeventRouting<Profile, IUserTevent.IProfile> Appliers = DeclareAppliers(it => it
     .For<IUserTevent.IProfile.IPropertyUpdated.IName>(static (profile, updated) => profile.Name = updated.Name));

   protected override TeventRouting TeventAppliers => Appliers;

   public Profile(User user) : base(user) {}

   public string Name { get; private set; } = "";

   public void Rename(string name) => Publish(new UserTevent.Profile.Renamed(name));
}
#endregion

#region user
class User : Taggregate<User, IUserTevent, UserTevent, IUserTevent<IUserTevent>, UserTevent<UserTevent>>
{
   static readonly TeventRouting<User, IUserTevent> Appliers = DeclareAppliers(it => it
     .For<IUserTevent.IPropertyUpdated.IEmail>(static (user, updated) => user.Email = updated.Email)
     .Component(static user => user.Profile));

   protected override TeventRouting TeventAppliers => Appliers;

   User() => Profile = new Profile(this);

   public string Email { get; private set; } = "";
   public Profile Profile { get; }

   public static User Register(string email)
   {
      var user = new User();
      user.Publish(new UserTevent.Registered(TaggregateId.New(), email));
      return user;
   }
}
#endregion

static class TomponentsExample
{
   internal static User Run()
   {
      #region using
      var user = User.Register("ada@example.com");
      user.Profile.Rename("Ada Lovelace");
      //user.Profile.Name is now "Ada Lovelace", and user.Version is 2: the profile's tevent is the user's.
      #endregion
      return user;
   }
}
