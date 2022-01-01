using System;

namespace PianoBot_VirtualPiano_GMod.Core.Exceptions
{
    /// <summary>
    /// This exception is thrown if you are interacting with a custom delay in a wrong way
    /// </summary>
    public class AutoPlayerCustomDelayException : AutoplayerException
    {
        public AutoPlayerCustomDelayException(){}
        public AutoPlayerCustomDelayException(string message) : base(message){}
        public AutoPlayerCustomDelayException(string message, Exception innerException) : base(message, innerException){}
    }
}
