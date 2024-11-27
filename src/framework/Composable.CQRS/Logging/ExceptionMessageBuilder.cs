using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using Composable.Serialization;
using Composable.SystemCE;
using Composable.SystemCE.ReflectionCE;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Composable.Logging;

static class ExceptionMessageBuilder
{
   const string LineSeparator = "----------------------------------------------------";
   const string IndentWith = "   ";
   const int StartDepth = 1;
   internal static string BuildExceptionLogMessage(Exception exception, Type type, string? message)
   {
      var builder = new StringBuilder();

      builder.AppendLine(CultureInfo.InvariantCulture, $"""
                                                          ERROR: 
                                                          {IndentWith}Originator: {type.GetFullNameCompilable()}
                                                          {IndentWith}MESSAGE: {message} 
                                                          """);

      if(exception is AggregateException aggregateException)
      {
         builder.AppendLine(BuildAggregateExceptionMessage(aggregateException, depth: StartDepth));
      } else
      {
         builder.Append(BuildRegularExceptionMessage(exception, depth: StartDepth));
      }

      return builder.ToString();
   }

   static string BuildAggregateExceptionMessage(AggregateException aggregateException, int depth)
   {
      return $"""
              {LineSeparator} Aggregate exception
              StackTrace:
              {LineSeparator}
              {aggregateException.StackTrace}
              {LineSeparator}
              {BuildInnerExceptionsMessages(aggregateException.InnerExceptions, depth)}
              """.IndentToDepth(IndentWith, depth);
   }

   static string BuildInnerExceptionsMessages(ReadOnlyCollection<Exception> exceptions, int depth) =>
      exceptions.Select((exception, index) => $"""
                                               {LineSeparator} Inner Exception {index + 1}
                                               {(exception is AggregateException aggregateException ?
                                                    BuildAggregateExceptionMessage(aggregateException, 1) :
                                                    BuildRegularExceptionMessage(exception, depth + 1))}
                                               """)
                .Select(it => it.IndentToDepth("   ", depth))
                .Join(Environment.NewLine);

   static string BuildRegularExceptionMessage(Exception exception, int depth) =>
      $"""
          Type: {exception.GetType().FullName}
          Message:
          {LineSeparator}
          {exception.Message}
          {LineSeparator}
          StackTrace:
          {LineSeparator} 
          {exception.StackTrace}
          {IndentWith}{LineSeparator}
          {IndentWith}SerializedException:
          {IndentWith}{LineSeparator}
          {SerializeException(exception).IndentToDepth(IndentWith, 1)}
          {IndentWith}{LineSeparator}
          """.IndentToDepth(IndentWith, depth);

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
