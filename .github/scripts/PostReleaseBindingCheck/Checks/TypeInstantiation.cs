using Mono.Cecil;

namespace PostReleaseBindingCheck.Checks;

/// <summary>
/// A type definition together with the generic arguments it is instantiated with, as one concrete type sees it:<br/>
/// for <c>class Greeter : IHandler&lt;string&gt;</c>, Greeter's view of <c>IHandler&lt;T&gt;</c> is <c>IHandler`1&lt;System.String&gt;</c>.
/// </summary>
/// <remarks>
/// The <see cref="TypeLoadCheck"/> compares method signatures through it: <c>Greeter.Handle(string)</c> implements<br/>
/// <c>IHandler&lt;T&gt;.Handle(T)</c> because, seen from Greeter, both render as <c>instance System.Void(System.String)</c>.<br/>
/// The concrete type's own generic parameters render as themselves (<c>!0</c>, <c>!1</c>), and so do a method's (<c>!!0</c>).
/// </remarks>
sealed class TypeInstantiation
{
   readonly IReadOnlyList<string> _genericArguments;

   TypeInstantiation(TypeDefinition definition, IReadOnlyList<string> genericArguments)
   {
      Definition = definition;
      _genericArguments = genericArguments;
   }

   public TypeDefinition Definition { get; }

   /// <summary>The instantiated type's name: <c>Fixture.IHandler`1&lt;System.String&gt;</c>.</summary>
   public string Name => _genericArguments.Count == 0 ? Definition.FullName : $"{Definition.FullName}<{string.Join(",", _genericArguments)}>";

   /// <summary>The concrete type <paramref name="type"/> itself, whose generic parameters stand for themselves.</summary>
   public static TypeInstantiation Of(TypeDefinition type) => new(type, [..type.GenericParameters.Select(parameter => $"!{parameter.Position}")]);

   /// <summary>This type, then its base class, then that one's, as far as Cecil can resolve them: through the packages of the set.</summary>
   public IReadOnlyList<TypeInstantiation> WithBaseClasses()
   {
      var classes = new List<TypeInstantiation> { this };
      for(var current = this; current.BaseClass() is { } baseClass; current = baseClass) classes.Add(baseClass);
      return classes;
   }

   /// <summary>Every interface this type implements, directly or through other interfaces, as far as Cecil can resolve them.</summary>
   public IReadOnlyList<TypeInstantiation> AllInterfaces()
   {
      var interfacesByName = new Dictionary<string, TypeInstantiation>();
      var interfacesToExpand = new Queue<TypeInstantiation>(DirectInterfaces());
      while(interfacesToExpand.TryDequeue(out var implementedInterface))
      {
         if(!interfacesByName.TryAdd(implementedInterface.Name, implementedInterface)) continue;
         foreach(var baseInterface in implementedInterface.DirectInterfaces()) interfacesToExpand.Enqueue(baseInterface);
      }

      return [..interfacesByName.Values];
   }

   /// <summary>
   /// <paramref name="method"/>'s signature, seen through this instantiation: static or instance, return type, parameter types<br/>
   /// and generic arity, such as <c>instance System.Void(System.String)`0</c>.
   /// </summary>
   public string SignatureOf(MethodDefinition method) =>
      $"{(method.HasThis ? "instance" : "static")} {Render(method.ReturnType)}({string.Join(",", method.Parameters.Select(parameter => Render(parameter.ParameterType)))})`{method.GenericParameters.Count}";

   TypeInstantiation? BaseClass() => Definition.BaseType is { } baseType ? InstantiationOf(baseType) : null;

   IEnumerable<TypeInstantiation> DirectInterfaces() =>
      Definition.Interfaces.Select(implementation => InstantiationOf(implementation.InterfaceType)).OfType<TypeInstantiation>();

   /// <summary>
   /// The type <paramref name="reference"/> names, a base class or interface in this type's own metadata, with its generic arguments<br/>
   /// rendered through this instantiation. <see langword="null"/> when it is outside the set, such as <see cref="IDisposable"/>.
   /// </summary>
   TypeInstantiation? InstantiationOf(TypeReference reference)
   {
      var definition = reference.Resolve();
      if(definition == null) return null;
      return reference is GenericInstanceType genericInstance
                ? new TypeInstantiation(definition, [..genericInstance.GenericArguments.Select(Render)])
                : new TypeInstantiation(definition, []);
   }

   string Render(TypeReference type) => type switch
   {
      GenericParameter { Type: GenericParameterType.Method } parameter => $"!!{parameter.Position}",
      GenericParameter parameter => _genericArguments[parameter.Position],
      GenericInstanceType genericInstance => $"{Render(genericInstance.ElementType)}<{string.Join(",", genericInstance.GenericArguments.Select(Render))}>",
      ArrayType array => $"{Render(array.ElementType)}[{new string(',', array.Rank - 1)}]",
      ByReferenceType byReference => $"{Render(byReference.ElementType)}&",
      PointerType pointer => $"{Render(pointer.ElementType)}*",
      RequiredModifierType modified => $"{Render(modified.ElementType)} modreq({modified.ModifierType.FullName})",
      OptionalModifierType modified => $"{Render(modified.ElementType)} modopt({modified.ModifierType.FullName})",
      _ => type.FullName
   };
}
