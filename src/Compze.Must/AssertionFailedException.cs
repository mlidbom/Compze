namespace Compze.Must;

/// <summary>The exception thrown when a <see cref="MustExtensions.Must{T}(T, string)"/> assertion fails.</summary>
/// <remarks>
/// Every line of its message ends in <see cref="Environment.NewLine"/>, whatever line endings the text it was made of had:<br/>
/// the values it shows, a custom assertion's own message, and Must's own text, whose raw string literals hold the line<br/>
/// endings of the source files Must was compiled from.
/// </remarks>
/// <param name="message">The assertion failure message.</param>
/// <param name="inner">The exception that triggered the failure, if any.</param>
public class AssertionFailedException(string message, Exception? inner = null) :
   Exception($"""

              {message}
              """.ReplaceLineEndings(), inner);//If we don't ensure a starting newline, the display in test output becomes quite bad
