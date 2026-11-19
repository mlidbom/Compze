namespace Compze.Teventive.TeventStore.Abstractions._internal.Exceptions;

class AttemptToAddEmptyTaggregateException(object value) : Exception($"Attempting to add an: {value.GetType().FullName} that has Version=0 and no history to persist.");