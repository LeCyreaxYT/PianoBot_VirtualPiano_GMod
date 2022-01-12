using System.Collections.Generic;
using System.Threading.Tasks;
using WindowsInput;
using WindowsInput.Native;

namespace PianoBot_VirtualPiano_GMod.Core.Models.INote
{
    /// <summary>
    /// This class defines a MultiNote
    /// A MultiNote is a collection of notes to be played at once
    /// or, at least close to "at once".
    /// </summary>
    public class MultiNote : Interfaces.INote
    {
        private readonly KeyboardSimulator _sim = new KeyboardSimulator(new InputSimulator());

        public NormalNote[] Notes { get; set; }

        public MultiNote(NormalNote[] notes)
        {
            Notes = notes;
        }

        private static readonly List<char> AlwaysHighNotes = new List<char>()
        {
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
            Task.Run(() =>
            {
                foreach (var note in Notes)
                {
                    bool isHighNote = char.IsUpper(note.Character) || AlwaysHighNotes.Contains(note.Character);

                    switch (isHighNote)
                    {
                        case true:
                            _sim
                                .KeyDown(VirtualKeyCode.LSHIFT)
                                .Sleep(5)
                                .KeyDown(note.NoteToPlay)
                                .Sleep(20)
                                .KeyUp(note.NoteToPlay)
                                .KeyUp(VirtualKeyCode.LSHIFT);
                            continue;
                        default:
                            _sim
                                .KeyDown(note.NoteToPlay)
                                .Sleep(25)
                                .KeyUp(note.NoteToPlay);
                            continue;
                    }
                }
            });
        }
    }
}
