using System;
using System.Runtime.Serialization;

namespace RobotsSharpParser
{
    public class RobotsNotloadedException : Exception
    {
        public RobotsNotloadedException() : base("Please call Load or LoadAsync.")
        {
        }

        public RobotsNotloadedException(string message) : base(message)
        {
        }

        public RobotsNotloadedException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected RobotsNotloadedException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
