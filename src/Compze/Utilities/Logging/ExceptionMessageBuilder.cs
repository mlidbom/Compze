using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;
using Compze.Utilities.SystemCE;
using Compze.Utilities.SystemCE.ReflectionCE;


namespace Compze.Utilities.Logging;

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
          {exception.ToString().IndentToDepth(IndentWith, 1)}
          {IndentWith}{LineSeparator}
          """.IndentToDepth(IndentWith, depth);



}
