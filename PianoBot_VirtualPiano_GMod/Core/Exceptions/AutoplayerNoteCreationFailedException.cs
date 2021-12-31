using System;

namespace PianoBot_VirtualPiano_GMod.Core.Exceptions
{
    public class AutoplayerNoteCreationFailedException : AutoplayerException
    {
        public AutoplayerNoteCreationFailedException(){}
        public AutoplayerNoteCreationFailedException(string message) : base(message){}
        public AutoplayerNoteCreationFailedException(string message, Exception innerException) : base(message, innerException){}
    }
}
