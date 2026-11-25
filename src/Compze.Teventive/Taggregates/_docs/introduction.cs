using Compze.Abstractions;
using Compze.Tessaging.TessageBus;
using Compze.Tessaging.TessageTypes;
using Compze.Teventive.Taggregates.Tevents;
using static System.Console;

#pragma warning disable // Documentation example code: deliberately illustrative fragments (empty marker interfaces, never-instantiated examples), not production code.

namespace Website.Teventive.Taggregates
{
   #region IUserTevent
   interface IUserTevent : ITaggregateTevent;
   #endregion

   namespace Introduction
   {
      //Copies of Compze's own declarations, without their documentation comments, for the page to show: keep them the same as
      //   Compze.Tessaging.TessageTypes.ITevent, Compze.Teventive.Taggregates.Tevents.ITaggregateTevent and ITaggregateCreatedTevent.
      namespace HideThisStuff
      {
         #region ITevent
         public interface ITevent : ITessage;
         #endregion

         #region ITaggregateTevent
         public interface ITaggregateTevent : ITevent
         {
            int TaggregateVersion { get; }
            TaggregateId TaggregateId { get; }
            DateTime UtcTimeStamp { get; }
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
            TessageBusHandlerRegistrar registrar = null!;

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
