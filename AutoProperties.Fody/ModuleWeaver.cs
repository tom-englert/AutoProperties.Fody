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
    [NotNull]
    public Action<string> LogDebug { get; set; }
    [NotNull]
    public Action<string> LogInfo { get; set; }
    [NotNull]
    public Action<string> LogWarning { get; set; }
    [NotNull]
    public Action<string> LogError { get; set; }
    [NotNull]
    public Action<string, SequencePoint> LogErrorPoint { get; set; }

    // An instance of Mono.Cecil.ModuleDefinition for processing
    [NotNull]
    public ModuleDefinition ModuleDefinition { get; set; }

    public ModuleWeaver()
    {
        LogDebug = LogInfo = LogWarning = LogError = _ => { };
        LogErrorPoint = (_, __) => { };
        ModuleDefinition = ModuleDefinition.CreateModule("empty", ModuleKind.Dll);
    }

    public void Execute()
    {
        ModuleDefinition.VisitAllMethods(this);

        if (!_hasErrors)
            ModuleDefinition.RemoveReferences(this);
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
}