namespace Compze.Teventive;

public class TeventUnhandledException(Type handlerType, Type teventType) : Exception($"{handlerType} does not handle nor ignore incoming tevent {teventType}");
