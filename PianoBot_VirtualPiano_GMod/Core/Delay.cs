namespace PianoBot_VirtualPiano_GMod.Core
{
    /// <summary>
    /// This class holds information about a delay
    /// It needs a character associated with the delay
    /// as well as a time to define how long the delay lasts
    /// </summary>
    internal class Delay
    {
        public char Character { get; set; }
        public int Time { get; set; }

        public Delay(char character, int time)
        {
            Character = character;
            Time = time;
        }
    }
}
