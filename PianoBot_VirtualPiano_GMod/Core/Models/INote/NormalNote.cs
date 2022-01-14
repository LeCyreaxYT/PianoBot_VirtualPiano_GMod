using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WindowsInput;
using WindowsInput.Events;
using WindowsInput.Native;

namespace PianoBot_VirtualPiano_GMod.Core.Models.INote
{
    public class NormalNote : Interfaces.INote
    {
        //private readonly KeyboardSimulator _sim = new(new InputSimulator());

        public KeyCode NoteToPlay { get; }
        public char Character { get; }
        private bool IsHighNote { get; }

        public NormalNote(char character, KeyCode note, bool isHighNote)
        {
            NoteToPlay = note;
            Character = character;
            IsHighNote = isHighNote;
        }
        
        public static Dictionary<string, KeyCode> NoteDictionary = new Dictionary<string, KeyCode>()
        {
            {"!", KeyCode.D1},
            {"@", KeyCode.D2},
            {"$", KeyCode.D4},
            {"%", KeyCode.D5},
            {"^", KeyCode.D6},
            {"*", KeyCode.D8},
            {"(", KeyCode.D9},
        };

        public void Play()
        {
            Task.Run(() =>
            {
                
                if (IsHighNote)
                {
                    Simulate.Events()
                        .ClickChord(KeyCode.LShift, NoteToPlay)
                        .Invoke();
                }
                else
                {
                    Simulate.Events()
                        .Click(NoteToPlay)
                        .Invoke();
                }
                
                // switch (IsHighNote)
                // {
                //     case true:
                //         _sim
                //             .KeyDown(VirtualKeyCode.LSHIFT)
                //             .Sleep(5)
                //             .KeyDown(NoteToPlay)
                //             .Sleep(45)
                //             .KeyUp(NoteToPlay)
                //             .KeyUp(VirtualKeyCode.LSHIFT);
                //         break;
                //     default:
                //         _sim
                //             .KeyDown(NoteToPlay)
                //             .Sleep(50)
                //             .KeyUp(NoteToPlay);
                //         break;
                // }
            });
        }
    }
}
