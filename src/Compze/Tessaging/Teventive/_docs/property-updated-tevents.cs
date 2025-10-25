using Compze.Tessaging.Hosting.Abstractions;
using Compze.Tessaging.Teventive.TeventStore.Abstractions;
using Compze.Utilities.SystemCE;
using static System.Console;

// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global

namespace Website.paradigms.semantic_tevents
{
   namespace NoCollision
   {
      #region IUserCreated
      interface IUserRegistered : IUserTevent, IAggregateCreatedTevent;
      interface IUserChangedEmail : IUserTevent;
      #endregion
   }

   namespace NoCollision1
   {
      #region IUserEmailTeventsNaive
      interface IUserRegistered : IUserTevent, IAggregateCreatedTevent
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

      interface IUserRegistered : IUserEmailPropertyUpdated, IAggregateCreatedTevent;
      interface IUserChangedEmail : IUserEmailPropertyUpdated;
      #endregion

      class Examples
      {
         public void IllustrateTeventListening()
         {
            TessageHandlerRegistrarWithDependencyInjectionSupport registrar = ((TessageHandlerRegistrarWithDependencyInjectionSupport)null!).NotNull();

            #region EmailPropertyUpdatedListener
            registrar
              .ForTevent<IUserEmailPropertyUpdated>(emailUpdated => WriteLine($"User: {emailUpdated.AggregateId} Email: {emailUpdated.Email}"));
            #endregion
         }
      }
   }
}
