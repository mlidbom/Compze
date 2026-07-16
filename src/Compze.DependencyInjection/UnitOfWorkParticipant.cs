using Compze.DependencyInjection.Abstractions;

namespace Compze.DependencyInjection;

///<summary>Begins registering a component with <see cref="Lifestyle.UnitOfWork"/>: a <see cref="Lifestyle.Scoped"/> component<br/>
/// that additionally requires an ambient transaction — it participates in the enclosing unit of work, and resolving it with no<br/>
/// ambient transaction present throws.</summary>
///<remarks>Deliberately only the single-service <see cref="For{TService}"/> — no multi-service overloads like<br/>
/// <see cref="Scoped"/>'s: one instance serving several service types usually pairs an updating face with a reading face, and<br/>
/// a reading face must resolve in plain read scopes, where a unit-of-work component cannot even be constructed<br/>
/// (see <c>src/Compze.DependencyInjection/dev_docs/unit-of-work-model.md</c>).</remarks>
public static class UnitOfWorkParticipant
{
   public static ComponentRegistrationWithoutInstantiationSpec<TService> For<TService>() where TService : class => new(Lifestyle.UnitOfWork, []);
}
