namespace Compze.Teventive.TeventStore.Abstractions.QueryModels.Generators;

///<summary>Thrown by the get operations of <see cref="IQueryModelReader"/> when no query model with the requested key and type<br/>
/// can be produced by any registered <see cref="IQueryModelGenerator"/>.</summary>
public class NoSuchQueryModelException(object key, Type type) : ArgumentOutOfRangeException($"Type: {type.FullName}, Key: {key}");
