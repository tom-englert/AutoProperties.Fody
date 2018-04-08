// ReSharper disable AssignNullToNotNullAttribute
// ReSharper disable PossibleNullReferenceException
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global

using System;
using System.Collections.Generic;
using System.Linq;

using AutoProperties.Fody;

using Fody;

using JetBrains.Annotations;

using Mono.Cecil;
using Mono.Cecil.Cil;

public class ModuleWeaver : BaseModuleWeaver, ILogger
{
    private bool _hasErrors;

    [NotNull]
    internal SystemReferences SystemReferences => new SystemReferences(ModuleDefinition);

    public ModuleWeaver()
    {
        LogDebug = LogInfo = LogWarning = LogError = _ => { };
        LogErrorPoint = (_, __) => { };
        ModuleDefinition = ModuleDefinition.CreateModule("empty", ModuleKind.Dll);
    }

    public override void Execute()
    {
        new PropertyAccessorWeaver(this).Execute();
        new BackingFieldAccessWeaver(ModuleDefinition, this).Execute();

        CleanReferences();
    }

    public override IEnumerable<string> GetAssembliesForScanning()
    {
        yield break;
    }

    public override bool ShouldCleanReference => true;

    private void CleanReferences()
    {
        new ReferenceCleaner(ModuleDefinition, this).RemoveAttributes();
    }

    void ILogger.LogDebug(string message)
    {
        LogDebug(message);
    }

    void ILogger.LogInfo(string message)
    {
        LogInfo(message);
    }

    void ILogger.LogWarning(string message)
    {
        LogWarning(message);
    }

    void ILogger.LogError(string message, SequencePoint sequencePoint)
    {
        _hasErrors = true;

        if (sequencePoint != null)
        {
            LogErrorPoint(message, sequencePoint);
        }
        else
        {
            LogError(message);
        }
    }

    void ILogger.LogError(string message, MethodReference method)
    {
        if (method == null)
            LogError(message);
        else
        {
            ((ILogger)this).LogError(message, GetFirstSequencePoint(method));
        }
    }

    [CanBeNull]
    private SequencePoint GetFirstSequencePoint([NotNull] MethodReference method)
    {
        try
        {
            return ModuleDefinition.SymbolReader?.Read(method.Resolve())?.SequencePoints?.FirstOrDefault();
        }
        catch
        {
            return null;
        }
    }
}