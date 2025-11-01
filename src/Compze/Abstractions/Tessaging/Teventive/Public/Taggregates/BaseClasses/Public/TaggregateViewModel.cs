using Compze.Core.Public;

namespace Compze.Core.Tessaging.Teventive.Public.Taggregates.BaseClasses.Public;

public abstract class TaggregateViewModel<TViewModel> : Entity<TViewModel> where TViewModel : TaggregateViewModel<TViewModel>
{
   protected TaggregateViewModel(){}
   protected TaggregateViewModel(TaggregateId id) : base(id){}
   public override TaggregateId Id => (TaggregateId)base.Id;
}
