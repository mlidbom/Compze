using Compze.Teventive._private.Implementation;
using Compze.Teventive.Taggregates.BaseClasses._private;
using Compze.Teventive.Taggregates.Tevents;

namespace Compze.Teventive.Taggregates.BaseClasses;

public abstract class TeventiveComponent<TParent,
                                        TParentTevent,
                                        TParentTeventImplementation,
                                        TComponent,
                                        TComponentTevent,
                                        TComponentTeventImplementation>
    : TeventApplyingComponent<TComponent, TComponentTevent>, ITeventiveInternals<TComponentTevent, TComponentTeventImplementation>, ITeventiveInATaggregate
    where TParent : ITeventiveInternals<TParentTevent, TParentTeventImplementation>
    where TParentTevent : class, ITaggregateTevent
    where TParentTeventImplementation : TaggregateTevent, TParentTevent
    where TComponentTevent : class, TParentTevent
    where TComponentTeventImplementation : TParentTeventImplementation, TComponentTevent
    where TComponent : TeventiveComponent<TParent, TParentTevent, TParentTeventImplementation, TComponent, TComponentTevent, TComponentTeventImplementation>
{
    static TeventiveComponent() => TaggregateTypeValidator<TComponent, TComponentTeventImplementation, TComponentTevent>.AssertStaticStructureIsValid();

    readonly TParent _parent;

#pragma warning disable CA1033 //These method should NOT clutter the public interface of this class they are unsafe.
    void ITeventiveInternals<TComponentTevent, TComponentTeventImplementation>.PublishInternal(TComponentTeventImplementation tevent) => Publish(tevent);
    TeventRouting ITeventiveInternals<TComponentTevent, TComponentTeventImplementation>.TeventAppliersInternal => TeventAppliers;
#pragma warning restore CA1033 //These method should NOT clutter the public interface of this class they are unsafe.

    bool ITeventiveInATaggregate.ItsTaggregateIsBeingRestored => ((ITeventiveInATaggregate)_parent).ItsTaggregateIsBeingRestored;

    ///<summary>A component, or an entity, of <paramref name="parent"/>, whose routing is asserted to route this component's tevents to a part.</summary>
#pragma warning disable CS0618 // This is just the type of infrastructure code the member is for
    protected TeventiveComponent(TParent parent) : base(parent, parent.TeventAppliersInternal) => _parent = parent;
#pragma warning restore CS0618

#pragma warning disable CS0618 // This is just the type of infrastructure code the method is for
    protected virtual void Publish(TComponentTeventImplementation tevent) => _parent.PublishInternal(tevent);
#pragma warning restore CS0618

    public abstract class Component<TEcComponent,
                                    TEcComponentTeventImplementation,
                                    TEcComponentTevent>(TComponent parent) :
       TeventiveComponent<TComponent,
          TComponentTevent,
          TComponentTeventImplementation,
          TEcComponent,
          TEcComponentTevent,
          TEcComponentTeventImplementation>(parent)
       where TEcComponentTevent : class, TComponentTevent
       where TEcComponentTeventImplementation : TComponentTeventImplementation, TEcComponentTevent
       where TEcComponent : Component<TEcComponent, TEcComponentTeventImplementation, TEcComponentTevent>;

    public abstract class Entity<TEntity,
                                 TEntityId,
                                 TEntityTevent,
                                 TEntityTeventImplementation,
                                 TEntityCreatedTevent,
                                 TEntityTeventIdGetterSetter>(TComponent taggregate) :
       Tentity<
          TComponent,
          TComponentTevent,
          TComponentTeventImplementation,
          TEntity,
          TEntityId,
          TEntityTeventImplementation,
          TEntityTevent,
          TEntityCreatedTevent,
          TEntityTeventIdGetterSetter>(taggregate)
       where TEntityId : struct
       where TEntityTevent : class, TComponentTevent
       where TEntityTeventImplementation : TComponentTeventImplementation, TEntityTevent
       where TEntityCreatedTevent : TEntityTevent
       where TEntity : Entity<TEntity, TEntityId, TEntityTevent, TEntityTeventImplementation, TEntityCreatedTevent, TEntityTeventIdGetterSetter>
       where TEntityTeventIdGetterSetter : IGetSetTaggregateEntityTeventEntityId<TEntityId, TEntityTeventImplementation, TEntityTevent>;

    public abstract class RemovableEntity<TEntity,
                                          TEntityId,
                                          TEntityTevent,
                                          TEntityTeventImplementation,
                                          TEntityCreatedTevent,
                                          TEntityRemovedTevent,
                                          TEntityTeventIdGetterSetter>(TComponent taggregate) :
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
          TEntityTeventIdGetterSetter>(taggregate)
       where TEntityId : struct
       where TEntityTevent : class, TComponentTevent
       where TEntityTeventImplementation : TComponentTeventImplementation, TEntityTevent
       where TEntityCreatedTevent : TEntityTevent
       where TEntityRemovedTevent : TEntityTevent
       where TEntity : RemovableEntity<TEntity, TEntityId, TEntityTevent, TEntityTeventImplementation, TEntityCreatedTevent, TEntityRemovedTevent, TEntityTeventIdGetterSetter>
       where TEntityTeventIdGetterSetter : IGetSetTaggregateEntityTeventEntityId<TEntityId, TEntityTeventImplementation, TEntityTevent>;
}
