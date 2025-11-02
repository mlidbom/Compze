using System;

namespace Compze.Utilities.Testing.Fluent;

public class AssertionFailedException(string message, Exception? inner = null) : 
   Exception($"""
              
              {message}
              """, inner);//If we don't ensure a starting newline, the display in test output becomes quite bad
