using System;

namespace raisimUnity
{
    public class RsuTcpConnectionException: Exception {
        public RsuTcpConnectionException(string message): base(message) {
        }
    }
    
    public class RsuInitException: Exception {
        public RsuInitException(string message): base(message) {
        }
    }
}