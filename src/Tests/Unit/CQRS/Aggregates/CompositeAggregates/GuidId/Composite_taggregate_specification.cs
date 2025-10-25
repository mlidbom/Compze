using System;
using Compze.Abstractions.Tessaging.Teventive.TeventStore.Public;
using Compze.Abstractions.Tessaging.Teventive.Public;
using Compze.Tests.Unit.CQRS.Taggregates.CompositeTaggregates.GuidId.Domain;
using Compze.Tests.Unit.CQRS.Taggregates.CompositeTaggregates.GuidId.Domain.Tevents;
using Compze.Tests.Unit.CQRS.Taggregates.CompositeTaggregates.GuidId.QueryModels;
using Compze.Utilities.SystemCE.ReactiveCE;
using Compze.Utilities.Testing.XUnit.BDD;
using FluentAssertions;

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
      readonly Guid _taggregateId;

      public After_constructing_root_taggregate_with_name_root_and_slaving_a_tuery_model_to_the_taggregates_tevents()
      {
         _taggregateId = Guid.NewGuid();
         _taggregate = new CompositeTaggregate("root", _taggregateId);
         _queryModel = new RootQueryModel();
         ITeventStored<CompositeTaggregateTevent.ICompositeTaggregateTevent> teventStored = _taggregate;
         teventStored.TeventStream.Subscribe(_queryModel.ApplyTevent);
         teventStored.Commit(_queryModel.LoadFromHistory);
      }

      [XF] public void Taggregate_name_is_root() => _taggregate.Name.Should().Be("root");
      [XF] public void Tuery_model_name_is_root() => _queryModel.Name.Should().Be("root");
      [XF] public void Taggregate_id_is_the_supplied_id() => _taggregate.Id.Should().Be(_taggregateId);
      [XF] public void QueryModel_id_is_the_supplied_id() => _queryModel.Id.Should().Be(_taggregateId);

      [XF] public void Taggregate_Component_Component_tests()
      {
         _taggregate.Component.CComponent.Name.Should().BeNullOrEmpty();
         _queryModel.Component.CComponent.Name.Should().BeNullOrEmpty();
         _taggregate.Component.CComponent.Rename("newName");
         _taggregate.Component.CComponent.Name.Should().Be("newName");
         _queryModel.Component.CComponent.Name.Should().Be("newName");
      }
   }
}
