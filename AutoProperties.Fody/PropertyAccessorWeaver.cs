namespace AutoProperties.Fody
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    using FodyTools;

    using Mono.Cecil;
    using Mono.Cecil.Cil;
    using Mono.Cecil.Rocks;

    using FieldAttributes = Mono.Cecil.FieldAttributes;
    using MethodAttributes = Mono.Cecil.MethodAttributes;

    internal class PropertyAccessorWeaver
    {
        private readonly ModuleDefinition _moduleDefinition;
        private readonly SystemReferences _systemReferences;
        private readonly ILogger _logger;

        public PropertyAccessorWeaver(ModuleWeaver moduleWeaver, SystemReferences systemReferences)
        {
            _logger = moduleWeaver;
            _moduleDefinition = moduleWeaver.ModuleDefinition;
            _systemReferences = systemReferences;
        }

        public void Execute()
        {
            try
            {
                var allTypes = _moduleDefinition.GetTypes();

                var allClasses = allTypes
                    .Where(type => type != null && type.IsClass && (type.BaseType != null));

                var allInterceptors = new Dictionary<TypeReference, Interceptors>(TypeReferenceEqualityComparer.Default);

                foreach (var classDefinition in allClasses.SelectMany(item => item.GetSelfAndBaseTypes().Reverse()))
                {
                    if (allInterceptors.ContainsKey(classDefinition))
                        continue;

                    try
                    {
                        var baseType = classDefinition.BaseType;
                        if (baseType?.IsGenericInstance == true)
                            baseType = baseType.GetElementType();

                        allInterceptors.Add(classDefinition, new Interceptors(this, classDefinition, allInterceptors.GetValueOrDefault(baseType)));
                    }
                    catch (WeavingException ex)
                    {
                        _logger.LogError(ex.Message, ex.Method);
                    }
                }

                foreach (var interceptors in allInterceptors.Values.Where(item => (item.ClassDefinition.Module == _moduleDefinition)))
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
            private readonly PropertyAccessorWeaver _weaver;
            private readonly Interceptors? _baseTypeInterceptors;
            private readonly MethodDefinition? _setInterceptor;
            private readonly MethodDefinition? _getInterceptor;

            public Interceptors(PropertyAccessorWeaver weaver, TypeReference classDefinition, Interceptors? baseTypeInterceptors)
            {
                _weaver = weaver;
                ClassDefinition = classDefinition.Resolve();
                _baseTypeInterceptors = baseTypeInterceptors;

                var allMethods = ClassDefinition.Methods;
                if (allMethods == null)
                    return;

                var getInterceptors = allMethods.Where(m => m?.CustomAttributes?.GetAttribute(AttributeNames.GetInterceptor) != null).ToArray();
                if (getInterceptors.Length > 1)
                    throw new WeavingException($"Multiple [GetInterceptor] attributed methods found in class {classDefinition}.", getInterceptors[1]);

                _getInterceptor = getInterceptors.FirstOrDefault();

                var setInterceptors = allMethods.Where(m => m?.CustomAttributes?.GetAttribute(AttributeNames.SetInterceptor) != null).ToArray();
                if (setInterceptors.Length > 1)
                    throw new WeavingException($"Multiple [SetInterceptor] attributed methods found in class {classDefinition}.", setInterceptors[1]);

                _setInterceptor = setInterceptors.FirstOrDefault();

                VerifyInterceptors();
            }

            public TypeDefinition ClassDefinition { get; }

            public MethodDefinition? SetInterceptor => _setInterceptor ?? WhenAccessibleInDerivedClass(_baseTypeInterceptors?.SetInterceptor);

            public MethodDefinition? GetInterceptor => _getInterceptor ?? WhenAccessibleInDerivedClass(_baseTypeInterceptors?.GetInterceptor);

            public void Execute()
            {
                new ClassWeaver(_weaver, ClassDefinition).Execute(this);
            }

            private void VerifyInterceptors()
            {
                VerifyGetInterceptor();
                VerifySetInterceptor();
            }

            private void VerifySetInterceptor()
            {
                if (_setInterceptor == null)
                    return;

                if (_setInterceptor.ReturnType?.FullName != "System.Void")
                {
                    throw new WeavingException($"The set interceptor of class {ClassDefinition} must not return a value.", _setInterceptor);
                }
            }

            private void VerifyGetInterceptor()
            {
                if (_getInterceptor == null)
                    return;

                var returnType = _getInterceptor.ReturnType;
                var genericParameter = _getInterceptor.GenericParameters?.FirstOrDefault();

                if (genericParameter != null)
                {
                    if (returnType?.GetElementType() != genericParameter)
                    {
                        throw new WeavingException($"The return type of the generic get interceptor of class {ClassDefinition} must be {genericParameter.Name}.", _getInterceptor);
                    }
                }
                else
                {
                    if (returnType?.FullName != "System.Object")
                    {
                        throw new WeavingException($"The return type of the get interceptor of class {ClassDefinition} must be System.Object.", _getInterceptor);
                    }
                }
            }

            private MethodDefinition? WhenAccessibleInDerivedClass(MethodDefinition? baseMethodDefinition)
            {
                if (baseMethodDefinition == null)
                    return null;

                if (baseMethodDefinition.IsPrivate)
                {
                    _weaver._logger.LogWarning($"{baseMethodDefinition} is not accessible from {ClassDefinition}, no properties will be intercepted.");
                    return null;
                }

                return baseMethodDefinition;
            }
        }

        private class ClassWeaver
        {
            private readonly PropertyAccessorWeaver _weaver;
            private readonly TypeDefinition _classDefinition;
            private readonly ILogger _logger;

            public ClassWeaver(PropertyAccessorWeaver weaver, TypeDefinition classDefinition)
            {
                _weaver = weaver;
                _classDefinition = classDefinition;
                _logger = _weaver._logger;
            }

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

            public void Execute(Interceptors interceptors)
            {
                var getInterceptor = interceptors.GetInterceptor;
                var setInterceptor = interceptors.SetInterceptor;

                if ((getInterceptor == null) && (setInterceptor == null))
                    return;

                _logger.LogInfo($"Intercept auto-properties in {_classDefinition}");
                _logger.LogDebug($"\tGet => {getInterceptor}, Set => {setInterceptor}");

                foreach (var property in _classDefinition.Properties)
                {
                    if (property.CustomAttributes.GetAttribute(AttributeNames.InterceptIgnore) != null)
                    {
                        _logger.LogInfo($"\tSkip {property.Name}, has [InterceptIgnore]");
                        continue;
                    }


                    try
                    {
                        new PropertyWeaver(this, property).Execute(getInterceptor!, setInterceptor!);
                    }
                    catch (WeavingException ex)
                    {
                        _logger.LogError($"Error intercepting {property}: {ex.Message}", ex.Method);
                    }
                }
            }

            private class PropertyWeaver
            {
                private readonly ClassWeaver _classWeaver;
                private readonly PropertyDefinition _property;
                private readonly SystemReferences _systemReferences;
                private readonly ModuleDefinition _moduleDefinition;
                private readonly ILogger _logger;
                private readonly TypeDefinition _classDefinition;
                private FieldDefinition? _propertyInfo;

                private bool _isBackingFieldAccessed;

                public PropertyWeaver(ClassWeaver classWeaver, PropertyDefinition property)
                {
                    _classWeaver = classWeaver;
                    _property = property;
                    _systemReferences = _classWeaver._weaver._systemReferences;
                    _moduleDefinition = _classWeaver._weaver._moduleDefinition;
                    _logger = _classWeaver._weaver._logger;
                    _classDefinition = _classWeaver._classDefinition;
                }

                public void Execute(MethodDefinition getInterceptor, MethodDefinition setInterceptor)
                {
                    var backingField = _property.FindAutoPropertyBackingField(_classDefinition.Fields);
                    if (backingField == null)
                    {
                        _logger.LogInfo($"\tSkip {_property.Name}, not an auto-property");
                        return;
                    }

                    _logger.LogInfo($"\tIntercept {_property.Name}");

                    var newGetter = BuildGetter(backingField, getInterceptor);
                    var newSetter = BuildSetter(backingField, setInterceptor);

                    foreach (var constructor in _classDefinition.GetConstructors())
                    {
                        constructor.ReplaceFieldAccessWithPropertySetter(backingField, _property, _classWeaver._weaver._moduleDefinition.SymbolReader);
                    }

                    if (!_isBackingFieldAccessed)
                    {
                        if (_classDefinition.GetConstructors().Any(ctor => ctor.AccessesMember(backingField)))
                        {
                            throw new WeavingException($"The auto-property {_property} is inline initialized and cannot be intercepted.", _property.GetMethod ?? _property.SetMethod);
                        }

                        _logger.LogDebug($"\t\tRemove backing field for {_property.Name}");
                        _classDefinition.Fields.Remove(backingField);
                    }
                    else
                    {
                        _logger.LogDebug($"\t\tPreserve backing field for {_property.Name} because an interceptor uses it.");
                    }

                    _property.GetMethod?.Body?.Instructions.Replace(newGetter);
                    _property.SetMethod?.Body?.Instructions.Replace(newSetter);
                }

                private FieldDefinition PropertyInfo
                {
                    get
                    {
                        if (_propertyInfo != null)
                            return _propertyInfo;

                        _propertyInfo = new FieldDefinition($"<{_property.Name}>k__PropertyInfo", FieldAttributes.InitOnly | FieldAttributes.Private | FieldAttributes.Static | FieldAttributes.CompilerControlled, _classWeaver._weaver._systemReferences.PropertyInfoType);

                        var declaringType = _property.DeclaringType;
                        declaringType.Fields.Add(_propertyInfo);

                        var getPropertyInfo = _systemReferences.GetPropertyInfo;

                        if (getPropertyInfo == null)
                            throw new WeavingException("The PropertyInfo parameter is not supported in the current framework.");

                        _classWeaver.StaticConstructor.Body.Instructions.InsertRange(0,
                            Instruction.Create(OpCodes.Ldtoken, declaringType.GetReference()),
                            Instruction.Create(OpCodes.Call, _systemReferences.GetTypeFromHandle),
                            Instruction.Create(OpCodes.Ldstr, _property.Name),
                            Instruction.Create(OpCodes.Ldc_I4, (int)(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly)),
                            Instruction.Create(OpCodes.Call, getPropertyInfo),
                            Instruction.Create(OpCodes.Stsfld, _propertyInfo.GetReference()));

                        return _propertyInfo;
                    }
                }

                private TypeReference? Import(TypeReference? type) => type == null ? null : _moduleDefinition.ImportReference(type);

                private IEnumerable<Instruction>? BuildGetter(FieldDefinition backingField, MethodDefinition? getInterceptor)
                {
                    var getMethod = _property.GetMethod;
                    if (getMethod == null)
                    {
                        _logger.LogDebug($"\t\tProperty has no getter");
                        return null;
                    }

                    if (getInterceptor == null)
                        throw new WeavingException($"property {_property} has a getter, but the class has no [GetInterceptor].", getMethod);

                    _logger.LogDebug($"\t\tIntercept getter");
                    return BuildInstructions(backingField, getInterceptor, false).ToArray();
                }

                private IEnumerable<Instruction>? BuildSetter(FieldDefinition backingField, MethodDefinition? setInterceptor)
                {
                    var setMethod = _property.SetMethod;
                    if (setMethod == null)
                    {
                        _logger.LogDebug($"\t\tProperty has no setter");
                        return null;
                    }

                    if (setInterceptor == null)
                        throw new WeavingException($"property {_property} has a setter, but the class has no [SetInterceptor].", setMethod);

                    _logger.LogDebug($"\t\tIntercept setter");
                    return BuildInstructions(backingField, setInterceptor, true).ToArray();
                }

                private IEnumerable<Instruction> BuildInstructions(FieldDefinition backingField, MethodDefinition interceptor, bool isSetter)
                {
                    yield return Instruction.Create(OpCodes.Ldarg_0);

                    var propertyType = _property.PropertyType;
                    var parameters = interceptor.Parameters;

                    foreach (var parameter in parameters)
                    {
                        var parameterType = parameter.ParameterType;

                        if (parameterType.IsByReference && parameterType.GetElementType().IsGenericParameter)
                        {
                            _isBackingFieldAccessed = true;
                            yield return Instruction.Create(OpCodes.Ldarg_0);
                            yield return Instruction.Create(OpCodes.Ldflda, backingField.GetReference());
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
                                yield return Instruction.Create(OpCodes.Ldfld, backingField.GetReference());
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
                                    yield return Instruction.Create(OpCodes.Ldsfld, PropertyInfo.GetReference());
                                    break;

                                case "System.Reflection.FieldInfo":
                                    _isBackingFieldAccessed = true;
                                    yield return Instruction.Create(OpCodes.Ldtoken, backingField.GetReference());
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
                                        yield return Instruction.Create(OpCodes.Ldfld, backingField.GetReference());
                                    }

                                    if (propertyType.IsGenericParameter || propertyType.IsValueType)
                                    {
                                        // we only need to box for value types or if type is generic (no cast for reference)
                                        yield return Instruction.Create(OpCodes.Box, Import(propertyType));
                                    }

                                    break;

                                default:
                                    throw new WeavingException($"A parameter of type {parameterType} in the interceptor {interceptor} is not supported.", interceptor);
                            }
                        }
                    }

                    if (interceptor.ContainsGenericParameter)
                    {
                        if (interceptor.GenericParameters.Count != 1)
                            throw new WeavingException($"Only one generic parameter is supported in the interceptor {interceptor}.", interceptor);

                        var generic = new GenericInstanceMethod(interceptor.GetReference(_classDefinition));
                        generic.GenericArguments.Add(Import(propertyType));

                        yield return Instruction.Create(OpCodes.Call, generic);
                    }
                    else
                    {
                        yield return Instruction.Create(OpCodes.Call, interceptor.GetReference(_classDefinition));

                        if (!isSetter)
                        {
                            yield return propertyType.IsValueType ? Instruction.Create(OpCodes.Unbox_Any, Import(propertyType)) : Instruction.Create(OpCodes.Castclass, Import(propertyType));
                        }
                    }

                    yield return Instruction.Create(OpCodes.Ret);
                }
            }
        }
    }
}

