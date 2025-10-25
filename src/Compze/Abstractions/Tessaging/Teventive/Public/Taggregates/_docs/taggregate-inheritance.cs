using System.Collections.Generic;
using Compze.Tessaging.Hosting.Abstractions;
using Compze.Tessaging.Teventive.TeventStore.Abstractions;
using Compze.Utilities.SystemCE;
using static System.Console;
// ReSharper disable NotAccessedVariable
// ReSharper disable RedundantAssignment

// ReSharper disable once CheckNamespace
namespace Website.paradigms.semantic_tevents.InheritingTaggregates;

#region noises1
interface IAnimalTevent : ITaggregateTevent
{
   interface IBorn : IAnimalTevent, ITaggregateCreatedTevent;
}

interface ICatTevent : IAnimalTevent;
interface IDogTevent : IAnimalTevent;
#endregion
#region noises1wrapped
interface IAnimalTevent<out T> : ITaggregateWrapperTevent<T> where T : IAnimalTevent;
interface ICatTevent<out T> : IAnimalTevent<T> where T : IAnimalTevent;
interface IDogTevent<out T> : IAnimalTevent<T> where T : IAnimalTevent;
#endregion

class Examples
{
   public void Enumerables()
   {
      #region enumerable-type-compatibility
      IEnumerable<object> objects = [new object(), new object()];
      IEnumerable<string> strings = ["1", "2"];
      objects = strings;
      #endregion
   }

   public void Listeners()
   {
      TessageHandlerRegistrarWithDependencyInjectionSupport registrar = ((TessageHandlerRegistrarWithDependencyInjectionSupport)null!).NotNull();

      #region doglistener
      registrar
        .ForTevent<IDogTevent<IAnimalTevent.IBorn>>(born => WriteLine($"Dog Id:{born.Tevent.TaggregateId} was born!"));
      #endregion
      #region catlistener
      registrar
        .ForTevent<ICatTevent<IAnimalTevent.IBorn>>(born => WriteLine($"Cat Id:{born.Tevent.TaggregateId} was born!"));
      #endregion
      #region animallistener
      registrar
        .ForTevent<IAnimalTevent.IBorn>(born => WriteLine($"Animal Id:{born.TaggregateId} was born!"));
      #endregion

      #region wrappedanimallistener
      registrar
        .ForTevent<IAnimalTevent<IAnimalTevent.IBorn>>(
            born => WriteLine($"{born.GetType().Name.Replace("Tevent", "")} Id: {born.Tevent.TaggregateId}, was born!"));
      #endregion
   }
}
