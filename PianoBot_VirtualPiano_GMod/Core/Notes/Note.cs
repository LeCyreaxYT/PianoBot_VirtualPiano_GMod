using System.Threading;
using PianoBot_VirtualPiano_GMod.Core.Interfaces;
using WindowsInput;
using WindowsInput.Native;

namespace PianoBot_VirtualPiano_GMod.Core.Notes
{
    public class Note : INote
    {
        private readonly InputSimulator _sim = new();

        public VirtualKeyCode NoteToPlay { get; }
        public char Character { get; }
        private bool IsHighNote { get; }

        public Note(char character, VirtualKeyCode note, bool isHighNote)
        {
            NoteToPlay = note;
            Character = character;
            IsHighNote = isHighNote;
        }

        public void Play()
        {
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
