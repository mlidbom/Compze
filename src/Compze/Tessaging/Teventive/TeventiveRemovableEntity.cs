using Compze.Abstractions.Tessaging.Teventive.TeventStore.Public;
using Compze.Abstractions.Tessaging.Teventive.Public;

namespace Compze.Tessaging.Teventive;

public abstract class TeventiveRemovableEntity<TParent,
                                              TParentTevent,
                                              TParentTeventImplementation,
                                              TEntity,
                                              TEntityId,
                                              TEntityTevent,
                                              TEntityTeventImplementation,
                                              TEntityCreatedTevent,
                                              TEntityRemovedTevent,
                                              TEntityTeventIdGetterSetter>
    : TeventiveEntity<TParent, TParentTevent, TParentTeventImplementation, TEntity, TEntityId, TEntityTeventImplementation, TEntityTevent, TEntityCreatedTevent, TEntityTeventIdGetterSetter>
    where TParent : ITeventiveInternals<TParentTevent, TParentTeventImplementation>
    where TParentTevent : class, IAggregateTevent
    where TEntityId : struct
    where TEntityTevent : class, TParentTevent
    where TParentTeventImplementation : AggregateTevent, TParentTevent
    where TEntityTeventImplementation : TParentTeventImplementation, TEntityTevent
    where TEntityCreatedTevent : TEntityTevent
    where TEntityRemovedTevent : TEntityTevent
    where TEntity : TeventiveRemovableEntity<TParent, TParentTevent, TParentTeventImplementation, TEntity, TEntityId, TEntityTevent, TEntityTeventImplementation, TEntityCreatedTevent, TEntityRemovedTevent, TEntityTeventIdGetterSetter>
    where TEntityTeventIdGetterSetter : IGetSetAggregateEntityTeventEntityId<TEntityId, TEntityTeventImplementation, TEntityTevent>
{
    static TeventiveRemovableEntity() => AggregateTypeValidator<TEntity, TEntityTeventImplementation, TEntityTevent>.AssertStaticStructureIsValid();

    protected TeventiveRemovableEntity(TParent aggregate) : base(aggregate)
    {
        RegisterTeventAppliers()
           .IgnoreUnhandled<TEntityRemovedTevent>();
    }

    public new static CollectionManager CreateSelfManagingCollection(TParent parent)
        => new(parent);

    public new class CollectionManager : TeventiveEntity<TParent, TParentTevent, TParentTeventImplementation, TEntity, TEntityId, TEntityTeventImplementation, TEntityTevent, TEntityCreatedTevent, TEntityTeventIdGetterSetter>.CollectionManager
    {
        internal CollectionManager(TParent parent): base(parent)
        {
#pragma warning disable 618 //Reviewed OK: This test class is allowed to use these "obsolete" methods.
            parent.RegisterTeventAppliersInternal().For<TEntityRemovedTevent>(e =>
            {
#pragma warning restore 618
                var id = IdGetter.GetId(e);
                ManagedEntities.Remove(id);
            });
        }
    }
}
