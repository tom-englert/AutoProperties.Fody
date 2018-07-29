using System;
using System.IO;
using System.Linq;
using System.Reflection;

using Fody;

using JetBrains.Annotations;

namespace Tests
{
    public static class ExtensionMethods
    {
        [NotNull]
        public static dynamic GetInstance([NotNull] this Assembly assembly, [NotNull] string className, [NotNull, ItemNotNull] params object[] args)
        {
            var type = assembly.GetType(className, true);

            // ReSharper disable AssignNullToNotNullAttribute
            return Activator.CreateInstance(type, args);
        }

        [NotNull]
        public static string FormatError([NotNull] this SequencePointMessage error)
        {
            var message = error.Text;
            var sequencePoint = error.SequencePoint;

            if (sequencePoint != null)
            {
                message = message + $"\r\n\t({sequencePoint.Document.Url}@{sequencePoint.StartLine}:{sequencePoint.StartColumn}\r\n\t => {File.ReadAllLines(sequencePoint.Document.Url).Skip(sequencePoint.StartLine - 1).FirstOrDefault()}";
            }

            return message;
        }

        [NotNull]
        public static string FormatMessage([NotNull] this LogMessage message)
        {
            switch (message.MessageImportance?.ToString())
            {
                case "Normal":
                    return "D: " + message.Text;

                case "High":
                    return "I: " + message.Text;

                default:
                    throw new NotImplementedException();
            }
        }
    }
}
