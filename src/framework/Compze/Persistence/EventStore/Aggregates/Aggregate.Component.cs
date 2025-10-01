using Compze.GenericAbstractions.Time;
using Compze.Messaging.Events;
using Compze.SystemCE.ReflectionCE;
using System;
using JetBrains.Annotations;

namespace Compze.Persistence.EventStore.Aggregates;

public partial class Aggregate<TAggregate, TAggregateEventImplementation, TAggregateEvent, TWrapperEventImplementation, TWrapperEventInterface>
    where TWrapperEventImplementation : TWrapperEventInterface
    where TWrapperEventInterface : IAggregateWrapperEvent<TAggregateEvent>
    where TAggregate : Aggregate<TAggregate, TAggregateEventImplementation, TAggregateEvent, TWrapperEventImplementation, TWrapperEventInterface>
    where TAggregateEvent : class, IAggregateEvent
    where TAggregateEventImplementation : AggregateEvent, TAggregateEvent
{
    public abstract class Component<TComponent, TComponentEventImplementation, TComponentEvent>
        : EventiveComponent<TAggregate, TAggregateEvent, TAggregateEventImplementation, TComponent, TComponentEventImplementation, TComponentEvent>
        where TComponentEvent : class, TAggregateEvent
        where TComponentEventImplementation : TAggregateEventImplementation, TComponentEvent
        where TComponent : Component<TComponent, TComponentEventImplementation, TComponentEvent>
    {
        protected Component(Action<TComponentEventImplementation> raiseEventThroughParent,
                            IEventHandlerRegistrar<TComponentEvent> appliersRegistrar,
                            bool registerEventAppliers)
            : base(raiseEventThroughParent, appliersRegistrar, registerEventAppliers) {}

        ////////////////////////Entity collection

        public class ComponentEntityCollectionManager<TParent, TEntity, TEntityId, TEntityEventImplementation, TEntityEvent, TEntityCreatedEvent, TEntityEventIdGetterSetter>
            : IEntityCollectionManager<TEntity, TEntityId, TEntityEvent, TEntityEventImplementation, TEntityCreatedEvent>
            where TEntityId : notnull
            where TEntityEvent : class, TAggregateEvent
            where TEntityCreatedEvent : TEntityEvent
            where TEntityEventImplementation : TEntityEvent, TAggregateEventImplementation
            where TEntity : Component<TEntity, TEntityEventImplementation, TEntityEvent>
            where TEntityEventIdGetterSetter : IGetAggregateEntityEventEntityId<TEntityEvent, TEntityId>
        {
            protected static readonly TEntityEventIdGetterSetter IdGetter = Constructor.For<TEntityEventIdGetterSetter>.DefaultConstructor.Instance();

            protected EntityCollection<TEntity, TEntityId> ManagedEntities { get; }
            readonly Action<TEntityEventImplementation> _raiseEventThroughParent;

            protected ComponentEntityCollectionManager(TParent parent,
                                              Action<TEntityEventImplementation> raiseEventThroughParent,
                                              IEventHandlerRegistrar<TEntityEvent> appliersRegistrar)
            {
                ManagedEntities = [];
                _raiseEventThroughParent = raiseEventThroughParent;
                appliersRegistrar
                   .For<TEntityCreatedEvent>(e =>
                    {
                        var entity = Constructor.For<TEntity>.WithArguments<TParent>.Instance(parent);
                        ManagedEntities.Add(entity, IdGetter.GetId(e));
                    })
                   .For<TEntityEvent>(e => ManagedEntities[IdGetter.GetId(e)].ApplyEvent(e));
            }

            public IReadOnlyEntityCollection<TEntity, TEntityId> Entities => ManagedEntities;

            public TEntity AddByPublishing<TCreationEvent>(TCreationEvent creationEvent) where TCreationEvent : TEntityEventImplementation, TEntityCreatedEvent
            {
                _raiseEventThroughParent(creationEvent);
                var result = ManagedEntities.InCreationOrder[^1];
                return result;
            }
        }

        ////////////////////////Nested component

        [UsedImplicitly(ImplicitUseKindFlags.InstantiatedWithFixedConstructorSignature)]
        public abstract class ComponentNestedComponent<TNestedComponent, TNestedComponentEventImplementation, TNestedComponentEvent> :
            Component<TNestedComponent, TNestedComponentEventImplementation, TNestedComponentEvent>
            where TNestedComponentEvent : class, TComponentEvent
            where TNestedComponentEventImplementation : TComponentEventImplementation, TNestedComponentEvent
            where TNestedComponent : ComponentNestedComponent<TNestedComponent, TNestedComponentEventImplementation, TNestedComponentEvent>
        {
            static ComponentNestedComponent() => AggregateTypeValidator<TNestedComponent, TNestedComponentEventImplementation, TNestedComponentEvent>.AssertStaticStructureIsValid();

            protected ComponentNestedComponent(TComponent parent)
                : base(parent.Publish, parent.RegisterEventAppliers(), registerEventAppliers: true) {}

            protected ComponentNestedComponent(Action<TNestedComponentEventImplementation> raiseEventThroughParent,
                                      IEventHandlerRegistrar<TNestedComponentEvent> appliersRegistrar,
                                      bool registerEventAppliers) : base(raiseEventThroughParent, appliersRegistrar, registerEventAppliers) {}
        }

        ////////////////////////Nested entity
        public abstract class ComponentNestedEntity<TEntity, TEntityId, TEntityEventImplementation, TEntityEvent, TEntityCreatedEvent, TEntityEventIdGetterSetter>
            : AggregateEntity<TEntity, TEntityId, TEntityEventImplementation, TEntityEvent, TEntityCreatedEvent, TEntityEventIdGetterSetter>
            where TEntityId : struct
            where TEntityEvent : class, TComponentEvent
            where TEntityEventImplementation : TComponentEventImplementation, TEntityEvent
            where TEntityCreatedEvent : TEntityEvent
            where TEntity : ComponentNestedEntity<TEntity, TEntityId, TEntityEventImplementation, TEntityEvent, TEntityCreatedEvent, TEntityEventIdGetterSetter>
            where TEntityEventIdGetterSetter : IGetSetAggregateEntityEventEntityId<TEntityId, TEntityEventImplementation, TEntityEvent>
        {
            static ComponentNestedEntity() => AggregateTypeValidator<TEntity, TEntityEventImplementation, TEntityEvent>.AssertStaticStructureIsValid();

            protected ComponentNestedEntity(Action<TEntityEventImplementation> raiseEventThroughParent, IEventHandlerRegistrar<TEntityEvent> appliersRegistrar) : base(raiseEventThroughParent, appliersRegistrar) {}
        }

        ///////////////////////Removable nested entity

        public abstract class ComponentRemovableNestedEntity<TEntity, TEntityId, TEntityEventImplementation, TEntityEvent, TEntityCreatedEvent, TEntityRemovedEvent, TEntityEventIdGetterSetter>
            : ComponentNestedEntity<TEntity, TEntityId, TEntityEventImplementation, TEntityEvent, TEntityCreatedEvent, TEntityEventIdGetterSetter>
            where TEntityId : struct
            where TEntityEvent : class, TComponentEvent
            where TEntityEventImplementation : TComponentEventImplementation, TEntityEvent
            where TEntityCreatedEvent : TEntityEvent
            where TEntityRemovedEvent : TEntityEvent
            where TEntityEventIdGetterSetter :
            IGetSetAggregateEntityEventEntityId<TEntityId, TEntityEventImplementation, TEntityEvent>
            where TEntity : ComponentRemovableNestedEntity<TEntity, TEntityId, TEntityEventImplementation, TEntityEvent, TEntityCreatedEvent, TEntityRemovedEvent, TEntityEventIdGetterSetter>
        {
            static ComponentRemovableNestedEntity() => AggregateTypeValidator<TEntity, TEntityEventImplementation, TEntityEvent>.AssertStaticStructureIsValid();

            protected ComponentRemovableNestedEntity(TComponent parent) : this(parent.Publish, parent.RegisterEventAppliers()) {}

            ComponentRemovableNestedEntity(Action<TEntityEventImplementation> raiseEventThroughParent, IEventHandlerRegistrar<TEntityEvent> appliersRegistrar)
                : base(raiseEventThroughParent, appliersRegistrar)
            {
                RegisterEventAppliers()
                   .IgnoreUnhandled<TEntityRemovedEvent>();
            }

            public static CollectionManager CreateSelfManagingCollection(TComponent parent)
                => new(parent: parent, raiseEventThroughParent: parent.Publish, appliersRegistrar: parent.RegisterEventAppliers());

            public new class CollectionManager : ComponentRemovableEntityCollectionManager<TComponent, TEntity, TEntityId, TEntityEventImplementation, TEntityEvent, TEntityCreatedEvent, TEntityRemovedEvent, TEntityEventIdGetterSetter>
            {
                internal CollectionManager(TComponent parent, Action<TEntityEventImplementation> raiseEventThroughParent, IEventHandlerRegistrar<TEntityEvent> appliersRegistrar)
                    : base(parent, raiseEventThroughParent, appliersRegistrar) {}
            }
        }

        ///////////////////////Removable entity collection

        public class ComponentRemovableEntityCollectionManager<TParent, TEntity, TEntityId, TEntityEventImplementation, TEntityEvent, TEntityCreatedEvent, TEntityRemovedEvent, TEntityEventIdGetterSetter>
            : ComponentEntityCollectionManager<TParent, TEntity, TEntityId, TEntityEventImplementation, TEntityEvent, TEntityCreatedEvent, TEntityEventIdGetterSetter>
            where TEntityId : notnull
            where TEntityEvent : class, TAggregateEvent
            where TEntityCreatedEvent : TEntityEvent
            where TEntityRemovedEvent : TEntityEvent
            where TEntityEventImplementation : TEntityEvent, TAggregateEventImplementation
            where TEntity : Component<TEntity, TEntityEventImplementation, TEntityEvent>
            where TEntityEventIdGetterSetter :
            IGetSetAggregateEntityEventEntityId<TEntityId, TEntityEventImplementation, TEntityEvent>
        {
            protected ComponentRemovableEntityCollectionManager(TParent parent,
                                              Action<TEntityEventImplementation> raiseEventThroughParent,
                                              IEventHandlerRegistrar<TEntityEvent> appliersRegistrar)
                : base(parent, raiseEventThroughParent, appliersRegistrar)
            {
                appliersRegistrar.For<TEntityRemovedEvent>(e =>
                {
                    var id = IdGetter.GetId(e);
                    ManagedEntities.Remove(id);
                });
            }
        }
    }
}
