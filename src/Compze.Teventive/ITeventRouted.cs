namespace Compze.Teventive;

///<summary>A teventive class that declares its tevent routing once, for the class, in <see cref="TeventRouting"/>s kept in static fields:<br/>
/// what Compze's base classes read a class's routing through. Every instance of the class then holds none of it.</summary>
///<remarks>Implement it explicitly, so that it doesn't show in the class's members, and list it again in the base list of every class<br/>
/// that declares a routing of its own:<br/>
/// <c>class VocabNote : Note, ITeventRouted { TeventRouting? ITeventRouted.TeventAppliers => Appliers; }</c><br/>
/// A base class reading the routing through this interface then gets the most derived class's. The base classes implement it to<br/>
/// return null, which means the class registers its appliers and tevent handlers per instance instead.<br/>
/// A class that inherits a routing it doesn't declare for itself is refused when it is constructed: every class declares its own,<br/>
/// extending its base class's with <see cref="TeventRouting{TInstance,TTevent}.Extend{TSubclass}"/>.</remarks>
public interface ITeventRouted
{
   ///<summary>The routing of the class's tevents to its tevent appliers, or null when the class registers them per instance.</summary>
   TeventRouting? TeventAppliers { get; }

   ///<summary>The routing of the class's tevents to its tevent handlers, or null when it has none, or registers them per instance.</summary>
   TeventRouting? TeventHandlers { get; }
}
