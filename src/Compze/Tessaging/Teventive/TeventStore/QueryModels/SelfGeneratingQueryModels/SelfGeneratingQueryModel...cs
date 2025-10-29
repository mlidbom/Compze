using System;
using System.Collections.Generic;
using Compze.Core.Public;
using Compze.Core.Tessaging.Teventive.Public;
using Compze.Core.Tessaging.Teventive.Public.Taggregates.Tevents.Public;
using Compze.Utilities.Contracts;
using Compze.Utilities.SystemCE.LinqCE;

namespace Compze.Tessaging.Teventive.TeventStore.QueryModels.SelfGeneratingQueryModels;

public partial class SelfGeneratingQueryModel<TQueryModel, TTaggregateTevent> : VersionedPersistentEntity<TQueryModel>
   where TQueryModel : SelfGeneratingQueryModel<TQueryModel, TTaggregateTevent>
   where TTaggregateTevent : class, ITaggregateTevent
{
   //Yes empty. Id should be assigned by an action, and it should be obvious that the taggregate in invalid until that happens
   protected SelfGeneratingQueryModel() : base(Guid.Empty) => Assert.Argument.Is(typeof(TTaggregateTevent).IsInterface);

   readonly IMutableTeventDispatcher<TTaggregateTevent> _teventDispatcher = IMutableTeventDispatcher<TTaggregateTevent>.New();

   protected ITeventHandlerRegistrar<TTaggregateTevent> RegisterTeventAppliers() => _teventDispatcher.Register();

   public void ApplyTevent(TTaggregateTevent theTevent)
   {
      if(theTevent is ITaggregateCreatedTevent)
      {
#pragma warning disable 618 //Reviewed OK: This is precisely the type of internal code this is supposed to use this "obsolete" method.
         SetIdBeVerySureYouKnowWhatYouAreDoing(theTevent.TaggregateId);
#pragma warning restore 618
      }

      Version = theTevent.TaggregateVersion;
      _teventDispatcher.Dispatch(theTevent);
   }

   public bool HandlesTevent(TTaggregateTevent tevent) => _teventDispatcher.Handles(tevent);

   public void LoadFromHistory(IEnumerable<ITaggregateTevent> history)
   {
      Assert.State.Is(Version == 0);
      history.ForEach(theTevent => ApplyTevent((TTaggregateTevent)theTevent));
      AssertInvariantsAreMet();
   }

   protected virtual void AssertInvariantsAreMet(){}
}