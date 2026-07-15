using System;
using Compze.Tessaging.Abstractions.Tessaging.Hosting.TessageHandling.Registration.Public;
using Compze.Teventive.Taggregates.Tevents.Public;
using Compze.Internals.SystemCE;
using static System.Console;

namespace Website.paradigms.semantic_tevents
{
   #region IUserTevent
   interface IUserTevent : ITaggregateTevent;
   #endregion

   namespace Introduction
   {
      namespace HideThisStuff
      {
         #region ITevent
         public interface ITevent;
         #endregion

         #region ITaggregateTevent
         public interface ITaggregateTevent : ITevent
         {
            Guid TaggregateId { get; }
         }
         #endregion

         #region ITaggregateCreatedTevent
         public interface ITaggregateCreatedTevent : ITaggregateTevent;
         #endregion
      }

      #region UserTevents1
      interface IUserCreated : IUserTevent, ITaggregateCreatedTevent;
      interface IUserRegistered : IUserCreated;
      interface IUserImported : IUserRegistered;
      #endregion

      class Examples
      {
         public void IllustrateTeventListening()
         {
            ITessageHandlerRegistrar registrar = ((ITessageHandlerRegistrar)null!).NotNull();

            #region UserTeventRegistration
            registrar
              .ForTevent<IUserTevent>(userTevent => WriteLine($"User: {userTevent.TaggregateId} something happened"))
              .ForTevent<IUserRegistered>(userRegistered => WriteLine($"User: {userRegistered.TaggregateId} registered"))
              .ForTevent<IUserImported>(userImported => WriteLine($"User: {userImported.TaggregateId} imported"));
            #endregion
         }
      }
   }
}
