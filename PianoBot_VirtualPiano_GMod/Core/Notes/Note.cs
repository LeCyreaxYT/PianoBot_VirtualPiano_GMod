using System.Threading;
using PianoBot_VirtualPiano_GMod.Core.Interfaces;
using WindowsInput;
using WindowsInput.Native;

namespace PianoBot_VirtualPiano_GMod.Core.Notes
{
    /// <summary>
    /// This class defines a Note
    /// This is where the behaviour of the note is defined
    /// The Key property holds information about which key
    /// on the keyboard the note corrosponds to
    /// </summary>
    public class Note : INote
    {
        private readonly InputSimulator _sim = new InputSimulator();

        public VirtualKeyCode NoteToPlay { get; private set; }
        public char Character { get; }
        public bool IsHighNote { get; private set; }

        public Note(char character, VirtualKeyCode note, bool isHighNote)
        {
            NoteToPlay = note;
            Character = character;
            IsHighNote = isHighNote;
        }

        public void Play()
        {
            //This method is used until a better solution is found. This will NOT play black keys :(
            //SendKeys.SendWait(Character.ToString());

            //EXPERIMENTAL SOLUTION
            //This method is a better solution as you can define a delay. However, this may result in unexpected behaviour should the program be terminated before KeyUp is run!
            if (IsHighNote)
            {
                _sim.Keyboard.KeyDown(VirtualKeyCode.LSHIFT);
                Thread.Sleep(50);
                
                _sim.Keyboard.KeyDown(NoteToPlay);
                Thread.Sleep(50);
                
                _sim.Keyboard.KeyUp(NoteToPlay);
                _sim.Keyboard.KeyPress(VirtualKeyCode.LSHIFT);
            }
            else
            {
                _sim.Keyboard.KeyDown(NoteToPlay);
                Thread.Sleep(50);
                _sim.Keyboard.KeyUp(NoteToPlay);
            }
        }

        public void Stop()
        {
            _sim.Keyboard.KeyPress(VirtualKeyCode.LSHIFT);
            _sim.Keyboard.KeyUp(NoteToPlay);
        }

        public override string ToString()
        {
            return Character.ToString();
        }

        public override bool Equals(object obj)
        {
            if (obj is not Note other) return false;
            return Character == other.Character && IsHighNote == other.IsHighNote;
        }

        public override int GetHashCode()
        {
            return Character.GetHashCode();
        }
    }
}
