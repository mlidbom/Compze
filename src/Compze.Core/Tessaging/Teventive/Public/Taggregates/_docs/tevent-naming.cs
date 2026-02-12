using Compze.Tessaging.Hosting.Abstractions;
using Compze.Tessaging.Teventive.TeventStore.Abstractions;
using Compze.Utilities.SystemCE;
using static System.Console;
using Tessaging_ITevent = Compze.Tessaging.Abstractions.ITevent;

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
         interface IName : Tessaging_ITevent;

         public void IllustrateTeventListening()
         {
            TessageHandlerRegistrarWithDependencyInjectionSupport registrar = ((TessageHandlerRegistrarWithDependencyInjectionSupport)null!).NotNull();

            #region Unhelpful
            registrar.ForTevent<IName>(nameTevent => WriteLine("Uhmm... What is happening here?"));
            #endregion

            #region helpful
            registrar
              .ForTevent<UserTevent.Profile.PropertyUpdated.IName>(clarity => WriteLine($"Ahh: {clarity.Name}"));
            #endregion

            #region helpful2
            registrar
              .ForTevent<IUserTevent.IProfile.IPropertyUpdated.IName>(clarity => WriteLine($"Ahh: {clarity.Name}"));
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