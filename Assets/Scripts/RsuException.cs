using System;

namespace raisimUnity
{
    public class RsuTcpConnectionException: Exception {
        public RsuTcpConnectionException(string message): base(message) {
        }
    }
    
    public class RsuReadXMLException: Exception {
        public RsuReadXMLException(string message): base("Cannot read XML: " + message) {
        }
    }
    
    public class RsuInitSceneException: Exception {
        public RsuInitSceneException(string message): base("Cannot initialize scene: " + message) {
        }
    }
    
    public class RsuInitVisualsException: Exception {
        public RsuInitVisualsException(string message): base("Cannot initialize visuals: " + message) {
        }
    }

    public class RsuUpdateObjectsPositionException : Exception
    {
        public RsuUpdateObjectsPositionException(string message): base("Cannot update position" + message) {
        }
    }
    
    public class RsuUpdateVisualsPositionException : Exception
    {
        public RsuUpdateVisualsPositionException(string message): base("Cannot update visual position" + message) {
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