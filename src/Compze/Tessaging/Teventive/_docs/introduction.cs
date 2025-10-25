using System;
using Compze.Tessaging.Hosting.Abstractions;
using Compze.Tessaging.Teventive.TeventStore.Abstractions;
using Compze.Utilities.SystemCE;
using static System.Console;

namespace Website.paradigms.semantic_tevents
{
   #region IUserTevent
   interface IUserTevent : IAggregateTevent;
   #endregion

   namespace Introduction
   {
      namespace HideThisStuff
      {
         #region ITevent
         public interface ITevent;
         #endregion

         #region IAggregateTevent
         public interface IAggregateTevent : ITevent
         {
            Guid AggregateId { get; }
         }
         #endregion

         #region IAggregateCreatedTevent
         public interface IAggregateCreatedTevent : IAggregateTevent;
         #endregion
      }

      #region UserTevents1
      interface IUserCreated : IUserTevent, IAggregateCreatedTevent;
      interface IUserRegistered : IUserCreated;
      interface IUserImported : IUserRegistered;
      #endregion

      class Examples
      {
         public void IllustrateTeventListening()
         {
            TessageHandlerRegistrarWithDependencyInjectionSupport registrar = ((TessageHandlerRegistrarWithDependencyInjectionSupport)null!).NotNull();

            #region UserTeventRegistration
            registrar
              .ForTevent<IUserTevent>(userTevent => WriteLine($"User: {userTevent.AggregateId} something happened"))
              .ForTevent<IUserRegistered>(userRegistered => WriteLine($"User: {userRegistered.AggregateId} registered"))
              .ForTevent<IUserImported>(userImported => WriteLine($"User: {userImported.AggregateId} imported"));
            #endregion
         }
      }
   }
}
