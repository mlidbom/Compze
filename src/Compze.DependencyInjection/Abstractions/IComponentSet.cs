namespace Compze.DependencyInjection.Abstractions;

///<summary>
/// Every member of the <typeparamref name="TService"/> component set (<c>ForSet(...)</c>), as an ordinary constructor<br/>
/// dependency: a component that takes an <see cref="IComponentSet{TService}"/> through <c>CreatedBy(...)</c> receives the whole<br/>
/// set — the injectable counterpart of calling <see cref="IServiceResolver.ResolveSet"/> on a resolver.
///</summary>
///<remarks>
/// Never registered by user code: the container synthesizes the one <see cref="IComponentSet{TService}"/> registration per<br/>
/// component-set service type when it is built, from the set's <c>ForSet(...)</c> members. Its lifestyle follows the members' —<br/>
/// <see cref="Lifestyle.Singleton"/> when every member is a singleton, <see cref="Lifestyle.Scoped"/> otherwise — so a dependency<br/>
/// on the set is subject to exactly the same lifestyle validation as a direct dependency on the members would be.<br/>
/// A set nothing contributed to is still a set — the empty one: a <c>CreatedBy(...)</c> dependency on a set with no<br/>
/// <c>ForSet(...)</c> members receives the empty set, because zero contributions is a legitimate state for a contribution seam.
///</remarks>
///<remarks>
/// The set holds no instances of its own: members resolve on enumeration, from the same container and scope that created the<br/>
/// set — the same thin-typed-view philosophy as <see cref="IServiceResolver{TService}"/>.
///</remarks>
public interface IComponentSet<out TService> : IEnumerable<TService> where TService : class;
