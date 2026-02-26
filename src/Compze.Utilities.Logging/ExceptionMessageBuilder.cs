using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;
using Compze.Utilities.SystemCE;
using Compze.Utilities.SystemCE.ReflectionCE;


namespace Compze.Utilities.Logging;

public static class ExceptionTessageBuilder
{
   const string LineSeparator = "----------------------------------------------------";
   const string IndentWith = "   ";
   const int StartDepth = 1;
   public static string BuildExceptionLogTessage(Exception exception, Type type, string caller, string? message)
   {
      var builder = new StringBuilder();

      builder.AppendLine(CultureInfo.InvariantCulture, $"""
                                                          ERROR: 
                                                          {IndentWith}Originator: {type.GetFullNameCompilable()}.{caller}
                                                          {IndentWith}MESSAGE: {message} 
                                                          """);

      if(exception is AggregateException taggregateException)
      {
         builder.AppendLine(BuildAggregateExceptionTessage(taggregateException, depth: StartDepth));
      } else
      {
         builder.Append(BuildRegularExceptionTessage(exception, depth: StartDepth));
      }

      return builder.ToString();
   }

   static string BuildAggregateExceptionTessage(AggregateException taggregateException, int depth)
   {
      return $"""
              {LineSeparator} Taggregate exception
              StackTrace:
              {LineSeparator}
              {taggregateException.StackTrace}
              {LineSeparator}
              {BuildInnerExceptionsTessages(taggregateException.InnerExceptions, depth)}
              """.IndentToDepth(IndentWith, depth);
   }

   static string BuildInnerExceptionsTessages(ReadOnlyCollection<Exception> exceptions, int depth) =>
      exceptions.Select((exception, index) => $"""
                                               {LineSeparator} Inner Exception {index + 1}
                                               {(exception is AggregateException taggregateException ?
                                                    BuildAggregateExceptionTessage(taggregateException, 1) :
                                                    BuildRegularExceptionTessage(exception, depth + 1))}
                                               """)
                .Select(it => it.IndentToDepth("   ", depth))
                .Join(Environment.NewLine);

   static string BuildRegularExceptionTessage(Exception exception, int depth) =>
      $"""
          Type: {exception.GetType().FullName}
          Tessage:
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
