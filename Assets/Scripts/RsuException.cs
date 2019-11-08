using System;

namespace raisimUnity
{
    public class RsuTcpConnectionException: Exception {
        public RsuTcpConnectionException(string message): base(message) {
        }
    }
    
    public class RsuReadXMLException: Exception {
        public RsuReadXMLException(string message): base("Cannot Read XML: " + message) {
        }
    }
    
    public class RsuInitSceneException: Exception {
        public RsuInitSceneException(string message): base("Cannot Initialize Scene: " + message) {
        }
    }
    
    public class RsuInitVisualsException: Exception {
        public RsuInitVisualsException(string message): base("Cannot Initialize Visuals: " + message) {
        }
    }

    public class RsuUpdateException : Exception
    {
        public RsuUpdateException(string message): base(message) {
        }
    }

    public class RsuFfmepgException : Exception
    {
        public RsuFfmepgException(string message): base(message) {
        }
    }

    public class RsuResourceException : Exception
    {
        public RsuResourceException(string message): base(message) {
        }
    }
}