using System;

namespace PianoBot_VirtualPiano_GMod.Core.Exceptions
{
    /// <summary>
    /// This exception is thrown if you are interacting with a custom note in a wrong way
    /// </summary>
    public class AutoPlayerCustomNoteException : AutoplayerException
    {
        public AutoPlayerCustomNoteException() { }
        public AutoPlayerCustomNoteException(string message) : base(message) { }
        public AutoPlayerCustomNoteException(string message, Exception innerException) : base(message, innerException) { }
    }
}
