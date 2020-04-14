using System;
using System.IO;
using System.Linq;
using System.Reflection;

using Fody;

namespace Tests
{
    public static class ExtensionMethods
    {
        public static dynamic GetInstance(this Assembly assembly, string className, params object[] args)
        {
            var type = assembly.GetType(className, true);

            return Activator.CreateInstance(type, args);
        }

        public static string FormatError(this SequencePointMessage error)
        {
            var message = error.Text;
            var sequencePoint = error.SequencePoint;

            if (sequencePoint != null)
            {
                message = message + $"\r\n\t({sequencePoint.Document.Url}@{sequencePoint.StartLine}:{sequencePoint.StartColumn}\r\n\t => {File.ReadAllLines(sequencePoint.Document.Url).Skip(sequencePoint.StartLine - 1).FirstOrDefault()}";
            }

            return message;
        }

        public static string FormatMessage(this LogMessage message)
        {
            switch (message.MessageImportance.ToString())
            {
                case "Low":
                    return "D: " + message.Text;

                case "Normal":
                    return "I: " + message.Text;

                default:
                    throw new NotImplementedException();
            }
        }
    }
}
