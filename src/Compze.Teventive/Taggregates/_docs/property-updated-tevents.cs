using Compze.Tessaging.Abstractions.Tessaging.Hosting.TessageHandling.Registration.Public;
using Compze.Teventive.Taggregates.Tevents.Public;
using Compze.Internals.SystemCE;
using static System.Console;

// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global

namespace Website.paradigms.semantic_tevents
{
   namespace NoCollision
   {
      #region IUserCreated
      interface IUserRegistered : IUserTevent, ITaggregateCreatedTevent;
      interface IUserChangedEmail : IUserTevent;
      #endregion
   }

   namespace NoCollision1
   {
      #region IUserEmailTeventsNaive
      interface IUserRegistered : IUserTevent, ITaggregateCreatedTevent
      {
         Email Email { get; }
      }

      interface IUserChangedEmail : IUserTevent
      {
         Email Email { get; }
      }
      #endregion
   }

   namespace NoCollision2
   {
      #region IUserEmailTeventsWorking
      interface IUserEmailPropertyUpdated : IUserTevent
      {
         Email Email { get; }
      }

      interface IUserRegistered : IUserEmailPropertyUpdated, ITaggregateCreatedTevent;
      interface IUserChangedEmail : IUserEmailPropertyUpdated;
      #endregion

      class Examples
      {
         public void IllustrateTeventListening()
         {
            ITessageHandlerRegistrar registrar = ((ITessageHandlerRegistrar)null!).NotNull();

            #region EmailPropertyUpdatedListener
            registrar
              .ForTevent<IUserEmailPropertyUpdated>(emailUpdated => WriteLine($"User: {emailUpdated.TaggregateId} Email: {emailUpdated.Email}"));
            #endregion
         }
      }
   }
}
