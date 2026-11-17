using Compze.Teventive._private.Implementation;
using Compze.Teventive.Taggregates.Tevents;

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

    ///<summary>Declares the tevent appliers of <typeparamref name="TEntity"/>, as <see cref="Tentity{TParent,TParentTevent,TParentTeventImplementation,TTentity,TTentityId,TTentityTeventImplementation,TTentityTevent,TTentityCreatedTevent,TTentityTeventIdGetterSetter}.DeclareAppliers"/> does,<br/>
    /// letting its <typeparamref name="TEntityRemovedTevent"/> reach no applier: its collection removes it once the tevent has reached any it has.</summary>
    protected new static TeventRouting<TEntity, TEntityTevent> DeclareAppliers(Action<TeventRouting<TEntity, TEntityTevent>.Declaration> declare) =>
       DeclareTentityAppliers(it =>
       {
          it.IgnoreUnhandled<TEntityRemovedTevent>();
          declare(it);
       });

    public new static ICollectionManager CreateSelfManagingCollection(TParent parent)
        => new CollectionManager(parent);

    new class CollectionManager : Tentity<TParent, TParentTevent, TParentTeventImplementation, TEntity, TEntityId, TEntityTeventImplementation, TEntityTevent, TEntityCreatedTevent, TEntityTeventIdGetterSetter>.CollectionManager
    {
        public CollectionManager(TParent parent): base(parent)
        {
#pragma warning disable 618 //Reviewed OK: This is just the type of infrastructure code the members are for.
            if(parent.DeclaresTeventAppliersInternal) return;

            parent.RegisterTeventAppliersInternal().For<TEntityRemovedTevent>(e =>
            {
#pragma warning restore 618
                var id = IdGetter.GetId(e);
                ManagedEntities.Remove(id);
            });
        }

        internal override bool Removes(TEntityTevent tevent) => tevent is TEntityRemovedTevent;
    }
}
