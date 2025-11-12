using Compze.Core.Tessaging.Teventive.Internal.Implementation;
using Compze.Core.Tessaging.Teventive.Public.Taggregates.Tevents.Public;

namespace Compze.Core.Tessaging.Teventive.Public.Taggregates.BaseClasses.Public;

public abstract class TeventiveComponent<TParent,
                                        TParentTevent,
                                        TParentTeventImplementation,
                                        TComponent,
                                        TComponentTevent,
                                        TComponentTeventImplementation>
    : ITeventiveInternals<TComponentTevent, TComponentTeventImplementation>
    where TParent : ITeventiveInternals<TParentTevent, TParentTeventImplementation>
    where TParentTevent : class, ITaggregateTevent
    where TParentTeventImplementation : TaggregateTevent, TParentTevent
    where TComponentTevent : class, TParentTevent
    where TComponentTeventImplementation : TParentTeventImplementation, TComponentTevent
    where TComponent : TeventiveComponent<TParent, TParentTevent, TParentTeventImplementation, TComponent, TComponentTevent, TComponentTeventImplementation>
{
    static TeventiveComponent() => TaggregateTypeValidator<TComponent, TComponentTeventImplementation, TComponentTevent>.AssertStaticStructureIsValid();

    readonly IMutableTeventDispatcher<TComponentTevent> _teventAppliersTeventDispatcher = IMutableTeventDispatcher<TComponentTevent>.New();

    TParent _parent;

#pragma warning disable CA1033 //These method should NOT clutter the public interface of this class they are unsafe.
    void ITeventiveInternals<TComponentTevent, TComponentTeventImplementation>.ApplyTeventInternal(TComponentTevent tevent) => ApplyTevent(tevent);
    void ITeventiveInternals<TComponentTevent, TComponentTeventImplementation>.PublishInternal(TComponentTeventImplementation tevent) => Publish(tevent);
    ITeventHandlerRegistrar<TComponentTevent> ITeventiveInternals<TComponentTevent, TComponentTeventImplementation>.RegisterTeventAppliersInternal() => RegisterTeventAppliers();
#pragma warning restore CA1033 //These method should NOT clutter the public interface of this class they are unsafe.

   void ApplyTevent(TComponentTevent tevent) => _teventAppliersTeventDispatcher.Dispatch(tevent);

    protected TeventiveComponent(TParent parent, bool registerTeventAppliers = true)
    {
        _parent = parent;
        if(registerTeventAppliers)
        {
#pragma warning disable CS0618 // This is just the type of infrastructure code the method is for
            parent.RegisterTeventAppliersInternal()
#pragma warning restore CS0618
                  .For<TComponentTevent>(ApplyTevent);
        }
    }

#pragma warning disable CS0618 // This is just the type of infrastructure code the method is for
    protected virtual void Publish(TComponentTeventImplementation tevent) => _parent.PublishInternal(tevent);
#pragma warning restore CS0618

    public abstract class Component<TEcComponent,
                                    TEcComponentTeventImplementation,
                                    TEcComponentTevent> :
        TeventiveComponent<TComponent,
            TComponentTevent,
            TComponentTeventImplementation,
            TEcComponent,
            TEcComponentTevent,
            TEcComponentTeventImplementation>
        where TEcComponentTevent : class, TComponentTevent
        where TEcComponentTeventImplementation : TComponentTeventImplementation, TEcComponentTevent
        where TEcComponent : Component<TEcComponent, TEcComponentTeventImplementation, TEcComponentTevent>
    {
        protected Component(TComponent parent) : base(parent) {}
    }

    protected ITeventHandlerRegistrar<TComponentTevent> RegisterTeventAppliers() => _teventAppliersTeventDispatcher.Register();

    public abstract class Entity<TEntity,
                                 TEntityId,
                                 TEntityTevent,
                                 TEntityTeventImplementation,
                                 TEntityCreatedTevent,
                                 TEntityTeventIdGetterSetter> :
        Tentity<
            TComponent,
            TComponentTevent,
            TComponentTeventImplementation,
            TEntity,
            TEntityId,
            TEntityTeventImplementation,
            TEntityTevent,
            TEntityCreatedTevent,
            TEntityTeventIdGetterSetter>
        where TEntityId : struct
        where TEntityTevent : class, TComponentTevent
        where TEntityTeventImplementation : TComponentTeventImplementation, TEntityTevent
        where TEntityCreatedTevent : TEntityTevent
        where TEntity : Entity<TEntity, TEntityId, TEntityTevent, TEntityTeventImplementation, TEntityCreatedTevent, TEntityTeventIdGetterSetter>
        where TEntityTeventIdGetterSetter : IGetSetTaggregateEntityTeventEntityId<TEntityId, TEntityTeventImplementation, TEntityTevent>
    {
        protected Entity(TComponent taggregate) : base(taggregate) {}
    }

    public abstract class RemovableEntity<TEntity,
                                          TEntityId,
                                          TEntityTevent,
                                          TEntityTeventImplementation,
                                          TEntityCreatedTevent,
                                          TEntityRemovedTevent,
                                          TEntityTeventIdGetterSetter> :
        TeventiveRemovableEntity<
            TComponent,
            TComponentTevent,
            TComponentTeventImplementation,
            TEntity,
            TEntityId,
            TEntityTevent,
            TEntityTeventImplementation,
            TEntityCreatedTevent,
            TEntityRemovedTevent,
            TEntityTeventIdGetterSetter>
        where TEntityId : struct
        where TEntityTevent : class, TComponentTevent
        where TEntityTeventImplementation : TComponentTeventImplementation, TEntityTevent
        where TEntityCreatedTevent : TEntityTevent
        where TEntityRemovedTevent : TEntityTevent
        where TEntity : RemovableEntity<TEntity, TEntityId, TEntityTevent, TEntityTeventImplementation, TEntityCreatedTevent, TEntityRemovedTevent, TEntityTeventIdGetterSetter>
        where TEntityTeventIdGetterSetter : IGetSetTaggregateEntityTeventEntityId<TEntityId, TEntityTeventImplementation, TEntityTevent>
    {
        protected RemovableEntity(TComponent taggregate) : base(taggregate) {}
    }
}
