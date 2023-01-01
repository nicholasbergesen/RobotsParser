using System;
using System.Runtime.Serialization;

namespace RobotsParser
{
    public class RobotsNotloadedException : Exception
    {
        public RobotsNotloadedException() : base("Please call LoadRobotsFromUrl or LoadRobotsContent.")
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
