namespace Compze.Must;

/// <summary>The exception thrown when a <see cref="__Must.Must{T}(T, string)"/> assertion fails.</summary>
/// <param name="message">The assertion failure message.</param>
/// <param name="inner">The exception that triggered the failure, if any.</param>
public class AssertionFailedException(string message, Exception? inner = null) :
   Exception($"""
              
              {message}
              """, inner);//If we don't ensure a starting newline, the display in test output becomes quite bad
