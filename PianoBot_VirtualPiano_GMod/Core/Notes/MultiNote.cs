using System.Collections.Generic;
using System.Threading;
using PianoBot_VirtualPiano_GMod.Core.Interfaces;
using WindowsInput;
using WindowsInput.Native;

namespace PianoBot_VirtualPiano_GMod.Core.Notes
{
    /// <summary>
    /// This class defines a MultiNote
    /// A MultiNote is a collection of notes to be played at once
    /// or, at least close to "at once".
    /// </summary>
    public class MultiNote : INote
    {
        private InputSimulator sim = new InputSimulator();

        public Note[] Notes { get; private set; }
        public bool IsHighNote { get; private set; }

        public MultiNote(Note[] notes, bool isHighNote)
        {
            Notes = notes;
            IsHighNote = isHighNote;
        }

        private static readonly List<char> AlwaysHighNotes = new List<char>()
        {
            ' ',
            '!',
            '@',
            '$',
            '%',
            '^',
            '*',
            '(',
        };
        
        public void Play()
        {
            foreach (var note in Notes)
            {
                bool isHighNote = char.IsUpper(note.Character) || AlwaysHighNotes.Contains(note.Character);

                switch (isHighNote)
                {
                    case true:
                        sim.Keyboard.KeyDown(VirtualKeyCode.LSHIFT);
                        Thread.Sleep(10);

                        sim.Keyboard.KeyDown(note.NoteToPlay);
                        Thread.Sleep(30);

                        sim.Keyboard.KeyUp(note.NoteToPlay);
                        sim.Keyboard.KeyUp(VirtualKeyCode.LSHIFT);
                        continue;
                    case false:
                        sim.Keyboard.KeyDown(note.NoteToPlay);
                        Thread.Sleep(40);

                        sim.Keyboard.KeyUp(note.NoteToPlay);
                        continue;
                }
            }
        }
    }
}
