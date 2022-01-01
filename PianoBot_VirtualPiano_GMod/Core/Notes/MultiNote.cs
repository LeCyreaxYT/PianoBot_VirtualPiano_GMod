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
        private readonly InputSimulator _sim = new InputSimulator();

        public Note[] Notes { get; set; }
        private bool IsHighNote { get; set; }

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
                        _sim.Keyboard.KeyDown(VirtualKeyCode.LSHIFT);
                        Thread.Sleep(10);

                        _sim.Keyboard.KeyDown(note.NoteToPlay);
                        Thread.Sleep(30);

                        _sim.Keyboard.KeyUp(note.NoteToPlay);
                        _sim.Keyboard.KeyUp(VirtualKeyCode.LSHIFT);
                        continue;
                    case false:
                        _sim.Keyboard.KeyDown(note.NoteToPlay);
                        Thread.Sleep(40);

                        _sim.Keyboard.KeyUp(note.NoteToPlay);
                        continue;
                }
            }
        }
    }
}
