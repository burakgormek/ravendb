using System;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace Raven.NewClient.Abstractions.FileSystem
{
    public class SynchronizationException : Exception
    {
        public SynchronizationException()
        {
        }

        public SynchronizationException(string message)
            : base(message)
        { }

        public SynchronizationException(string message, Exception inner)
            : base(message, inner)
        { }
    }
}
