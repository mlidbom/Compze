using Compze.Tessaging.TessageTypes;
using Compze.TypeIdentifiers;
// ReSharper disable PropertyCanBeMadeInitOnly.Global serialization...
// ReSharper disable MemberCanBeInternal serialization...

// ReSharper disable ClassNeverInstantiated.Global
#pragma warning disable CA1724 // Type names should not match namespaces

namespace Compze.Tessaging.Specifications.Typermedia;

public class Greeting
{
   public string Message { get; set; } = "";
}

public class GreetingTuery : Remotable.NonTransactional.Tueries.Tuery<Greeting>
{
   public string Name { get; set; } = "";
}

public class GreeterRegistered
{
   public string Name { get; set; } = "";
}

public class RegisterGreeterTommand : Remotable.AtMostOnce.AtMostOnceTypermediaTommand<GreeterRegistered>
{
   RegisterGreeterTommand() {}
   public static RegisterGreeterTommand Create(string name) => new() { Id = TessageId.New(), Name = name };

   // ReSharper disable once MemberCanBeInternal — Serialized via Newtonsoft
   public string Name { get; set; } = "";
}

public class TueryAnswer
{
   public string Message { get; set; } = "";
}

public class TueryBothEndpointsHandle : Remotable.NonTransactional.Tueries.Tuery<TueryAnswer>;

public class TueryOnlyTheSecondEndpointHandles : Remotable.NonTransactional.Tueries.Tuery<TueryAnswer>;

public class TueryServedByTheLateEndpoint : Remotable.NonTransactional.Tueries.Tuery<TueryAnswer>;

public class TueryNothingServes : Remotable.NonTransactional.Tueries.Tuery<TueryAnswer>;

///<summary>A number carried back from a navigation step, so that a chain composing steps has something to pass along.</summary>
public class NumberResource
{
   public int Value { get; set; }
}

public class TheAnswerTuery : Remotable.NonTransactional.Tueries.Tuery<NumberResource>;

public class AddTuery : Remotable.NonTransactional.Tueries.Tuery<NumberResource>
{
   public int Left { get; set; }
   public int Right { get; set; }
}

public class RememberNumberTommand : Remotable.AtMostOnce.AtMostOnceTypermediaTommand<NumberResource>
{
   RememberNumberTommand() {}
   public static RememberNumberTommand Create(int number) => new() { Id = TessageId.New(), Number = number };

   public int Number { get; set; }
}
