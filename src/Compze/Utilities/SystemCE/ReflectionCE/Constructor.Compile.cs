using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Compze.Utilities.SystemCE.ReflectionCE;

public static partial class Constructor
{
   internal static class Compile
   {
      internal static CompilerBuilder<TTypeToConstruct> ForType<TTypeToConstruct>() => new();
      internal static CompilerBuilder<object> ForType(Type typeToConstruct) => new(typeToConstruct);

      internal class CompilerBuilder<TInstance>
      {
         readonly Type _typeToConstruct;
         internal CompilerBuilder(Type typeToConstruct) => _typeToConstruct = typeToConstruct;

         internal CompilerBuilder() : this(typeof(TInstance)) {}

         Delegate WithArgumentTypes(Type argument1Type) => CompileForSignature(typeof(Func<,>).MakeGenericType(argument1Type, _typeToConstruct));
         internal Func<TInstance> DefaultConstructor() => (Func<TInstance>)CompileForSignature(typeof(Func<>).MakeGenericType(_typeToConstruct));
         internal Func<TArgument1, TInstance> WithArguments<TArgument1>() => (Func<TArgument1, TInstance>)WithArgumentTypes(typeof(TArgument1));

         internal Func<object, object> WithArgumentTypesAsObjectFunc(Type argument1Type)
         {
            var constructor = WithArgumentTypes(argument1Type);

            var parameter = Expression.Parameter(typeof(object), "arg");
            var parameterCastToCorrectType = Expression.Convert(parameter, argument1Type);
            var constructorInvocation = Expression.Invoke(Expression.Constant(constructor), parameterCastToCorrectType);
            var castReturnValueToObject = Expression.Convert(constructorInvocation, typeof(object));
            var lambdaForWholeSequence = Expression.Lambda<Func<object, object>>(castReturnValueToObject, parameter);

            return lambdaForWholeSequence.Compile();
         }

         static Delegate CompileForSignature(Type delegateType)
         {
            var delegateTypeGenericArgumentTypes = delegateType.GetGenericArguments();
            var instanceType = delegateTypeGenericArgumentTypes[^1];
            var constructorArgumentTypes = delegateTypeGenericArgumentTypes[..^1];

            var constructor = instanceType.GetConstructor(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance, binder: null, types: constructorArgumentTypes, modifiers: null);
            if(constructor == null)
            {
               throw new Exception($"Expected to find a constructor with the signature: [private|protected|public] {instanceType.GetFullNameCompilable()}({DescribeParameterList(constructorArgumentTypes)})");
            }

            var parameters = constructorArgumentTypes.Select((type, index) => Expression.Parameter(type, $"argument_{index}")).ToArray();
            var constructorCall = Expression.New(constructor, parameters);
            var lambda = Expression.Lambda(delegateType, constructorCall, parameters);

            return lambda.Compile();
         }

         static string DescribeParameterList(IEnumerable<Type> parameterTypes) => parameterTypes.Select(parameterType => parameterType.FullNameNotNull()).Join(", ");
      }
   }
}
