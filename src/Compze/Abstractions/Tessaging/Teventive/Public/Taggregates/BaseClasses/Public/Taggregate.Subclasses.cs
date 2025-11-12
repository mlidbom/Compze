using Compze.Core.Tessaging.Teventive.Public.Taggregates.Tevents.Public;
using JetBrains.Annotations;

namespace Compze.Core.Tessaging.Teventive.Public.Taggregates.BaseClasses.Public;

public partial class Taggregate<TTaggregate, TTaggregateTevent, TTaggregateTeventImplementation, TWrapperTeventInterface, TWrapperTeventImplementation>
    where TWrapperTeventImplementation : TWrapperTeventInterface
    where TWrapperTeventInterface : ITaggregateIdentifyingTevent<TTaggregateTevent>
    where TTaggregate : Taggregate<TTaggregate, TTaggregateTevent, TTaggregateTeventImplementation, TWrapperTeventInterface, TWrapperTeventImplementation>
    where TTaggregateTevent : class, ITaggregateTevent
    where TTaggregateTeventImplementation : TaggregateTevent, TTaggregateTevent
{
    public abstract class Component<TComponent, TComponentTevent, TComponentTeventImplementation>
        : TeventiveComponent<TTaggregate, TTaggregateTevent, TTaggregateTeventImplementation, TComponent, TComponentTevent, TComponentTeventImplementation>
        where TComponentTevent : class, TTaggregateTevent
        where TComponentTeventImplementation : TTaggregateTeventImplementation, TComponentTevent
        where TComponent : Component<TComponent, TComponentTevent, TComponentTeventImplementation>
    {
        protected Component(TTaggregate parent) : base(parent) {}
    }

    [UsedImplicitly(ImplicitUseKindFlags.InstantiatedWithFixedConstructorSignature)]
    public abstract class Entity<TEntity, TEntityId, TEntityTeventImplementation, TEntityTevent, TEntityCreatedTevent, TEntityTeventIdGetterSetter>
        : TeventiveEntity<TTaggregate, TTaggregateTevent, TTaggregateTeventImplementation, TEntity, TEntityId, TEntityTeventImplementation, TEntityTevent, TEntityCreatedTevent, TEntityTeventIdGetterSetter>
        where TEntityId : struct
        where TEntityTevent : class, TTaggregateTevent
        where TEntityTeventImplementation : TTaggregateTeventImplementation, TEntityTevent
        where TEntityCreatedTevent : TEntityTevent
        where TEntity : Entity<TEntity, TEntityId, TEntityTeventImplementation, TEntityTevent, TEntityCreatedTevent, TEntityTeventIdGetterSetter>
        where TEntityTeventIdGetterSetter : IGetSetTaggregateEntityTeventEntityId<TEntityId, TEntityTeventImplementation, TEntityTevent>
    {
        protected Entity(TTaggregate taggregate) : base(taggregate) {}
    }

    public abstract class RemovableEntity<TEntity, TEntityId, TEntityTeventImplementation, TEntityTevent, TEntityCreatedTevent, TEntityRemovedTevent, TEntityTeventIdGetterSetter>
        : TeventiveRemovableEntity<TTaggregate, TTaggregateTevent, TTaggregateTeventImplementation, TEntity, TEntityId, TEntityTevent, TEntityTeventImplementation, TEntityCreatedTevent, TEntityRemovedTevent, TEntityTeventIdGetterSetter>
        where TEntityId : struct
        where TEntityTevent : class, TTaggregateTevent
        where TEntityTeventImplementation : TTaggregateTeventImplementation, TEntityTevent
        where TEntityCreatedTevent : TEntityTevent
        where TEntityRemovedTevent : TEntityTevent
        where TEntity : RemovableEntity<TEntity, TEntityId, TEntityTeventImplementation, TEntityTevent, TEntityCreatedTevent, TEntityRemovedTevent, TEntityTeventIdGetterSetter>
        where TEntityTeventIdGetterSetter : IGetSetTaggregateEntityTeventEntityId<TEntityId, TEntityTeventImplementation, TEntityTevent>
    {
        protected RemovableEntity(TTaggregate taggregate) : base(taggregate) {}
    }
}
