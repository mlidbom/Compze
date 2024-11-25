using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using Composable.Functional;
using Composable.Serialization;
using Composable.SystemCE;
using Composable.SystemCE.ReflectionCE;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Composable.Logging;

#pragma warning disable CA2326 //Todo about this resides elsewhere search for CA2326 to find it

enum LogLevel
{
   None = 0,
   Error = 1,
   Warning = 2,
   Info = 3,
   Debug = 4
}
class ConsoleLogger : ILogger
{
   readonly Type _type;

   LogLevel _logLevel = LogLevel.Info;

   ConsoleLogger(Type type) => _type = type;

   public static ILogger Create(Type type) => new ConsoleLogger(type);
   public ILogger WithLogLevel(LogLevel level) => new ConsoleLogger(_type) { _logLevel = level };
   public Unit Error(Exception exception, string? message)
   {
      if(_logLevel >= LogLevel.Error)
      {
         ConsoleCE.WriteLine($@"
############################################# ERROR in : {_type.GetFullNameCompilable()} #############################################
MESSAGE: {message} 
EXCEPTION: {exception}

{(exception is AggregateException aggregateException
     ? $@"
############################################# SERIALIZED AGGREGATE EXCEPTION #############################################
{SerializeExceptions(aggregateException.InnerExceptions)}"
     : $@"
############################################# SERIALIZED EXCEPTION #############################################
{SerializeException(exception)}")}

############################################# END ERROR #############################################
");
      }

      return Unit.Instance;
   }

   static string SerializeExceptions(ReadOnlyCollection<Exception> exceptions) =>
      exceptions.Select((exception, index) => $@"

############################################# INNER EXCEPTION {index + 1} #############################################
{SerializeException(exception)}
############################################# END EXCEPTION {index + 1} #############################################

").Join(string.Empty);

   static string SerializeException(Exception exception)
   {
      try
      {
         return JsonConvert.SerializeObject(exception, Formatting.Indented, ExceptionSerializationSettings);
      }
      catch(Exception e)
      {
         return $"Serialization Failed with message: {e.Message}";
      }
   }

   public Unit Warning(string message)
   {
      if(_logLevel >= LogLevel.Warning)
      {
         ConsoleCE.WriteLine($"WARNING:{_type}: {DateTime.Now:HH:mm:ss.fff} {message}");
      }

      return Unit.Instance;
   }

   public Unit Warning(Exception exception, string message)
   {
      if(_logLevel >= LogLevel.Warning)
      {
         ConsoleCE.WriteLine($"WARNING:{_type}: {DateTime.Now:HH:mm:ss.fff} {message}, \n: Exception: {exception}");
      }

      return Unit.Instance;
   }

   public Unit Info(string message)
   {
      if(_logLevel >= LogLevel.Info)
      {
         ConsoleCE.WriteLine($"INFO:{_type}: {DateTime.Now:HH:mm:ss.fff} {message}");
      }

      return Unit.Instance;
   }

   public Unit Debug(string message)
   {
      if(_logLevel >= LogLevel.Debug)
      {
         ConsoleCE.WriteLine($"DEBUG:{_type}: {DateTime.Now:HH:mm:ss.fff} {message}");
      }

      return Unit.Instance;
   }

   [StringFormatMethod(formatParameterName: "message")]
   public Unit DebugFormat(string message, params object[] arguments) => Unit.From(() => StringCE.FormatInvariant(message, arguments));

#pragma warning disable CA2326
#pragma warning disable CA2327 //Todo: see if we can mitigate the security impact without breaking our serialization.
   static readonly JsonSerializerSettings ExceptionSerializationSettings =
      new()
      {
         TypeNameHandling = TypeNameHandling.All,
         ContractResolver = IgnoreStackTraces.Instance
      };
#pragma warning restore CA2326
#pragma warning restore CA2327

   class IgnoreStackTraces : IncludeMembersWithPrivateSettersResolver, IStaticInstancePropertySingleton
   {
      public new static readonly IgnoreStackTraces Instance = new();
      IgnoreStackTraces()
      {
         IgnoreSerializableInterface = true;
         IgnoreSerializableAttribute = true;
      }
      protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
      {
         var property = base.CreateProperty(member, memberSerialization);

         if(property.PropertyName is nameof(Exception.StackTrace) or "StackTraceString")
         {
            property.Ignored = true;
         }

         return property;
      }
   }
}
