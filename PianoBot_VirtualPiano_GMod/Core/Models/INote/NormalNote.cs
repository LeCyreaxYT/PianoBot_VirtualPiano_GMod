using System.Threading.Tasks;
using WindowsInput;
using WindowsInput.Native;

namespace PianoBot_VirtualPiano_GMod.Core.Models.INote
{
    public class NormalNote : Interfaces.INote
    {
        private readonly KeyboardSimulator _sim = new(new InputSimulator());

        public VirtualKeyCode NoteToPlay { get; }
        public char Character { get; }
        private bool IsHighNote { get; }

        public NormalNote(char character, VirtualKeyCode note, bool isHighNote)
        {
            NoteToPlay = note;
            Character = character;
            IsHighNote = isHighNote;
        }

        public void Play()
        {
            Task.Run(() =>
            {
                switch (IsHighNote)
                {
                    case true:
                        _sim
                            .KeyDown(VirtualKeyCode.LSHIFT)
                            .Sleep(5)
                            .KeyDown(NoteToPlay)
                            .Sleep(20)
                            .KeyUp(NoteToPlay)
                            .KeyUp(VirtualKeyCode.LSHIFT);
                        break;
                    default:
                        _sim
                            .KeyDown(NoteToPlay)
                            .Sleep(25)
                            .KeyUp(NoteToPlay);
                        break;
                }
            });
        }
    }
}
