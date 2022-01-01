﻿using System;

namespace PianoBot_VirtualPiano_GMod.Core.Exceptions
{
    /// <summary>
    /// This exception is thrown if a MultiNote end has been reached without having a start definition
    /// </summary>
    public class AutoplayerMultiNoteNotDefinedException : AutoplayerNoteCreationFailedException
    {
        public AutoplayerMultiNoteNotDefinedException(){}
        public AutoplayerMultiNoteNotDefinedException(string message) : base(message){}
        public AutoplayerMultiNoteNotDefinedException(string message, Exception innerException) : base(message, innerException){}
    }
}
