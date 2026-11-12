namespace Compze.Tests.Infrastructure;

///<summary>Thrown on purpose by code a specification hands to the code under specification, such as a condition or an action, so the specification<br/>
/// can tell the failure it caused from any other.</summary>
public class IntentionalSpecificationException() : Exception("Thrown on purpose by the specification.");
