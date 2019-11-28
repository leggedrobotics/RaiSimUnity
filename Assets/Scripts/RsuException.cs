/*
 * MIT License
 * 
 * Copyright (c) 2019, Dongho Kang, Robotics Systems Lab, ETH Zurich
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */

using System;

namespace raisimUnity
{
    public class RsuTcpConnectionException: Exception {
        public RsuTcpConnectionException(string message): base(message) {
        }
    }
    
    public class RsuIdleException: Exception {
        public RsuIdleException(string message): base("Cannot get server status: " + message) {
        }
    }
    
    public class RsuReadXMLException: Exception {
        public RsuReadXMLException(string message): base("Cannot read XML: " + message) {
        }
    }
    
    public class RsuInitObjectsException: Exception {
        public RsuInitObjectsException(string message): base("Cannot initialize objects: " + message) {
        }
    }
    
    public class RsuInitVisualsException: Exception {
        public RsuInitVisualsException(string message): base("Cannot initialize visuals: " + message) {
        }
    }

    public class RsuUpdateObjectsPositionException : Exception
    {
        public RsuUpdateObjectsPositionException(string message): base("Cannot update position: " + message) {
        }
    }
    
    public class RsuUpdateVisualsPositionException : Exception
    {
        public RsuUpdateVisualsPositionException(string message): base("Cannot update visual position: " + message) {
        }
    }
    
    public class RsuUpdateContactsException : Exception
    {
        public RsuUpdateContactsException(string message): base("Cannot update contacts: " + message) {
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