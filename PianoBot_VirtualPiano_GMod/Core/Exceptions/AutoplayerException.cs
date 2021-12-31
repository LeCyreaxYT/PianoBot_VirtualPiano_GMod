using System;

namespace PianoBot_VirtualPiano_GMod.Core.Exceptions
{
    /// <summary>
    /// This is the main exception class for the program
    /// All exceptions inherents from this
    /// </summary>
    public class AutoplayerException : Exception
    {
        public AutoplayerException(){}
        public AutoplayerException(string message) : base(message){}
        public AutoplayerException(string message, Exception innerException) : base(message, innerException){}
    }
}
