namespace PianoBot_VirtualPiano_GMod.Core.Models.INote
{
    /// <summary>
    /// This class defines a SpeedChangeNote which is a note
    /// that will change the speed of the delay between notes being played
    /// It needs a boolean value to define wheather or not to turn on
    /// the faster speed
    /// </summary>
    public class SpeedChangeNote : Interfaces.INote
    {
        public bool TurnOnFast { get; }

        public SpeedChangeNote(bool turnOnFast)
        {
            TurnOnFast = turnOnFast;
        }

        public void Play()
        {
            AutoPlayer.ChangeSpeed(TurnOnFast);
        }
    }
}
