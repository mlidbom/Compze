using Compze.Core.Public;
using Compze.Core.Tessaging.Teventive.Public;
using Compze.Tests.Unit.CQRS.Taggregates.CompositeTaggregates.GuidId.Domain;
using Compze.Tests.Unit.CQRS.Taggregates.CompositeTaggregates.GuidId.Domain.Tevents;
using Compze.Tests.Unit.CQRS.Taggregates.CompositeTaggregates.GuidId.QueryModels;
using Compze.Utilities.SystemCE.ReactiveCE;
using Compze.Utilities.Testing.Must;
using Compze.Utilities.Testing.XUnit.BDD;

// ReSharper disable InconsistentNaming
// ReSharper disable ImplicitlyCapturedClosure

// ReSharper disable MemberHidesStaticFromOuterClass

#pragma warning disable CA1724 // Type names should not match namespaces

namespace Compze.Tests.Unit.CQRS.Taggregates.CompositeTaggregates.GuidId;

public static partial class Composite_taggregate_specification
{
   public partial class After_constructing_root_taggregate_with_name_root_and_slaving_a_tuery_model_to_the_taggregates_tevents
   {
      readonly CompositeTaggregate _taggregate;
      readonly RootQueryModel _queryModel;
      readonly TaggregateId _taggregateId;

      public After_constructing_root_taggregate_with_name_root_and_slaving_a_tuery_model_to_the_taggregates_tevents()
      {
         _taggregateId = new TaggregateId();
         _taggregate = new CompositeTaggregate("root", _taggregateId);
         _queryModel = new RootQueryModel();
         ITaggregate<ICompositeTaggregateTevent> taggregate = _taggregate;
         taggregate.TeventStream.Subscribe(_queryModel.ApplyTevent);
         taggregate.Commit(_queryModel.LoadFromHistory);
      }

      [XF] public void Taggregate_name_is_root() => _taggregate.Name.Must().Be("root");
      [XF] public void Tuery_model_name_is_root() => _queryModel.Name.Must().Be("root");
      [XF] public void Taggregate_id_is_the_supplied_id() => _taggregate.Id.Must().Be(_taggregateId);
      [XF] public void QueryModel_id_is_the_supplied_id() => _queryModel.Id.Must().Be(_taggregateId);

      [XF] public void Taggregate_Component_Component_tests()
      {
         _taggregate.Component.CComponent.Name.Must().BeNullOrEmpty();
         _queryModel.Component.CComponent.Name.Must().BeNullOrEmpty();
         _taggregate.Component.CComponent.Rename("newName");
         _taggregate.Component.CComponent.Name.Must().Be("newName");
         _queryModel.Component.CComponent.Name.Must().Be("newName");
      }
   }
}
