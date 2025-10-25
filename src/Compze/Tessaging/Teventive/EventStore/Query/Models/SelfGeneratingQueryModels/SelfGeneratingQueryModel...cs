using System;
using System.Collections.Generic;
using Compze.Abstractions.Public;
using Compze.Abstractions.Tessaging.Teventive.TeventStore.Public;
using Compze.Abstractions.Tessaging.Teventive.Public;
using Compze.Utilities.Contracts;
using Compze.Utilities.SystemCE.LinqCE;

namespace Compze.Tessaging.Teventive.TeventStore.Tuery.Models.SelfGeneratingQueryModels;

public partial class SelfGeneratingQueryModel<TQueryModel, TAggregateTevent> : VersionedPersistentEntity<TQueryModel>
   where TQueryModel : SelfGeneratingQueryModel<TQueryModel, TAggregateTevent>
   where TAggregateTevent : class, IAggregateTevent
{
   //Yes empty. Id should be assigned by an action, and it should be obvious that the aggregate in invalid until that happens
   protected SelfGeneratingQueryModel() : base(Guid.Empty) => Assert.Argument.Is(typeof(TAggregateTevent).IsInterface);

   readonly IMutableTeventDispatcher<TAggregateTevent> _teventDispatcher = IMutableTeventDispatcher<TAggregateTevent>.New();

   protected ITeventHandlerRegistrar<TAggregateTevent> RegisterTeventAppliers() => _teventDispatcher.Register();

   public void ApplyTevent(TAggregateTevent theTevent)
   {
      if(theTevent is IAggregateCreatedTevent)
      {
#pragma warning disable 618 //Reviewed OK: This is precisely the type of internal code this is supposed to use this "obsolete" method.
         SetIdBeVerySureYouKnowWhatYouAreDoing(theTevent.AggregateId);
#pragma warning restore 618
      }

      Version = theTevent.AggregateVersion;
      _teventDispatcher.Dispatch(theTevent);
   }

   public bool HandlesTevent(TAggregateTevent @tevent) => _teventDispatcher.Handles(@tevent);

   public void LoadFromHistory(IEnumerable<IAggregateTevent> history)
   {
      Assert.State.Is(Version == 0);
      history.ForEach(theTevent => ApplyTevent((TAggregateTevent)theTevent));
      AssertInvariantsAreMet();
   }

   protected virtual void AssertInvariantsAreMet(){}
}