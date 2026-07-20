using Compze.Contracts;
using Compze.Tessaging.TessageTypes;

namespace Compze.Teventive;

///<summary>Configures an <see cref="IMutableTeventDispatcher{TTevent}"/>. Supplied when the dispatcher is created and immutable from then on:<br/>
/// which tevents may legitimately go unhandled is dispatcher-lifetime policy, not mutable registration state.</summary>
public class TeventDispatcherConfig
{
   ///<summary>The default config <see cref="Options"/> is <see cref="TeventDispatcherOptions.None"/> and <see cref="IgnoredUnhandled"/> is empty.</summary>
   public static readonly TeventDispatcherConfig Default = new();

   public static readonly TeventDispatcherConfig IgnoreAllUnhandled = new(TeventDispatcherOptions.IgnoreAllUnhandled);

   ///<summary>Create a copy of the config that allows the passed types to be unhandled when published.</summary>
   public TeventDispatcherConfig IgnoreUnhandled<T1>() => new(this) { IgnoredUnhandled = [.. IgnoredUnhandled, .. EnumerableCE.OfTypes<T1>()] };
   ///<summary>Create a copy of the config that allows the passed types to be unhandled when published.</summary>
   public TeventDispatcherConfig IgnoreUnhandled<T1, T2>() => new(this) { IgnoredUnhandled = [.. IgnoredUnhandled, .. EnumerableCE.OfTypes<T1, T2>()] };
   ///<summary>Create a copy of the config that allows the passed types to be unhandled when published.</summary>
   public TeventDispatcherConfig IgnoreUnhandled<T1, T2, T3>() => new(this) { IgnoredUnhandled = [.. IgnoredUnhandled, .. EnumerableCE.OfTypes<T1, T2, T3>()] };
   ///<summary>Create a copy of the config that allows the passed types to be unhandled when published.</summary>
   public TeventDispatcherConfig IgnoreUnhandled<T1, T2, T3, T4>() => new(this) { IgnoredUnhandled = [.. IgnoredUnhandled, .. EnumerableCE.OfTypes<T1, T2, T3, T4>()] };

   ///<summary>The on/off dispatcher options to enable.</summary>
   public TeventDispatcherOptions Options { get; init; }

   ///<summary>The tevent types that may legitimately go unhandled: dispatching a tevent that no registered handler matches throws a <see cref="TeventUnhandledException"/><br/>
   /// unless the tevent is assignable to one of these types. The types need not belong to the dispatcher's own tevent hierarchy: any interface the concrete tevents<br/>
   /// implement works, such as a generic tevent like <see cref="Taggregates.Tevents.Public.ITaggregateCreatedTevent"/> from a parallel hierarchy.</summary>
   public IReadOnlyList<Type> IgnoredUnhandled
   {
      get;
      init
      {
         foreach(var teventType in value)
         {
            Argument.Assert(typeof(ITevent).IsAssignableFrom(teventType), () => $"{teventType} is not an {nameof(ITevent)} and could therefore never be dispatched, much less go unhandled.");
         }

         field = value;
      }
   }

   TeventDispatcherConfig(TeventDispatcherConfig source) : this(source.Options, source.IgnoredUnhandled) {}

   public TeventDispatcherConfig(TeventDispatcherOptions options = TeventDispatcherOptions.None, IReadOnlyList<Type>? ignoreUnhandled = null)
   {
      Options = options;
      IgnoredUnhandled = ignoreUnhandled ?? [];
   }
}

///<summary>The on/off options an <see cref="IMutableTeventDispatcher{TTevent}"/> can be created with via <see cref="TeventDispatcherConfig"/>.</summary>
[Flags]
public enum TeventDispatcherOptions
{
   None = 0,

   ///<summary>No dispatched tevent is required to have a matching handler: the <see cref="TeventUnhandledException"/> check is disabled entirely.</summary>
   IgnoreAllUnhandled = 1
}
