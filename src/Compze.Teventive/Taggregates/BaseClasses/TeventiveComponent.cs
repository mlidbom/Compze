using Compze.Teventive._private;
using Compze.Tessaging.TessageTypes;
using Compze.Contracts;
using Compze.Teventive._private.Implementation;
using Compze.Teventive.Taggregates.Tevents;

namespace Compze.Teventive.Taggregates.BaseClasses;

public abstract class TeventiveComponent<TParent,
                                        TParentTevent,
                                        TParentTeventImplementation,
                                        TComponent,
                                        TComponentTevent,
                                        TComponentTeventImplementation>
    : ITeventiveInternals<TComponentTevent, TComponentTeventImplementation>, ITeventRoutedMember<TComponentTevent>
    where TParent : ITeventiveInternals<TParentTevent, TParentTeventImplementation>
    where TParentTevent : class, ITaggregateTevent
    where TParentTeventImplementation : TaggregateTevent, TParentTevent
    where TComponentTevent : class, TParentTevent
    where TComponentTeventImplementation : TParentTeventImplementation, TComponentTevent
    where TComponent : TeventiveComponent<TParent, TParentTevent, TParentTeventImplementation, TComponent, TComponentTevent, TComponentTeventImplementation>
{
    static TeventiveComponent() => TaggregateTypeValidator<TComponent, TComponentTeventImplementation, TComponentTevent>.AssertStaticStructureIsValid();

    //wip: taggregate-experiments-opus keeps the per-instance dispatcher for the classes that declare no TeventRouting. Null for those that do.
    readonly IMutableTeventDispatcher<TComponentTevent>? _teventAppliersDispatcher;

    TParent _parent;

#pragma warning disable CA1033 //These method should NOT clutter the public interface of this class they are unsafe.
    void ITeventiveInternals<TComponentTevent, TComponentTeventImplementation>.ApplyTeventInternal(TComponentTevent tevent) => ApplyTevent(tevent);
    void ITeventiveInternals<TComponentTevent, TComponentTeventImplementation>.PublishInternal(TComponentTeventImplementation tevent) => Publish(tevent);
    ITeventSubscriber<TComponentTevent> ITeventiveInternals<TComponentTevent, TComponentTeventImplementation>.RegisterTeventAppliersInternal() => RegisterTeventAppliers();
    bool ITeventiveInternals<TComponentTevent, TComponentTeventImplementation>.DeclaresTeventAppliersInternal => TeventAppliers is not null;
#pragma warning restore CA1033 //These method should NOT clutter the public interface of this class they are unsafe.

   void ApplyTevent(TComponentTevent tevent) => _teventAppliersDispatcher._assert().NotNull().Dispatch(tevent);

    ///<summary>The routing of this component's class to its tevent appliers, when the class declares one: a <see cref="TeventRouting"/> in a static field.<br/>
    /// Its parent's routing then reaches it through <see cref="TeventRouting{TInstance}.Declaration.Component{TComponentTevent}"/>, and it does not register on its parent.<br/>
    /// Null for a class that registers its appliers per instance, through <see cref="RegisterTeventAppliers"/>.</summary>
    protected virtual TeventRouting? TeventAppliers => ConventionTeventRouting.AppliersOf(GetType());

    ///<summary>The routing of this component's class to its tevent handlers, when the class declares one. See <see cref="TeventAppliers"/>.</summary>
    protected virtual TeventRouting? TeventHandlers => ConventionTeventRouting.HandlersOf(GetType());

#pragma warning disable CA1033 //These members should NOT clutter the public interface of this class.
    void ITeventRoutedMember<TComponentTevent>.DispatchInternal(IPublisherTevent<ITevent> wrapped, bool toTeventHandlers) =>
       (toTeventHandlers ? TeventHandlers : TeventAppliers._assert().NotNull())?.Dispatch(this, wrapped);
#pragma warning restore CA1033

    protected TeventiveComponent(TParent parent, bool registerTeventAppliers = true, TeventDispatcherConfig? teventAppliersDispatcherConfig = null)
    {
        _parent = parent;
        if(TeventAppliers is not null) return;

        _teventAppliersDispatcher = IMutableTeventDispatcher<TComponentTevent>.New(teventAppliersDispatcherConfig);
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

    protected ITeventSubscriber<TComponentTevent> RegisterTeventAppliers()
    {
        State.Assert(TeventAppliers is null, () => $"{GetType()} declares its tevent appliers in a {nameof(TeventRouting)}, so it cannot also register appliers per instance.");
        return _teventAppliersDispatcher._assert().NotNull().Register();
    }

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
