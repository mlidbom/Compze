namespace PostReleaseBindingCheck.Failures;

/// <summary>The ways a <see cref="BindingFailure"/> can happen, each named after what the runtime would find.</summary>
public enum BindingFailureKind
{
   /// <summary>A type the referencing package uses does not exist in the referenced package: <see cref="TypeLoadException"/>.</summary>
   MissingType,

   /// <summary>
   /// A method or field the referencing package uses does not exist in the referenced package with the signature it was compiled against,<br/>
   /// whether static or instance included: <see cref="MissingMethodException"/> or <see cref="MissingFieldException"/>.
   /// </summary>
   MissingMember,

   /// <summary>A type the referencing package uses is not accessible to it: <see cref="TypeAccessException"/>.</summary>
   InaccessibleType,

   /// <summary>A method or field the referencing package uses is not accessible to it: <see cref="MethodAccessException"/> or <see cref="FieldAccessException"/>.</summary>
   InaccessibleMember,

   /// <summary>
   /// A concrete type of the referencing package does not implement an abstract member of an interface or base class in the referenced package,<br/>
   /// usually one added after the referencing package was compiled: <see cref="TypeLoadException"/>.
   /// </summary>
   UnimplementedAbstractMember,

   /// <summary>A type of the referencing package derives from a class of the referenced package that is sealed: <see cref="TypeLoadException"/>.</summary>
   SealedBaseClass
}
