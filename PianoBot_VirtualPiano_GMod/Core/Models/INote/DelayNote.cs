using System.Threading;

namespace PianoBot_VirtualPiano_GMod.Core.Models.INote
{
    /// <summary>
    /// This class defines a DelayNote
    /// A DelayNote is a note that extends the default delay
    /// Do note that this delay will be added to two times the default delay
    /// The reason for this is that a default delay is added after each note
    /// including a delay note
    /// </summary>
    public class DelayNote : Interfaces.INote
    {
        public char Character { get; }
        private int Time { get; }

        public DelayNote(char character, int delayTime)
        {
            Character = character;
            Time = delayTime;
        }

        public void Play()
        {
            Thread.Sleep(Time);
        }
    }
}
