using System;
using Compze.Core.Tessaging.Teventive.Internal.Implementation;
using Compze.Core.Tessaging.Teventive.Public.Taggregates.Tevents.Public;
using Compze.Contracts;
using Compze.Utilities.SystemCE.ReflectionCE;
using JetBrains.Annotations;

namespace Compze.Core.Tessaging.Teventive.Public.Taggregates.BaseClasses.Public;

[UsedImplicitly(ImplicitUseKindFlags.InstantiatedWithFixedConstructorSignature)]
public abstract class Tentity<TParent,
                              TParentTevent,
                              TParentTeventImplementation,
                              TTentity,
                              TTentityId,
                              TTentityTeventImplementation,
                              TTentityTevent,
                              TTentityCreatedTevent,
                              TTentityTeventIdGetterSetter>
   : TeventiveComponent<TParent, TParentTevent, TParentTeventImplementation, TTentity, TTentityTevent, TTentityTeventImplementation>
   where TParent : ITeventiveInternals<TParentTevent, TParentTeventImplementation>
   where TParentTevent : class, ITaggregateTevent
   where TTentityId : struct
   where TTentityTevent : class, TParentTevent
   where TParentTeventImplementation : TaggregateTevent, TParentTevent
   where TTentityTeventImplementation : TParentTeventImplementation, TTentityTevent
   where TTentityCreatedTevent : TTentityTevent
   where TTentity : Tentity<TParent, TParentTevent, TParentTeventImplementation, TTentity, TTentityId, TTentityTeventImplementation, TTentityTevent, TTentityCreatedTevent, TTentityTeventIdGetterSetter>
   where TTentityTeventIdGetterSetter : IGetSetTaggregateEntityTeventEntityId<TTentityId, TTentityTeventImplementation, TTentityTevent>
{
   static Tentity() => TaggregateTypeValidator<TTentity, TTentityTeventImplementation, TTentityTevent>.AssertStaticStructureIsValid();

   static readonly TTentityTeventIdGetterSetter IdGetterSetter = Constructor.For<TTentityTeventIdGetterSetter>.DefaultConstructor.Instance();

   TTentityId _id;
   public TTentityId Id => _id._assert().NotDefault();

   protected Tentity(TParent taggregate) : base(taggregate, false)
   {
      RegisterTeventAppliers()
        .For<TTentityCreatedTevent>(e => _id = IdGetterSetter.GetId(e));
   }

   protected override void Publish(TTentityTeventImplementation tevent)
   {
      var id = IdGetterSetter.GetId(tevent);
      if(Equals(id, default(TTentityId)))
      {
         IdGetterSetter.SetEntityId(tevent, Id);
      } else if(!Equals(id, Id))
      {
         throw new Exception($"Attempted to raise tevent with EntityId: {id} from within entity with EntityId: {Id}");
      }

      base.Publish(tevent);
   }

   // ReSharper disable once UnusedMember.Global todo: write tests.
   public static CollectionManager CreateSelfManagingCollection(TParent parent) => new(parent);

   public class CollectionManager : IEntityCollectionManager<TTentity, TTentityId, TTentityTevent, TTentityTeventImplementation, TTentityCreatedTevent>
   {
      protected static readonly TTentityTeventIdGetterSetter IdGetter = Constructor.For<TTentityTeventIdGetterSetter>.DefaultConstructor.Instance();

      protected EntityCollection<TTentity, TTentityId> ManagedEntities { get; }

      TParent _parent;

      public CollectionManager(TParent parent)
      {
         ManagedEntities = [];
         _parent = parent;
#pragma warning disable CS0618 // This is just the type of infrastructure code the methods are for
         parent.RegisterTeventAppliersInternal()
               .For<TTentityCreatedTevent>(e =>
                {
                   var entity = Constructor.For<TTentity>.WithArguments<TParent>.Instance(parent);
                   ManagedEntities.Add(entity, IdGetter.GetId(e));
                })
               .For<TTentityTevent>(e => GetEntityAsTeventiveInternals(e).ApplyTeventInternal(e));
#pragma warning restore CS0618
      }

      ITeventiveInternals<TTentityTevent, TTentityTeventImplementation> GetEntityAsTeventiveInternals(TTentityTevent e) => ManagedEntities[IdGetter.GetId(e)];

      public IReadOnlyEntityCollection<TTentity, TTentityId> Entities => ManagedEntities;

      public TTentity AddByPublishing<TCreationTevent>(TCreationTevent creationTevent) where TCreationTevent : TTentityTeventImplementation, TTentityCreatedTevent
      {
#pragma warning disable CS0618 // This is just the type of infrastructure code the methods are for
         _parent.PublishInternal(creationTevent);
#pragma warning restore CS0618
         var result = ManagedEntities.InCreationOrder[^1];
         return result;
      }
   }
}
