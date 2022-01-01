using System;

namespace PianoBot_VirtualPiano_GMod.Core.Exceptions
{
    public class AutoplayerTargetNotFoundException : AutoplayerException
    {
        public AutoplayerTargetNotFoundException(){}
        public AutoplayerTargetNotFoundException(string message) : base(message){}
        public AutoplayerTargetNotFoundException(string message, Exception innerException) : base(message, innerException){}
    }
}
