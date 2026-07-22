using Compze.DependencyInjection.Abstractions;
using Compze.Tessaging.TessageTypes;

namespace Compze.Tessaging.TessageBus;

///<summary>The minimal declaration door for tevent observation — what an endpoint-declaration's <c>ObserveTevents</c> override<br/>
/// receives. Observation is the deliberately transaction-ignoring watch surface: an observer watches committed facts and never<br/>
/// participates in the delivering transaction.<br/>
/// Implemented by <see cref="TeventObservationRegistrar"/>, whose docs carry the full observation semantics.</summary>
public interface ITeventObservationRegistrar
{
   ///<summary>Registers an observer for tevents compatible with <typeparamref name="TTevent"/>. The observer receives a plain<br/>
   /// <see cref="IScopeResolver"/>, never a unit of work: its invocation is a fresh scope with no transaction.</summary>
   ITeventObservationRegistrar ForTevent<TTevent>(Action<TTevent, IScopeResolver> observer) where TTevent : ITevent;
}
