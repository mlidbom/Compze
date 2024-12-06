using System;
using System.Globalization;
using System.Linq;
using NUnit.Framework;
using Serilog;
using Serilog.Events;
using Serilog.Parsing;

namespace ScratchPad.Serilog;

class SerilogParserExperiments
{
   [Test] public void DoStuff()
   {
      const string messageTemplate = "{Name} is {Age} years old.";
      var parser = new MessageTemplateParser();
      var template = parser.Parse(messageTemplate);

      object[] propertyValues = ["Bob", 34];
      var properties = template.Tokens
                               .OfType<PropertyToken>()
                               .Distinct()
                               .Zip(propertyValues, Tuple.Create)
                               .ToDictionary(
                                   p => p.Item1.PropertyName,
                                   LogEventPropertyValue (p) => new ScalarValue(p.Item2));

      var rendered = template.Render(properties, formatProvider:CultureInfo.InvariantCulture);

      Assert.That(rendered, Is.EqualTo("\"Bob\" is 34 years old."));

      var logger = new LoggerConfiguration()
                  .WriteTo.Console(formatProvider:CultureInfo.InvariantCulture)
                  .CreateLogger();

      logger.Information(messageTemplate, "Bob", 34);
   }
}
