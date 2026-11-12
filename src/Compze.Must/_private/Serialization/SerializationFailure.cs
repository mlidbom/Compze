using Compze.Internals.SystemCE.ReflectionCE;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Compze.Must._private.Serialization;

/// <summary>
/// Why Must could not serialize a value: the exception, where in the object graph it was thrown, and the configuration
/// that leaves the failing member out.
/// </summary>
class SerializationFailure
{
   const int LongestPathShownInFull = 300;
   const int PathCharactersShownAtEachEnd = 120;

   readonly Exception _exception;
   readonly Type? _declaringType;
   readonly string? _memberName;
   readonly string _path;

   SerializationFailure(Exception exception, Type? declaringType, string? memberName, string path)
   {
      _exception = exception;
      _declaringType = declaringType;
      _memberName = memberName;
      _path = path;
   }

   /// <summary>
   /// The failure an error Newtonsoft reports describes. Newtonsoft wraps an exception thrown by a getter in a
   /// <see cref="JsonSerializationException"/>; the getter's own exception is the one that says what went wrong.
   /// </summary>
   public static SerializationFailure From(ErrorContext error) =>
      new(error.Error is JsonSerializationException { InnerException: {} thrownByAGetter } ? thrownByAGetter : error.Error,
          error.OriginalObject?.GetType(),
          error.Member as string,
          PathOfTheFailingMember(error));

   /// <summary>Newtonsoft reports the path of the object or collection whose member failed; the member's own path appends its name, or its index.</summary>
   static string PathOfTheFailingMember(ErrorContext error)
   {
      var containerPath = error.Path;
      return error.Member switch
      {
         string name when containerPath.Length == 0 => name,
         string name                                => $"{containerPath}.{name}",
         int index                                  => $"{containerPath}[{index}]",
         _                                          => containerPath
      };
   }

   public string Describe() =>
      $"""
       {WhatWentWrong()}
       {WhereItWentWrong()}{HowToLeaveItOut()}
       """;

   string WhatWentWrong() =>
      _exception is SerializationStackExhaustedException
         ? _exception.Message
         : $"{_exception.GetType().FullName}: {_exception.Message}";

   string WhereItWentWrong() =>
      _declaringType != null && _memberName != null
         ? $"while reading {_declaringType.GetFullNameCompilable()}.{_memberName}, at the path: {ShortenedPath()}"
         : _path.Length > 0
            ? $"at the path: {ShortenedPath()}"
            : "at the root of the value";

   string HowToLeaveItOut()
   {
      if(_declaringType == null)
         return "";

      var rule = _memberName != null
                    ? $"""ExcludeMember({TypeExpression(_declaringType)}, "{_memberName}")"""
                    : $"ExcludeType({TypeExpression(_declaringType)})";

      return $"""

              To leave it out of this assertion, configure the Must call:
                 .Must(serialization: it => it.{rule})
              To leave it out of every assertion, once when the test assembly starts:
                 MustSerialization.{rule};
              """;
   }

   string ShortenedPath() =>
      _path.Length <= LongestPathShownInFull
         ? _path
         : $"{_path[..PathCharactersShownAtEachEnd]} ... {_path[^PathCharactersShownAtEachEnd..]}";

   /// <summary>C# that evaluates to <paramref name="type"/>: a typeof expression where the test can name the type, and a lookup by name where it is not public.</summary>
   static string TypeExpression(Type type) =>
      type.IsVisible
         ? $"typeof({type.GetFullNameCompilable()})"
         : $"""Type.GetType("{type.FullName}, {type.Assembly.GetName().Name}")!""";
}
