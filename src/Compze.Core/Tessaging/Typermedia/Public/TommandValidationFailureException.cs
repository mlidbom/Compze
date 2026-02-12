using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Compze.Core.Tessaging.Typermedia.Public;

public class TommandValidationFailureException(IEnumerable<ValidationResult> failures) : Exception(string.Join(Environment.NewLine, failures));