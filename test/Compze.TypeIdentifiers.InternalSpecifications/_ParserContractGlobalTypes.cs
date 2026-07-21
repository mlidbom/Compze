// Global-namespace test type whose FullName is exactly 32 hex chars, so Guid.TryParse(FullName) succeeds.
// Used to pin that a GUID-shaped CLR type name does NOT get misclassified as a mapped "GUID, 0" component.
// Must live outside any namespace so its FullName has no dotted prefix.
#pragma warning disable CA1050 // intentionally in the global namespace
#pragma warning disable CS8981 // intentionally lowercase/hex identifier

// ReSharper disable once CheckNamespace Must stay in the global namespace so its FullName has no dotted prefix (see comment above).
public class deadbeefdeadbeefdeadbeefdeadbeef;
