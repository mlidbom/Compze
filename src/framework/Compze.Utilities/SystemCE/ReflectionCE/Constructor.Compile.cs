using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Compze.SystemCE.ReflectionCE;

public static partial class Constructor
{
   internal static class Compile
   {
      internal static CompilerBuilder<TTypeToConstruct> ForReturnType<TTypeToConstruct>() => new();
      internal static CompilerBuilder<object> ForReturnType(Type typeToConstruct) => new(typeToConstruct);


      internal class CompilerBuilder<TInstance>
      {
         readonly Type _typeToConstruct;
         internal CompilerBuilder(Type typeToConstruct) => _typeToConstruct = typeToConstruct;

         internal CompilerBuilder() : this(typeof(TInstance))
         {
         }

         internal Delegate WithArgumentTypes(Type argument1Type) => CompileForSignature(typeof(Func<,>).MakeGenericType(argument1Type, _typeToConstruct));
         internal Func<TInstance> DefaultConstructor() => (Func<TInstance>)CompileForSignature(typeof(Func<>).MakeGenericType(_typeToConstruct));
         internal Func<TArgument1, TInstance> WithArguments<TArgument1>() => (Func<TArgument1, TInstance>)WithArgumentTypes(typeof(TArgument1));

         static Delegate CompileForSignature(Type delegateType)
         {
            var delegateTypeGenericArgumentTypes = delegateType.GetGenericArguments();
            var instanceType = delegateTypeGenericArgumentTypes[^1];
            var constructorArgumentTypes = delegateTypeGenericArgumentTypes[..^1];

            var constructor = instanceType.GetConstructor(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance, binder: null, types: constructorArgumentTypes, modifiers: null);
            if (constructor == null)
            {
               throw new Exception($"Expected to find a constructor with the signature: [private|protected|public] {instanceType.GetFullNameCompilable()}({DescribeParameterList(constructorArgumentTypes)})");
            }

            var constructorCallMethod = new DynamicMethod(name:$"Generated_constructor_for_{instanceType.Name}", returnType: instanceType, parameterTypes: constructorArgumentTypes, owner: instanceType);
            var ilGenerator = constructorCallMethod.GetILGenerator();
            for (var argumentIndex = 0; argumentIndex < constructorArgumentTypes.Length; argumentIndex++)
            {
               ilGenerator.Emit(OpCodes.Ldarg, argumentIndex);
            }
            ilGenerator.Emit(OpCodes.Newobj, constructor);
            ilGenerator.Emit(OpCodes.Ret);
            return constructorCallMethod.CreateDelegate(delegateType);
         }

         static string DescribeParameterList(IEnumerable<Type> parameterTypes) => parameterTypes.Select(parameterType => parameterType.FullNameNotNull()).Join(", ");
      }
   }
}