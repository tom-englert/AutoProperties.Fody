#pragma warning disable CCRSI_ContractForNotNull // Element with [NotNull] attribute does not have a corresponding not-null contract.
#pragma warning disable CCRSI_CreateContractInvariantMethod // Missing Contract Invariant Method.
// ReSharper disable AssignNullToNotNullAttribute
// ReSharper disable PossibleNullReferenceException
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global

using System;

using AutoProperties.Fody;

using JetBrains.Annotations;

using Mono.Cecil;
using Mono.Cecil.Cil;

public class ModuleWeaver : ILogger
{
    private bool _hasErrors;

    // Will log an informational message to MSBuild
    public Action<string> LogDebug { get; set; }
    public Action<string> LogInfo { get; set; }
    public Action<string> LogWarning { get; set; }
    public Action<string> LogError { get; set; }
    public Action<string, SequencePoint> LogErrorPoint { get; set; }

    // An instance of Mono.Cecil.ModuleDefinition for processing
    public ModuleDefinition ModuleDefinition { get; set; }

    // Init logging delegates to make testing easier
    public ModuleWeaver()
    {
        LogDebug = LogInfo = LogWarning = LogError = _ => { };
        LogErrorPoint = (_, __) => { };
    }

    public void Execute()
    {
        ModuleDefinition.VisitAllMethods(this);

        if (!_hasErrors)
            ModuleDefinition.RemoveReferences(this);
    }

    void ILogger.LogDebug([NotNull] string message)
    {
        LogDebug(message);
    }

    void ILogger.LogInfo([NotNull] string message)
    {
        LogInfo(message);
    }

    void ILogger.LogWarning([NotNull] string message)
    {
        LogWarning(message);
    }

    void ILogger.LogError([NotNull] string message, [CanBeNull] SequencePoint sequencePoint)
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
}