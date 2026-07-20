using Compze.Tessaging.TessageBus;
using Compze.Tessaging.TessageTypes;
using Compze.Teventive.Taggregates.Tevents.Public;
using static System.Console;

#pragma warning disable // Documentation example code: deliberately illustrative fragments (empty marker interfaces, never-instantiated examples), not production code.

// ReSharper disable UnusedParameter.Local
// ReSharper disable ArrangeTypeMemberModifiers

// ReSharper disable ClassNeverInstantiated.Global

// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedType.Global
// ReSharper disable UnusedType.Local
// ReSharper disable PartialTypeWithSinglePart

namespace Website.paradigms.semantic_tevents
{
   namespace NoCollision3
   {
      class Unhelpful
      {
         interface IName : ITevent;

         public void IllustrateTeventListening()
         {
            TessageBusHandlerRegistrar registrar = null!;

            #region Unhelpful
            registrar.ForTevent<IName>(nameTevent => { WriteLine("Uhmm... What is happening here?"); return Task.CompletedTask; });
            #endregion

            #region helpful
            registrar
              .ForTevent<UserTevent.Profile.PropertyUpdated.IName>(clarity => { WriteLine($"Ahh: {clarity.Name}"); return Task.CompletedTask; });
            #endregion

            #region helpful2
            registrar
              .ForTevent<IUserTevent.IProfile.IPropertyUpdated.IName>(clarity => { WriteLine($"Ahh: {clarity.Name}"); return Task.CompletedTask; });
            #endregion
         }
      }

      #region nested-tevents
      partial class UserTevent
      {
         internal interface IUserTevent : ITaggregateTevent;

         internal static partial class Profile
         {
            internal interface IProfileTevent : IUserTevent;

            internal partial class PropertyUpdated
            {
               internal interface IName : IProfileTevent
               {
                  string Name { get; }
               }
            }
         }
      }
      #endregion

      #region nested-tevents2
      interface IUserTevent : ITaggregateTevent
      {
         internal interface IProfile : IUserTevent
         {
            internal interface IPropertyUpdated : IProfile
            {
               internal interface IName : IPropertyUpdated
               {
                  string Name { get; }
               }
            }
         }
      }
      #endregion
   }
}