using Compze.Abstractions.Public;
using Compze.Abstractions.Tessaging.Public;
using Compze.Internals.SystemCE.LinqCE;
using Compze.Internals.SystemCE.ReflectionCE;
using Compze.Teventive.Taggregates.Tevents.Public;
using Compze.Teventive.TeventStore.Abstractions.Public;
using Compze.Teventive.TeventStore.Abstractions.QueryModels;
using Compze.Teventive.TeventStore.Abstractions.QueryModels.Generators.Public;
using JetBrains.Annotations;

namespace Compze.Teventive.TeventStore.QueryModels.Generators;

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
   protected ITeventSubscriber<TTevent> RegisterHandlers() => _teventDispatcher.Register();

   public TViewModel? TryGenerate(EntityId id) => TryGenerate(id, int.MaxValue);

   public TViewModel? TryGenerate(EntityId id, int version)
   {
      //todo:review: this conversion is iffy
      var history = _session.GetHistory(new TaggregateId(id.Value)).Take(version).Cast<IPublisherTevent<TTevent>>().ToList();
      if (history.None())
      {
         return null;
      }
      var queryModel = Constructor.For<TViewModel>.DefaultConstructor.Instance();
      Model = queryModel;
      history.ForEach(_teventDispatcher.Dispatch);
      var result = Model;//Yes it does make sense. Look at the registered handler for ITaggregateDeletedTevent
      Model = null;
      return result;
   }
}
