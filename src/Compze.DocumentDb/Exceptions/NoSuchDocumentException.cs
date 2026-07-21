namespace Compze.DocumentDb.Exceptions;

///<summary>Thrown by the get operations of <see cref="IDocumentDbReader"/> and <see cref="IDocumentDbSession"/> when no document<br/>
/// with the requested key and type exists. Reach for the try-get operations when absence is an expected outcome.</summary>
public class NoSuchDocumentException(object key, Type type) : ArgumentOutOfRangeException($"Type: {type.FullName}, Key: {key}");
