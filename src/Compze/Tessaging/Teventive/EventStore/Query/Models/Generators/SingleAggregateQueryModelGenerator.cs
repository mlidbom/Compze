using System;
using System.Linq;
using Compze.Abstractions.Tessaging.Teventive.TeventStore.Public;
using Compze.Abstractions.Tessaging.Teventive.TeventStore.Tuery.Models.Generators.Public;
using Compze.Abstractions.Tessaging.Teventive.Public;
using Compze.Utilities.Functional;
using Compze.Utilities.SystemCE.LinqCE;
using Compze.Utilities.SystemCE.ReflectionCE;
using JetBrains.Annotations;

namespace Compze.Tessaging.Teventive.TeventStore.Tuery.Models.Generators;

[UsedImplicitly(ImplicitUseKindFlags.InstantiatedWithFixedConstructorSignature)]
public abstract class SingleAggregateQueryModelGenerator<TImplementer, TViewModel, TTevent, TSession>
   : IQueryModelGenerator<TViewModel>,
     IVersioningQueryModelGenerator<TViewModel>
   where TImplementer : SingleAggregateQueryModelGenerator<TImplementer, TViewModel, TTevent, TSession>
   where TSession : ITeventStoreReader
   where TTevent : class, IAggregateTevent
   where TViewModel : class, ISingleAggregateQueryModel
{
   readonly IMutableTeventDispatcher<TTevent> _teventDispatcher = IMutableTeventDispatcher<TTevent>.New();
   readonly TSession _session;
   protected TViewModel? Model { get; private set; }

   protected SingleAggregateQueryModelGenerator(TSession session)
   {
      _session = session;
      _teventDispatcher.Register()
                      .ForGenericTevent<IAggregateCreatedTevent>(e => Model!.SetId(e.AggregateId))
                      .ForGenericTevent<IAggregateDeletedTevent>(_ => Model = null);
   }

   ///<summary>Registers handlers for the incoming tevents. All matching handlers will be called in the order they were registered.</summary>
   protected ITeventHandlerRegistrar<TTevent> RegisterHandlers() => _teventDispatcher.Register();

   public Option<TViewModel> TryGenerate(Guid id) => TryGenerate(id, int.MaxValue);

   public Option<TViewModel> TryGenerate(Guid id, int version)
   {
      var history = _session.GetHistory(id).Take(version).Cast<TTevent>().ToList();
      if (history.None())
      {
         return Option.None<TViewModel>();
      }
      var queryModel = Constructor.For<TViewModel>.DefaultConstructor.Instance();
      Model = queryModel;
      history.ForEach(_teventDispatcher.Dispatch);
      var result = Model;//Yes it does make sense. Look at the registered handler for IAggregateDeletedTevent
      Model = null;
      return Option.Some(result);
   }
}