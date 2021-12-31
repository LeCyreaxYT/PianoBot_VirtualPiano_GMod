using System;

namespace PianoBot_VirtualPiano_GMod.Core.Exceptions
{
    /// <summary>
    /// This exception is thrown if you are interacting with a custom delay in a wrong way
    /// </summary>
    public class AutoplayerCustomDelayException : AutoplayerException
    {
        public AutoplayerCustomDelayException(){}
        public AutoplayerCustomDelayException(string message) : base(message){}
        public AutoplayerCustomDelayException(string message, Exception innerException) : base(message, innerException){}
    }
}
