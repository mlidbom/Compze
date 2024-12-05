﻿namespace Compze.Contracts.Deprecated;

///<summary>Exception thrown when string is empty and that is not allowed.</summary>
class StringIsEmptyContractViolationException : ContractViolationException
{
   ///<summary>Standard constructor</summary>
   public StringIsEmptyContractViolationException(IInspectedValue badValue) : base(badValue) { }
}