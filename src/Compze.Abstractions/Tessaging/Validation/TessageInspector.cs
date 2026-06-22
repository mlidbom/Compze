using Compze.Abstractions.Tessaging.Public;

namespace Compze.Abstractions.Tessaging.Validation;

/// <summary>
/// The one place messaging code calls to assert that a message — a "tessage" (the framework's umbrella term
/// for its tommands, tevents and tqueries) — is valid before a handler subscribes to it, it is executed, or it
/// is sent to another process. Throws when it is not.
/// </summary>
/// <remarks>
/// A tessage can be invalid in two distinct ways, and this facade exposes both so callers reach for one obvious
/// place instead of choosing between the underlying checkers. <see cref="TessageTypeInspector"/> enforces the
/// design rules a message <c>type</c> must satisfy (the generic <see cref="AssertValid{TTessage}"/> /
/// <see cref="AssertValidForSubscription{TTessage}"/> overloads); <see cref="TessageValidator"/> enforces the
/// rules a message <c>instance</c> must satisfy for a particular use
/// (<see cref="AssertValidToSendRemote"/>, <see cref="AssertValidToExecuteLocally"/>).
/// </remarks>
public static class TessageInspector
{
   /// <summary>Asserts that <typeparamref name="TTessage"/> is a validly-designed type for a handler to subscribe to.</summary>
   public static void AssertValidForSubscription<TTessage>() => TessageTypeInspector.AssertValidForSubscription(typeof(TTessage));

   /// <summary>Asserts that <typeparamref name="TTessage"/> is a validly-designed message type.</summary>
   public static void AssertValid<TTessage>() => TessageTypeInspector.AssertValid(typeof(TTessage));

   /// <summary>Asserts that <paramref name="tessage"/> is valid to send to another process across the wire.</summary>
   public static void AssertValidToSendRemote(ITessage tessage) => TessageValidator.AssertValidToSendRemote(tessage);

   /// <summary>Asserts that <paramref name="tessage"/> is valid to execute in the current process.</summary>
   public static void AssertValidToExecuteLocally(ITessage tessage) => TessageValidator.AssertValidToExecuteLocally(tessage);
}
