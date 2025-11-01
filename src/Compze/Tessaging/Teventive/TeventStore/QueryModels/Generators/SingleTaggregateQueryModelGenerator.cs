using System.Linq;
using Compze.Core.Public;
using Compze.Core.Tessaging.Teventive.Public;
using Compze.Core.Tessaging.Teventive.Public.Taggregates.Tevents.Public;
using Compze.Core.Tessaging.Teventive.TeventStore.Public;
using Compze.Core.Tessaging.Teventive.TeventStore.QueryModels;
using Compze.Core.Tessaging.Teventive.TeventStore.QueryModels.Generators.Public;
using Compze.Utilities.Functional;
using Compze.Utilities.SystemCE.LinqCE;
using Compze.Utilities.SystemCE.ReflectionCE;
using JetBrains.Annotations;

namespace Compze.Tessaging.Teventive.TeventStore.QueryModels.Generators;

[UsedImplicitly(ImplicitUseKindFlags.InstantiatedWithFixedConstructorSignature)]
public abstract class SingleTaggregateQueryModelGenerator<TImplementer, TViewModel, TTevent, TSession>
   : IQueryModelGenerator<TViewModel>,
     IVersioningQueryModelGenerator<TViewModel>
   where TImplementer : SingleTaggregateQueryModelGenerator<TImplementer, TViewModel, TTevent, TSession>
   where TSession : ITeventStoreReader
   where TTevent : class, ITaggregateTevent
   where TViewModel : class, ISingleTaggregateQueryModel
{
   readonly IMutableTeventDispatcher<TTevent> _teventDispatcher = IMutableTeventDispatcher<TTevent>.New();
   readonly TSession _session;
   protected TViewModel? Model { get; private set; }

   protected SingleTaggregateQueryModelGenerator(TSession session)
   {
      _session = session;
      _teventDispatcher.Register()
                      .ForGenericTevent<ITaggregateCreatedTevent>(e => Model!.SetId(e.TaggregateId))
                      .ForGenericTevent<ITaggregateDeletedTevent>(_ => Model = null);
   }

   ///<summary>Registers handlers for the incoming tevents. All matching handlers will be called in the order they were registered.</summary>
   protected ITeventHandlerRegistrar<TTevent> RegisterHandlers() => _teventDispatcher.Register();

   public Option<TViewModel> TryGenerate(EntityId id) => TryGenerate(id, int.MaxValue);

   public Option<TViewModel> TryGenerate(EntityId id, int version)
   {
      //todo: this conversion is iffy
      var history = _session.GetHistory(new TaggregateId(id.Value)).Take(version).Cast<TTevent>().ToList();
      if (history.None())
      {
         return Option.None<TViewModel>();
      }
      var queryModel = Constructor.For<TViewModel>.DefaultConstructor.Instance();
      Model = queryModel;
      history.ForEach(_teventDispatcher.Dispatch);
      var result = Model;//Yes it does make sense. Look at the registered handler for ITaggregateDeletedTevent
      Model = null;
      return Option.Some(result);
   }
}