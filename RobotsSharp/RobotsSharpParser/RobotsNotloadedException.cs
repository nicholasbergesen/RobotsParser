using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace RobotsSharpParser
{
    public class RobotsNotloadedException : Exception
    {
        public RobotsNotloadedException()
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
