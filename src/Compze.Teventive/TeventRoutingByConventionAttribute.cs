namespace Compze.Teventive;

///<summary>Routes this class's tevents by convention: its <c>Apply</c> methods are its tevent appliers, its <c>Handle</c> methods its tevent handlers,<br/>
/// and every field that holds a component, an entity collection or a shared-tomponent slot is a part its routing routes to.<br/>
/// Inherited by subclasses, so it goes on the root of a hierarchy.</summary>
///<remarks>A prototype (taggregate-experiments-opus-convention): a final design would need no attribute, the convention being how routing works.</remarks>
[AttributeUsage(AttributeTargets.Class, Inherited = true)]
public sealed class TeventRoutingByConventionAttribute : Attribute;
