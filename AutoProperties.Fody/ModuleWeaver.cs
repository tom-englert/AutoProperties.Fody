// ReSharper disable AssignNullToNotNullAttribute
// ReSharper disable PossibleNullReferenceException
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global

using System.Collections.Generic;
using System.Linq;

using AutoProperties.Fody;

using Fody;

using JetBrains.Annotations;

using Mono.Cecil;
using Mono.Cecil.Cil;

public class ModuleWeaver : BaseModuleWeaver, ILogger
{
    public override void Execute()
    {
        // System.Diagnostics.Debugger.Launch();

        var systemReferences = new SystemReferences(this);

        new PropertyAccessorWeaver(this, systemReferences).Execute();
        new BackingFieldAccessWeaver(ModuleDefinition, this).Execute();

        CleanReferences();
    }

    public override IEnumerable<string> GetAssembliesForScanning()
    {
        return new[] { "mscorlib", "System", "System.Reflection", "System.Runtime", "netstandard" };
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
            return method.Resolve().ReadSequencePoints(ModuleDefinition.SymbolReader)?.FirstOrDefault();
        }
        catch
        {
            return null;
        }
    }
}