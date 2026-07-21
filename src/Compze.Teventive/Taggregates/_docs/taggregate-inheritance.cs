using Compze.Tessaging.TessageBus;
using Compze.Teventive.Taggregates.Tevents;
using static System.Console;

#pragma warning disable // Documentation example code: deliberately illustrative fragments (empty marker interfaces, never-instantiated examples), not production code.
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
interface IAnimalTevent<out T> : ITaggregateTevent<T> where T : IAnimalTevent;
interface ICatTevent<out T> : IAnimalTevent<T> where T : IAnimalTevent;
interface IDogTevent<out T> : IAnimalTevent<T> where T : IAnimalTevent;
#endregion

class Examples
{
   public void Enumerables()
   {
      #region enumerable-type-compatibility
      IEnumerable<object> objects = [new(), new()];
      IEnumerable<string> strings = ["1", "2"];
      objects = strings;
      #endregion
   }

   public void Listeners()
   {
      TessageBusHandlerRegistrar registrar = null!;

      #region doglistener
      registrar
        .ForTevent<IDogTevent<IAnimalTevent.IBorn>>(born => { WriteLine($"Dog Id:{born.Tevent.TaggregateId} was born!"); return Task.CompletedTask; });
      #endregion
      #region catlistener
      registrar
        .ForTevent<ICatTevent<IAnimalTevent.IBorn>>(born => { WriteLine($"Cat Id:{born.Tevent.TaggregateId} was born!"); return Task.CompletedTask; });
      #endregion
      #region animallistener
      registrar
        .ForTevent<IAnimalTevent.IBorn>(born => { WriteLine($"Animal Id:{born.TaggregateId} was born!"); return Task.CompletedTask; });
      #endregion

      #region wrappedanimallistener
      registrar
        .ForTevent<IAnimalTevent<IAnimalTevent.IBorn>>(
            born =>
            {
               WriteLine($"{born.GetType().Name.Replace("Tevent", "")} Id: {born.Tevent.TaggregateId}, was born!");
               return Task.CompletedTask;
            });
      #endregion
   }
}
