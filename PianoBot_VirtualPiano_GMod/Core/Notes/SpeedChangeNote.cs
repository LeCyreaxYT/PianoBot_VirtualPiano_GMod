using PianoBot_VirtualPiano_GMod.Core.Interfaces;

namespace PianoBot_VirtualPiano_GMod.Core.Notes
{
    /// <summary>
    /// This class defines a SpeedChangeNote which is a note
    /// that will change the speed of the delay between notes being played
    /// It needs a boolean value to define wheather or not to turn on
    /// the faster speed
    /// </summary>
    public class SpeedChangeNote : INote
    {
        public bool TurnOnFast { get; }

        public SpeedChangeNote(bool turnOnFast)
        {
            TurnOnFast = turnOnFast;
        }

        public void Play()
        {
            //AutoPlayer.ChangeSpeed(TurnOnFast);
        }
    }
}
