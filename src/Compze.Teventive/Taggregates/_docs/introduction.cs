using Compze.Tessaging.Engine;
using Compze.Teventive.Taggregates.Tevents.Public;
using static System.Console;

#pragma warning disable // Documentation example code: deliberately illustrative fragments (empty marker interfaces, never-instantiated examples), not production code.

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
            TessageHandlerRegistrar registrar = null!;

            #region UserTeventRegistration
            registrar
              .ForTevent<IUserTevent>(userTevent => WriteLine($"User: {userTevent.TaggregateId} something happened"))
              .ForTevent<IUserRegistered>(userRegistered => WriteLine($"User: {userRegistered.TaggregateId} registered"))
              .ForTevent<IUserImported>(userImported => { WriteLine($"User: {userImported.TaggregateId} imported"); return Task.CompletedTask; });
            #endregion
         }
      }
   }
}
