using JetBrains.Annotations;

using Mono.Cecil;
using Mono.Cecil.Cil;

namespace AutoProperties.Fody
{
    internal interface ILogger
    {
        void LogDebug([CanBeNull] string message);
        void LogInfo([CanBeNull] string message);
        void LogWarning([CanBeNull] string message);
        void LogError([CanBeNull] string message, [CanBeNull] SequencePoint sequencePoint = null);
        void LogError([CanBeNull] string message, [CanBeNull] MethodReference method);
    }
}
