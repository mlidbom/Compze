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
    public abstract class Component<TComponent, TComponentTevent, TComponentTeventImplementation>(TTaggregate parent) : TeventiveComponent<TTaggregate, TTaggregateTevent, TTaggregateTeventImplementation, TComponent, TComponentTevent, TComponentTeventImplementation>(parent)
       where TComponentTevent : class, TTaggregateTevent
       where TComponentTeventImplementation : TTaggregateTeventImplementation, TComponentTevent
       where TComponent : Component<TComponent, TComponentTevent, TComponentTeventImplementation>;

    [UsedImplicitly(ImplicitUseKindFlags.InstantiatedWithFixedConstructorSignature)]
    public abstract class Entity<TEntity, TEntityId, TEntityTevent, TEntityTeventImplementation, TEntityCreatedTevent, TEntityTeventIdGetterSetter>(TTaggregate taggregate) : Tentity<TTaggregate, TTaggregateTevent, TTaggregateTeventImplementation, TEntity, TEntityId, TEntityTeventImplementation, TEntityTevent, TEntityCreatedTevent, TEntityTeventIdGetterSetter>(taggregate)
       where TEntityId : struct
       where TEntityTevent : class, TTaggregateTevent
       where TEntityTeventImplementation : TTaggregateTeventImplementation, TEntityTevent
       where TEntityCreatedTevent : TEntityTevent
       where TEntity : Entity<TEntity, TEntityId, TEntityTevent, TEntityTeventImplementation, TEntityCreatedTevent, TEntityTeventIdGetterSetter>
       where TEntityTeventIdGetterSetter : IGetSetTaggregateEntityTeventEntityId<TEntityId, TEntityTeventImplementation, TEntityTevent>;

    public abstract class RemovableEntity<TEntity, TEntityId, TEntityTevent, TEntityTeventImplementation, TEntityCreatedTevent, TEntityRemovedTevent, TEntityTeventIdGetterSetter>(TTaggregate taggregate) : TeventiveRemovableEntity<TTaggregate, TTaggregateTevent, TTaggregateTeventImplementation, TEntity, TEntityId, TEntityTevent, TEntityTeventImplementation, TEntityCreatedTevent, TEntityRemovedTevent, TEntityTeventIdGetterSetter>(taggregate)
       where TEntityId : struct
       where TEntityTevent : class, TTaggregateTevent
       where TEntityTeventImplementation : TTaggregateTeventImplementation, TEntityTevent
       where TEntityCreatedTevent : TEntityTevent
       where TEntityRemovedTevent : TEntityTevent
       where TEntity : RemovableEntity<TEntity, TEntityId, TEntityTevent, TEntityTeventImplementation, TEntityCreatedTevent, TEntityRemovedTevent, TEntityTeventIdGetterSetter>
       where TEntityTeventIdGetterSetter : IGetSetTaggregateEntityTeventEntityId<TEntityId, TEntityTeventImplementation, TEntityTevent>;
}
