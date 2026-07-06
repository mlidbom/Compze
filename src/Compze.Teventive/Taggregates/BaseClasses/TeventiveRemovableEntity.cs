using Compze.Teventive.Internal.Implementation;
using Compze.Teventive.Taggregates.Tevents.Public;

namespace Compze.Teventive.Taggregates.BaseClasses;

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
    : Tentity<TParent, TParentTevent, TParentTeventImplementation, TEntity, TEntityId, TEntityTeventImplementation, TEntityTevent, TEntityCreatedTevent, TEntityTeventIdGetterSetter>
    where TParent : ITeventiveInternals<TParentTevent, TParentTeventImplementation>
    where TParentTevent : class, ITaggregateTevent
    where TEntityId : struct
    where TEntityTevent : class, TParentTevent
    where TParentTeventImplementation : TaggregateTevent, TParentTevent
    where TEntityTeventImplementation : TParentTeventImplementation, TEntityTevent
    where TEntityCreatedTevent : TEntityTevent
    where TEntityRemovedTevent : TEntityTevent
    where TEntity : TeventiveRemovableEntity<TParent, TParentTevent, TParentTeventImplementation, TEntity, TEntityId, TEntityTevent, TEntityTeventImplementation, TEntityCreatedTevent, TEntityRemovedTevent, TEntityTeventIdGetterSetter>
    where TEntityTeventIdGetterSetter : IGetSetTaggregateEntityTeventEntityId<TEntityId, TEntityTeventImplementation, TEntityTevent>
{
    static TeventiveRemovableEntity() => TaggregateTypeValidator<TEntity, TEntityTeventImplementation, TEntityTevent>.AssertStaticStructureIsValid();

    protected TeventiveRemovableEntity(TParent taggregate)
       : base(taggregate, TeventDispatcherConfig.Default.IgnoreUnhandled<TEntityRemovedTevent>()) {}

    public new static ICollectionManager CreateSelfManagingCollection(TParent parent)
        => new CollectionManager(parent);

    new class CollectionManager : Tentity<TParent, TParentTevent, TParentTeventImplementation, TEntity, TEntityId, TEntityTeventImplementation, TEntityTevent, TEntityCreatedTevent, TEntityTeventIdGetterSetter>.CollectionManager
    {
        public CollectionManager(TParent parent): base(parent)
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
