using System;
using Compze.Abstractions.Tessaging.Teventive.Internal.Implementation;
using Compze.Abstractions.Tessaging.Teventive.Public.Taggregates.Tevents.Public;
using Compze.Utilities.Contracts;
using Compze.Utilities.SystemCE.ReflectionCE;
using JetBrains.Annotations;

namespace Compze.Abstractions.Tessaging.Teventive.Public.Taggregates.BaseClasses.Public;

[UsedImplicitly(ImplicitUseKindFlags.InstantiatedWithFixedConstructorSignature)]
public abstract class TeventiveEntity<TParent,
                                     TParentTevent,
                                     TParentTeventImplementation,
                                     TEntity,
                                     TEntityId,
                                     TEntityTeventImplementation,
                                     TEntityTevent,
                                     TEntityCreatedTevent,
                                     TEntityTeventIdGetterSetter>
    : TeventiveComponent<TParent, TParentTevent, TParentTeventImplementation, TEntity, TEntityTevent, TEntityTeventImplementation>
    where TParent : ITeventiveInternals<TParentTevent, TParentTeventImplementation>
    where TParentTevent : class, ITaggregateTevent
    where TEntityId : struct
    where TEntityTevent : class, TParentTevent
    where TParentTeventImplementation : TaggregateTevent, TParentTevent
    where TEntityTeventImplementation : TParentTeventImplementation, TEntityTevent
    where TEntityCreatedTevent : TEntityTevent
    where TEntity : TeventiveEntity<TParent, TParentTevent, TParentTeventImplementation, TEntity, TEntityId, TEntityTeventImplementation, TEntityTevent, TEntityCreatedTevent, TEntityTeventIdGetterSetter>
    where TEntityTeventIdGetterSetter : IGetSetTaggregateEntityTeventEntityId<TEntityId, TEntityTeventImplementation, TEntityTevent>
{
    static TeventiveEntity() => TaggregateTypeValidator<TEntity, TEntityTeventImplementation, TEntityTevent>.AssertStaticStructureIsValid();

    static readonly TEntityTeventIdGetterSetter IdGetterSetter = Constructor.For<TEntityTeventIdGetterSetter>.DefaultConstructor.Instance();

    TEntityId _id;
    public TEntityId Id => Assert.Result.ReturnNotDefault(_id);

    protected TeventiveEntity(TParent taggregate) : base(taggregate, false)
    {
        RegisterTeventAppliers()
           .For<TEntityCreatedTevent>(e => _id = IdGetterSetter.GetId(e));
    }

    protected override void Publish(TEntityTeventImplementation @tevent)
    {
        var id = IdGetterSetter.GetId(@tevent);
        if(Equals(id, default(TEntityId)))
        {
            IdGetterSetter.SetEntityId(@tevent, Id);
        } else if(!Equals(id, Id))
        {
            throw new Exception($"Attempted to raise tevent with EntityId: {id} from within entity with EntityId: {Id}");
        }

        base.Publish(@tevent);
    }

    // ReSharper disable once UnusedMember.Global todo: write tests.
    public static CollectionManager CreateSelfManagingCollection(TParent parent) => new(parent);

    public class CollectionManager : IEntityCollectionManager<TEntity, TEntityId, TEntityTevent, TEntityTeventImplementation, TEntityCreatedTevent>
    {
        protected static readonly TEntityTeventIdGetterSetter IdGetter = Constructor.For<TEntityTeventIdGetterSetter>.DefaultConstructor.Instance();

        protected EntityCollection<TEntity, TEntityId> ManagedEntities { get; }

        TParent _parent;

        internal CollectionManager(TParent parent)
        {
            ManagedEntities = [];
            _parent = parent;
#pragma warning disable CS0618 // This is just the type of infrastructure code the methods are for
            parent.RegisterTeventAppliersInternal()
               .For<TEntityCreatedTevent>(e =>
                {
                    var entity = Constructor.For<TEntity>.WithArguments<TParent>.Instance(parent);
                    ManagedEntities.Add(entity, IdGetter.GetId(e));
                })
               .For<TEntityTevent>(e => GetEntityAsTeventiveInternals(e).ApplyTeventInternal(e));
#pragma warning restore CS0618
        }

        ITeventiveInternals<TEntityTevent, TEntityTeventImplementation> GetEntityAsTeventiveInternals(TEntityTevent e) => ManagedEntities[IdGetter.GetId(e)];

        public IReadOnlyEntityCollection<TEntity, TEntityId> Entities => ManagedEntities;

        public TEntity AddByPublishing<TCreationTevent>(TCreationTevent creationTevent) where TCreationTevent : TEntityTeventImplementation, TEntityCreatedTevent
        {
#pragma warning disable CS0618 // This is just the type of infrastructure code the methods are for
            _parent.PublishInternal(creationTevent);
#pragma warning restore CS0618
            var result = ManagedEntities.InCreationOrder[^1];
            return result;
        }
    }
}
