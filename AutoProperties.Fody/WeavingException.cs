namespace AutoProperties.Fody
{
    using System;

    using Mono.Cecil;

    internal class WeavingException : Exception
    {
        public WeavingException(string message, MethodReference? method = null)
            : base(message)
        {
            Method = method;
        }

        public MethodReference? Method { get; }
    }
}
