namespace Compze.Teventive.TeventStore.Abstractions.QueryModels.Generators.Public;

public class NoSuchDocumentException(object key, Type type) : ArgumentOutOfRangeException($"Type: {type.FullName}, Key: {key}");