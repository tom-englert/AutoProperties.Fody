using System;

using AutoProperties.Fody;

using Mono.Cecil;

public class ModuleWeaver : ILogger
{
    // Will log an informational message to MSBuild
    public Action<string> LogDebug { get; set; }
    public Action<string> LogInfo { get; set; }
    public Action<string> LogWarning { get; set; }
    public Action<string> LogError { get; set; }

    // An instance of Mono.Cecil.ModuleDefinition for processing
    public ModuleDefinition ModuleDefinition { get; set; }

    // Init logging delegates to make testing easier
    public ModuleWeaver()
    {
        LogDebug = LogInfo = LogWarning = LogError = _ => { };
    }

    public void Execute()
    {
        ModuleDefinition.VisitAllMethods(this);
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

    void ILogger.LogError(string message)
    {
        LogError(message);
    }
}