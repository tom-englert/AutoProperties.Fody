using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

using JetBrains.Annotations;

using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

namespace AutoProperties.Fody
{
    internal class PropertyAccessorWeaver
    {
        [NotNull]
        private readonly ModuleDefinition _moduleDefinition;
        [NotNull]
        private readonly SystemReferences _systemReferences;
        [NotNull]
        private readonly ILogger _logger;

        public PropertyAccessorWeaver([NotNull] ModuleWeaver moduleWeaver)
        {
            _logger = moduleWeaver;
            _moduleDefinition = moduleWeaver.ModuleDefinition;
            _systemReferences = moduleWeaver.SystemReferences;
        }

        public void Execute()
        {
            try
            {
                var allTypes = _moduleDefinition.GetTypes();

                // ReSharper disable once AssignNullToNotNullAttribute
                var allClasses = allTypes
                    .Where(type => type != null && type.IsClass && (type.BaseType != null));

                var allInterceptors = new Dictionary<TypeReference, Interceptors>(TypeReferenceEqualityComparer.Default);

                foreach (var classDefinition in allClasses.SelectMany(item => item.GetSelfAndBaseTypes().Reverse()))
                {
                    Debug.Assert(classDefinition != null, nameof(classDefinition) + " != null");

                    if (allInterceptors.ContainsKey(classDefinition))
                        continue;

                    try
                    {
                        allInterceptors.Add(classDefinition, new Interceptors(this, classDefinition, allInterceptors.GetValueOrDefault(classDefinition.BaseType)));
                    }
                    catch (WeavingException ex)
                    {
                        _logger.LogError(ex.Message, ex.Method);
                    }
                }

                // ReSharper disable once PossibleNullReferenceException
                foreach (var interceptors in allInterceptors.Values.Where(item => item.HasValues))
                {
                    try
                    {
                        interceptors.Execute();
                    }
                    catch (WeavingException ex)
                    {
                        _logger.LogError(ex.Message, ex.Method);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Unhandled exception. Weaving aborted.");
                _logger.LogDebug(ex.ToString());
            }
        }

        private class Interceptors
        {
            [NotNull]
            private readonly TypeDefinition _classDefinition;
            [NotNull]
            private readonly PropertyAccessorWeaver _weaver;

            [CanBeNull]
            private readonly Interceptors _baseTypeInterceptors;
            [CanBeNull]
            private readonly MethodDefinition _setInterceptor;
            [CanBeNull]
            private readonly MethodDefinition _getInterceptor;

            public Interceptors([NotNull] PropertyAccessorWeaver weaver, [NotNull] TypeReference classDefinition, [CanBeNull] Interceptors baseTypeInterceptors)
            {
                _weaver = weaver;
                // ReSharper disable once AssignNullToNotNullAttribute
                _classDefinition = classDefinition.Resolve();
                _baseTypeInterceptors = baseTypeInterceptors;

                // ReSharper disable once PossibleNullReferenceException
                var allMethods = _classDefinition.Methods;
                if (allMethods == null)
                    return;

                var getInterceptors = allMethods.Where(m => m?.CustomAttributes?.GetAttribute("AutoProperties.GetInterceptorAttribute") != null).ToArray();
                if (getInterceptors.Length > 1)
                    throw new WeavingException($"Multiple [GetInterceptor] attributed methods found in class {classDefinition}.", getInterceptors[1]);

                _getInterceptor = getInterceptors.FirstOrDefault();

                var setInterceptors = allMethods.Where(m => m?.CustomAttributes?.GetAttribute("AutoProperties.SetInterceptorAttribute") != null).ToArray();
                if (setInterceptors.Length > 1)
                    throw new WeavingException($"Multiple [SetInterceptor] attributed methods found in class {classDefinition}.", setInterceptors[1]);

                _setInterceptor = setInterceptors.FirstOrDefault();
            }

            [CanBeNull]
            public MethodDefinition SetInterceptor => _setInterceptor ?? _baseTypeInterceptors?.SetInterceptor.WhenAccessibleInDerivedClass();

            [CanBeNull]
            public MethodDefinition GetInterceptor => _getInterceptor ?? _baseTypeInterceptors?.GetInterceptor.WhenAccessibleInDerivedClass();

            public bool HasValues => GetInterceptor != null || SetInterceptor != null;

            public void Execute()
            {
                new ClassWeaver(_weaver, _classDefinition).Execute(this);
            }
        }

        private class ClassWeaver
        {
            [NotNull]
            private readonly PropertyAccessorWeaver _weaver;
            [NotNull]
            private readonly TypeDefinition _classDefinition;
            [NotNull]
            private readonly ILogger _logger;

            public ClassWeaver([NotNull] PropertyAccessorWeaver weaver, [NotNull] TypeDefinition classDefinition)
            {
                _weaver = weaver;
                _classDefinition = classDefinition;
                _logger = _weaver._logger;
            }

            [NotNull]
            [SuppressMessage("ReSharper", "PossibleNullReferenceException")]
            private MethodDefinition StaticConstructor
            {
                get
                {
                    var constructor = _classDefinition.GetStaticConstructor();
                    if (constructor != null)
                        return constructor;

                    const MethodAttributes attributes = MethodAttributes.Private | MethodAttributes.RTSpecialName | MethodAttributes.SpecialName | MethodAttributes.Static;

                    constructor = new MethodDefinition(".cctor", attributes, _weaver._moduleDefinition.TypeSystem.Void);
                    constructor.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));

                    _classDefinition.Methods.Add(constructor);

                    return constructor;
                }
            }

            [SuppressMessage("ReSharper", "PossibleNullReferenceException")]
            [SuppressMessage("ReSharper", "AssignNullToNotNullAttribute")]
            public void Execute([NotNull] Interceptors interceptors)
            {
                _logger.LogInfo($"Intercept auto-properties in {_classDefinition}");

                foreach (var property in _classDefinition.Properties)
                {
                    if (property.CustomAttributes.GetAttribute("AutoProperties.InterceptIgnoreAttribute") != null)
                    {
                        _logger.LogDebug($"\tSkip {property.Name} => [InterceptIgnore]");
                        continue;
                    }

                    new PropertyWeaver(this, property).Execute(interceptors);
                }
            }

            private class PropertyWeaver
            {
                [NotNull]
                private readonly ClassWeaver _classWeaver;
                [NotNull]
                private readonly PropertyDefinition _property;
                [NotNull]
                private readonly SystemReferences _systemReferences;
                [NotNull]
                private readonly ModuleDefinition _moduleDefinition;
                [NotNull]
                private readonly ILogger _logger;
                [CanBeNull]
                private FieldDefinition _propertyInfo;

                private bool _isBackingFieldAccessed;

                public PropertyWeaver([NotNull] ClassWeaver classWeaver, [NotNull] PropertyDefinition property)
                {
                    _classWeaver = classWeaver;
                    _property = property;
                    _systemReferences = _classWeaver._weaver._systemReferences;
                    _moduleDefinition = _classWeaver._weaver._moduleDefinition;
                    _logger = _classWeaver._weaver._logger;
                }

                public void Execute([NotNull] Interceptors interceptors)
                {
                    var classDefinition = _classWeaver._classDefinition;

                    // ReSharper disable once AssignNullToNotNullAttribute
                    var backingField = _property.FindAutoPropertyBackingField(classDefinition.Fields);
                    if (backingField == null)
                    {
                        _logger.LogDebug($"\tSkip {_property.Name} => not an auto-property");
                        return;
                    }

                    _logger.LogInfo($"\tIntercept {_property.Name}");
                    _logger.LogDebug($"\t\tGet => {interceptors.GetInterceptor}, Set => {interceptors.SetInterceptor}");

                    var newGetter = BuildGetter(backingField, interceptors.GetInterceptor);
                    var newSetter = BuildSetter(backingField, interceptors.SetInterceptor);

                    if (!_isBackingFieldAccessed)
                    {
                        if (classDefinition.GetConstructors().Any(ctor => ctor.AccessesMember(backingField)))
                        {
                            throw new WeavingException($"The auto-property {_property} is inline initialized and cannot be intercepted.", _property.GetMethod ?? _property.SetMethod);
                        }

                        _logger.LogDebug($"\t\tRemove backing field for {_property.Name}");
                        classDefinition.Fields.Remove(backingField);
                    }
                    else
                    {
                        _logger.LogDebug($"\t\tPreserve backing field for {_property.Name} because an interceptor uses it.");
                    }

                    _property.GetMethod?.Body?.Instructions.Replace(newGetter);
                    _property.SetMethod?.Body?.Instructions.Replace(newSetter);
                }

                [NotNull]
                [SuppressMessage("ReSharper", "AssignNullToNotNullAttribute")]
                [SuppressMessage("ReSharper", "PossibleNullReferenceException")]
                private FieldDefinition PropertyInfo
                {
                    get
                    {
                        if (_propertyInfo != null)
                            return _propertyInfo;

                        _propertyInfo = new FieldDefinition($"<{_property.Name}>k__PropertyInfo", FieldAttributes.InitOnly | FieldAttributes.Private | FieldAttributes.Static | FieldAttributes.CompilerControlled, _classWeaver._weaver._systemReferences.PropertyInfoType);
                        _property.DeclaringType.Fields.Add(_propertyInfo);

                        _classWeaver.StaticConstructor.Body.Instructions.InsertRange(0,
                            Instruction.Create(OpCodes.Ldtoken, _property.DeclaringType),
                            Instruction.Create(OpCodes.Call, _systemReferences.GetTypeFromHandle),
                            Instruction.Create(OpCodes.Ldstr, _property.Name),
                            Instruction.Create(OpCodes.Call, _systemReferences.GetPropertyInfo),
                            Instruction.Create(OpCodes.Stsfld, _propertyInfo));

                        return _propertyInfo;
                    }
                }

                [CanBeNull]
                private TypeReference Import([CanBeNull] TypeReference type) => type == null ? null : _moduleDefinition.ImportReference(type);
                [CanBeNull]
                private MethodReference Import([CanBeNull] MethodReference method) => method == null ? null : _moduleDefinition.ImportReference(method);

                [CanBeNull, ItemNotNull]
                private IEnumerable<Instruction> BuildGetter([NotNull] FieldDefinition backingField, [CanBeNull] MethodDefinition getInterceptor)
                {
                    if (_property.GetMethod == null)
                    {
                        _logger.LogDebug($"\t\tProperty has no getter");
                    }

                    if (getInterceptor == null)
                        throw new WeavingException($"property {_property} has a getter, but the class has no [GetInterceptor].", _property.GetMethod);

                    return BuildInstructions(backingField, getInterceptor, false).ToArray();
                }

                [CanBeNull, ItemNotNull]
                private IEnumerable<Instruction> BuildSetter([NotNull] FieldDefinition backingField, [CanBeNull] MethodDefinition setInterceptor)
                {
                    if (_property.SetMethod == null)
                    {
                        _logger.LogDebug($"\t\tProperty has no setter");
                        return null;
                    }

                    if (setInterceptor == null)
                        throw new WeavingException($"property {_property} has a setter, but the class has no [SetInterceptor].", _property.SetMethod);

                    return BuildInstructions(backingField, setInterceptor, true).ToArray();
                }

                [NotNull, ItemNotNull]
                [SuppressMessage("ReSharper", "AssignNullToNotNullAttribute")]
                private IEnumerable<Instruction> BuildInstructions([NotNull] FieldReference backingField, [NotNull] MethodReference interceptor, bool isSetter)
                {
                    yield return Instruction.Create(OpCodes.Ldarg_0);

                    var propertyType = _property.PropertyType;
                    Debug.Assert(propertyType != null, nameof(propertyType) + " != null");

                    var parameters = interceptor.Parameters;
                    Debug.Assert(parameters != null, nameof(parameters) + " != null");

                    foreach (var parameter in parameters)
                    {
                        Debug.Assert(parameter != null, nameof(parameter) + " != null");

                        var parameterType = parameter.ParameterType;
                        Debug.Assert(parameterType != null, nameof(parameterType) + " != null");

                        // ReSharper disable once PossibleNullReferenceException
                        if (parameterType.IsByReference && parameterType.GetElementType().IsGenericParameter)
                        {
                            _isBackingFieldAccessed = true;
                            yield return Instruction.Create(OpCodes.Ldarg_0);
                            yield return Instruction.Create(OpCodes.Ldflda, backingField);
                        }
                        else if (parameterType.IsGenericParameter)
                        {
                            if (isSetter)
                            {
                                yield return Instruction.Create(OpCodes.Ldarg_1);
                            }
                            else
                            {
                                _isBackingFieldAccessed = true;
                                yield return Instruction.Create(OpCodes.Ldarg_0);
                                yield return Instruction.Create(OpCodes.Ldfld, backingField);
                            }
                        }
                        else
                        {
                            switch (parameterType.FullName)
                            {
                                case "System.String":
                                    yield return Instruction.Create(OpCodes.Ldstr, _property.Name);
                                    break;

                                case "System.Type":
                                    yield return Instruction.Create(OpCodes.Ldtoken, Import(propertyType));
                                    yield return Instruction.Create(OpCodes.Call, _systemReferences.GetTypeFromHandle);
                                    break;

                                case "System.Reflection.PropertyInfo":
                                    // ReSharper disable once ArrangeRedundantParentheses
                                    yield return Instruction.Create(OpCodes.Ldsfld, PropertyInfo);
                                    break;

                                case "System.Reflection.FieldInfo":
                                    _isBackingFieldAccessed = true;
                                    yield return Instruction.Create(OpCodes.Ldtoken, backingField);
                                    yield return Instruction.Create(OpCodes.Call, _systemReferences.GetFieldFromHandle);
                                    break;

                                case "System.Object":
                                    if (isSetter)
                                    {
                                        yield return Instruction.Create(OpCodes.Ldarg_1);
                                    }
                                    else
                                    {
                                        _isBackingFieldAccessed = true;
                                        yield return Instruction.Create(OpCodes.Ldarg_0);
                                        yield return Instruction.Create(OpCodes.Ldfld, backingField);
                                    }

                                    yield return propertyType.IsValueType 
                                        ? Instruction.Create(OpCodes.Box, Import(propertyType)) 
                                        : Instruction.Create(OpCodes.Castclass, Import(parameterType));
                                    break;

                                default:
                                    throw new WeavingException($"A parameter of type {parameterType} in the interceptor {interceptor} is not supported.", interceptor);
                            }
                        }
                    }

                    if (interceptor.ContainsGenericParameter)
                    {
                        // ReSharper disable once PossibleNullReferenceException
                        if (interceptor.GenericParameters.Count != 1)
                            throw new WeavingException($"Only one generic parameter is supported in the interceptor {interceptor}.", interceptor);

                        var generic = new GenericInstanceMethod(Import(interceptor));
                        // ReSharper disable once PossibleNullReferenceException
                        generic.GenericArguments.Add(Import(propertyType));

                        interceptor = generic;
                    }

                    yield return Instruction.Create(OpCodes.Call, Import(interceptor));

                    if (!isSetter && !interceptor.IsGenericInstance)
                    {
                        yield return propertyType.IsValueType ? Instruction.Create(OpCodes.Unbox_Any, Import(propertyType)) : Instruction.Create(OpCodes.Castclass, Import(propertyType));
                    }

                    yield return Instruction.Create(OpCodes.Ret);
                }
            }
        }
    }
}

