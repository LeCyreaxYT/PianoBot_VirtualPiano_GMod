using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using PianoBot_VirtualPiano_GMod.Core.Exceptions;
using PianoBot_VirtualPiano_GMod.Core.Interfaces;
using PianoBot_VirtualPiano_GMod.Core.Notes;
using WindowsInput.Native;

namespace PianoBot_VirtualPiano_GMod.Core
{
    public delegate void AutoPlayerEvent();
    public delegate void AutoPlayerExceptionEvent(AutoplayerException e);

    internal static class AutoPlayer
    {
        #region Events

        public static event AutoPlayerEvent? AddingNoteFinished;
        public static event AutoPlayerEvent? AddingNotesFailed;
        public static event AutoPlayerEvent? NotesCleared;
        public static event AutoPlayerEvent? LoadCompleted;
        public static event AutoPlayerEvent? LoadFailed;
        public static event AutoPlayerEvent? SaveCompleted;
        public static event AutoPlayerEvent? SongFinishedPlaying;
        public static event AutoPlayerEvent? SongWasStopped;
        public static event AutoPlayerExceptionEvent? SongWasInteruptedByException;

        #endregion

        #region Field and property declarations

        //This can be accessed to get the version text
        public static string Version => "Version: 2.2c";

        //This will be used to define compatibility of save files between versions
        private static List<string> SupportedVersionsSave { get; } = new() {"Version: 2.1", "Version: 2.2a", "Version: 2.2b", "Version: 2.2c"};


        public static bool Pause { get; set; } = false;
        public static bool Loop { get; set; } = false;

        public static List<INote> Song { get; } = new();
        public static List<Delay> Delays { get; } = new();
        public static Dictionary<Note, Note> CustomNotes { get; } = new();
        private static readonly List<Note> MultiNoteBuffer = new();

        private static bool _buildingMultiNote = false;
        private const bool MultiNoteIsHighNote = false;
        private static int _delayAtNormalSpeed = 100;

        public static int DelayAtNormalSpeed
        {
            get => _delayAtNormalSpeed;
            set => _delayAtNormalSpeed = value;
        }

        private static int _delayAtFastSpeed = 100;

        private static int DelayAtFastSpeed
        {
            get => _delayAtFastSpeed;
            set => _delayAtFastSpeed = value;
        }

        public static List<Thread> SongThreads { get; } = new();


        private static Note? LastNote { get; set; }


        //TESTING: This dictionary will serve as a virtual key lookup. So when a note is created, it will check the dictionary for the virtual keycode of the character
        public static Dictionary<char, VirtualKeyCode> VirtualDictionary { get; } = new()
        {
            #region Characters
            ['A'] = VirtualKeyCode.VK_A,
            ['B'] = VirtualKeyCode.VK_B,
            ['C'] = VirtualKeyCode.VK_C,
            ['D'] = VirtualKeyCode.VK_D,
            ['E'] = VirtualKeyCode.VK_E,
            ['F'] = VirtualKeyCode.VK_F,
            ['G'] = VirtualKeyCode.VK_G,
            ['H'] = VirtualKeyCode.VK_H,
            ['I'] = VirtualKeyCode.VK_I,
            ['J'] = VirtualKeyCode.VK_J,
            ['K'] = VirtualKeyCode.VK_K,
            ['L'] = VirtualKeyCode.VK_L,
            ['M'] = VirtualKeyCode.VK_M,
            ['N'] = VirtualKeyCode.VK_N,
            ['O'] = VirtualKeyCode.VK_O,
            ['P'] = VirtualKeyCode.VK_P,
            ['Q'] = VirtualKeyCode.VK_Q,
            ['R'] = VirtualKeyCode.VK_R,
            ['S'] = VirtualKeyCode.VK_S,
            ['T'] = VirtualKeyCode.VK_T,
            ['U'] = VirtualKeyCode.VK_U,
            ['V'] = VirtualKeyCode.VK_V,
            ['W'] = VirtualKeyCode.VK_W,
            ['X'] = VirtualKeyCode.VK_X,
            ['Y'] = VirtualKeyCode.VK_Y,
            ['Z'] = VirtualKeyCode.VK_Z,
            ['|'] = VirtualKeyCode.OEM_PLUS,

            #endregion
            #region Numbers

            ['0'] = VirtualKeyCode.VK_0,
            ['1'] = VirtualKeyCode.VK_1,
            ['2'] = VirtualKeyCode.VK_2,
            ['3'] = VirtualKeyCode.VK_3,
            ['4'] = VirtualKeyCode.VK_4,
            ['5'] = VirtualKeyCode.VK_5,
            ['6'] = VirtualKeyCode.VK_6,
            ['7'] = VirtualKeyCode.VK_7,
            ['8'] = VirtualKeyCode.VK_8,
            ['9'] = VirtualKeyCode.VK_9,

            #endregion
            #region Symbols

            [' '] = VirtualKeyCode.OEM_MINUS,
            ['!'] = VirtualKeyCode.VK_1,
            ['@'] = VirtualKeyCode.VK_2,
            ['$'] = VirtualKeyCode.VK_4,
            ['%'] = VirtualKeyCode.VK_5,
            ['^'] = VirtualKeyCode.VK_6,
            ['*'] = VirtualKeyCode.VK_8,
            ['('] = VirtualKeyCode.VK_9,

            #endregion
        };

        public static readonly List<char> AlwaysHighNotes = new()
        {
            ' ',
            '-',
            '!',
            '@',
            '$',
            '%',
            '^',
            '*',
            '(',
        };

        #endregion

        #region Note handling

        /// <summary>
        /// This method will clear the song from all notes
        /// </summary>
        public static void ClearAllNotes()
        {
            Song.Clear();
            NotesCleared?.Invoke();
        }

        /// <summary>
        /// This method will take a string of notes and break it down into single notes
        /// and send them to the AddNoteFromChar method to process them
        /// </summary>
        public static void AddNotesFromString(string rawNotes)
        {
            _buildingMultiNote = false;
            foreach (char note in rawNotes)
            {
                AddNoteFromChar(note);
            }

            AddingNoteFinished?.Invoke();
        }


        /// <summary>
        /// This method will process a given character and add a corrosponding note to the song
        /// </summary>
        private static void AddNoteFromChar(char note)
        {
            Delay delay = Delays.Find(x => x.Character == note);

            switch (note)
            {
                case '[' when _buildingMultiNote:
                    AddingNotesFailed?.Invoke();
                    throw new AutoplayerInvalidMultiNoteException("A multi note cannot be defined whithin a multinote definition!");
                case '[':
                    _buildingMultiNote = true;
                    MultiNoteBuffer.Clear();
                    break;
                case ']' when !_buildingMultiNote:
                    AddingNotesFailed?.Invoke();
                    throw new AutoplayerMultiNoteNotDefinedException("A multi note must have a start before the end!");
                case ']':
                    _buildingMultiNote = false;
                    Song.Add(new MultiNote(MultiNoteBuffer.ToArray(), MultiNoteIsHighNote));
                    MultiNoteBuffer.Clear();
                    break;
                case '{' when _buildingMultiNote:
                    AddingNotesFailed?.Invoke();
                    throw new AutoplayerInvalidMultiNoteException("A speed change note cannot be defined whithin a multinote definition!");
                case '{':
                    Song.Add(new SpeedChangeNote(true));
                    break;
                case '}' when _buildingMultiNote:
                    AddingNotesFailed?.Invoke();
                    throw new AutoplayerInvalidMultiNoteException("A speed change note cannot be defined whithin a multinote definition!");
                case '}':
                    Song.Add(new SpeedChangeNote(false));
                    break;

                default:
                {
                    if (delay != null)
                    {
                        switch (_buildingMultiNote)
                        {
                            case false:
                                Song.Add(new DelayNote(delay.Character, delay.Time));
                                break;
                            default:
                                AddingNotesFailed?.Invoke();
                                throw new AutoplayerInvalidMultiNoteException("A delay note cannot be defined whithin a multinote definition!");
                        }
                    }
                    else
                    {
                        if (note is '\n' or '\r')
                        {
                            if (LastNote is {NoteToPlay: VirtualKeyCode.OEM_PLUS}) return;
                            Song.Add(new Note(note, VirtualKeyCode.OEM_MINUS, false));
                            return;
                        }
                        //If it didn't match any case, it must be a normal note

                        VirtualKeyCode vk;
                        try
                        {
                            VirtualDictionary.TryGetValue(char.ToUpper(note), out vk);

                            if (vk == 0)
                            {
                                return;
                            }
                        }
                        catch (ArgumentNullException)
                        {
                            return;
                        }

                        //This will check if the note is an uppercase letter, or if the note is in the list of high notes
                        bool isHighNote = char.IsUpper(note) || AlwaysHighNotes.Contains(note);

                        switch (_buildingMultiNote)
                        {
                            case true:
                                MultiNoteBuffer.Add(new Note(note, vk, isHighNote));
                                LastNote = new Note(note, vk, isHighNote);
                                break;
                            default:
                                Song.Add(new Note(note, vk, isHighNote));
                                LastNote = new Note(note, vk, isHighNote);
                                break;
                        }
                    }

                    break;
                }
            }
        }

        #endregion

        #region Custom delay handling

        /// <summary>
        /// This method will check if a delay exists in the list of delays
        /// If it does, it returns true, otherwise it returns false
        /// </summary>
        public static bool CheckDelayExists(char character)
        {
            return (Delays.Find(x => x.Character == character) != null);
        }

        /// <summary>
        /// This method will add a delay to the list of delays
        /// </summary>
        public static void AddDelay(char character, int time)
        {
            if (!CheckDelayExists(character))
            {
                Delays.Add(new Delay(character, time));
            }
            else
            {
                throw new AutoplayerCustomDelayException("Trying to add already existing delay");
            }
        }

        /// <summary>
        /// This method will remove a delay from the list of delays
        /// </summary>
        public static void RemoveDelay(char character)
        {
            if (CheckDelayExists(character))
            {
                Delays.Remove(Delays.Find(x => x.Character == character));
            }
            else
            {
                throw new AutoplayerCustomDelayException("Trying to remove non-existent delay");
            }
        }

        /// <summary>
        /// This method will set a new time to a specified delay from the list of delays
        /// </summary>
        public static void ChangeDelay(char character, int newTime)
        {
            if (CheckDelayExists(character))
            {
                Delays.Find(x => x.Character == character).Time = newTime;
            }
            else
            {
                throw new AutoplayerCustomDelayException("Trying to modify non-existent delay");
            }
        }

        /// <summary>
        /// This method will remove all delays from the list of delays
        /// </summary>
        public static void ResetDelays()
        {
            Delays.Clear();
        }

        #endregion

        #region Custom note handling

        /// <summary>
        /// This method will check if a note exists in the list of custom notes
        /// If it does, it returns true, otherwise it returns false
        /// </summary>
        public static bool CheckNoteExists(Note note)
        {
            return CustomNotes.ContainsKey(note);
        }

        /// <summary>
        /// This method will check if a note exists in the list of custom notes
        /// as well as the connection between the note and the new note of the pair
        /// </summary>
        private static bool CheckNoteExists(Note note, Note newNote)
        {
            CustomNotes.TryGetValue(note, out var checkNote);
            return checkNote != null && Equals(checkNote, newNote);
        }

        /// <summary>
        /// This method will add a note to the list of custom notes
        /// </summary>
        public static void AddNote(Note note, Note newNote)
        {
            if (!CheckNoteExists(note, newNote))
            {
                CustomNotes.Add(note, newNote);
            }
            else
            {
                throw new AutoplayerCustomNoteException("Trying to add already existing note");
            }
        }

        /// <summary>
        /// This method will remove a note from the list of custom notes
        /// </summary>
        public static void RemoveNote(Note note)
        {
            if (CheckNoteExists(note))
            {
                CustomNotes.Remove(note);
            }
            else
            {
                throw new AutoplayerCustomNoteException("Trying to remove non-existent note");
            }
        }

        /// <summary>
        /// This method will set a new note to the value of a specified note from the list of custom notes
        /// </summary>
        public static void ChangeNote(Note note, Note newNote)
        {
            if (CheckNoteExists(note))
            {
                CustomNotes.Remove(note);
                CustomNotes.Add(note, newNote);
            }
            else
            {
                throw new AutoplayerCustomNoteException("Trying to modify non-existent note");
            }
        }

        /// <summary>
        /// This method will remove all notes from the list of custom notes
        /// </summary>
        public static void ResetNotes()
        {
            CustomNotes.Clear();
        }

        #endregion

        /// <summary>
        /// This method will play the notes in the song in sequence
        /// </summary>
        public static void PlaySong()
        {

            foreach (INote note in Song)
            {
                try
                {
                    if (note is Note note1)
                    {
                        if (CustomNotes.ContainsKey(note1))
                        {
                            CustomNotes.TryGetValue(note1, out var customNote);
                            customNote.Play();
                        }
                        else
                        {
                            switch (note1)
                            {
                                case {NoteToPlay: VirtualKeyCode.OEM_PLUS}:
                                    int miliseconds1 = (60000 / (DelayAtNormalSpeed * 2));
                                    Thread.Sleep(miliseconds1);
                                    continue;
                                case {NoteToPlay: VirtualKeyCode.OEM_MINUS}:
                                    continue;
                                default:
                                    note.Play();
                                    break;
                            }
                        }
                    }
                    else
                    {
                        note.Play();
                    }


                    if (note is not Note && note is not MultiNote) continue;

                    //Rechte die BPM 1/6 zu Milisekunden umrechnen
                    int miliseconds = (60000 / (DelayAtNormalSpeed * 2));
                    Thread.Sleep(miliseconds);
                }
                catch (AutoplayerTargetNotFoundException error)
                {
                    SongWasStopped?.Invoke();
                    SongWasInteruptedByException?.Invoke(error);
                }
                catch (ArgumentException)
                {
                    SongWasStopped?.Invoke();
                    SongWasInteruptedByException?.Invoke(new AutoplayerException(
                        $"The program encountered an invalid note. Please inform the developer of this incident so it can be added to the list of invalid characters. Info: '{note}'"));
                }
            }

            if (Loop)
            {
                PlaySong();
            }

            SongFinishedPlaying?.Invoke();
        }



        #region Save & Load
        /// <summary>
        /// This method will save the song and its settings to a file at the "path" variable's destination
        /// </summary>
        public static void SaveSong(string path)
        {
            StreamWriter sw = new(path);
            sw.WriteLine(Version);
            sw.WriteLine("DELAYS");
            sw.WriteLine(Delays.Count);
            if (Delays.Count != 0)
            {
                foreach (Delay delay in Delays)
                {
                    sw.WriteLine(delay.Character);
                    sw.WriteLine(delay.Time);
                }
            }
            sw.WriteLine("CUSTOM NOTES");
            sw.WriteLine(CustomNotes.Count);
            if (CustomNotes.Count != 0)
            {
                foreach (KeyValuePair<Note, Note> note in CustomNotes)
                {
                    sw.WriteLine(note.Value.Character);
                    sw.WriteLine(note.Key.Character);
                }
            }
            sw.WriteLine("SPEEDS");
            sw.WriteLine(DelayAtNormalSpeed);
            sw.WriteLine(DelayAtFastSpeed);
            sw.WriteLine("NOTES");
            sw.WriteLine(Song.Count);
            if (Song.Count != 0)
            {
                foreach (INote note in Song)
                {
                    switch (note)
                    {
                        case DelayNote delayNote:
                            sw.Write(delayNote.Character);
                            break;
                        case SpeedChangeNote {TurnOnFast: true}:
                            sw.Write("{");
                            break;
                        case SpeedChangeNote _:
                            sw.Write("}");
                            break;
                        case Note note1:
                            sw.Write(note1.Character);
                            break;
                        case MultiNote note1:
                        {
                            sw.Write("[");
                            foreach (Note multiNote in note1.Notes)
                            {
                                sw.Write(multiNote.Character);
                            }
                            sw.Write("]");
                            break;
                        }
                    }
                }
            }
            sw.Dispose();
            sw.Close();
            SaveCompleted?.Invoke();
        }
        /// <summary>
        /// This method will load a song and its settings from a file at the "path" variable's destination
        /// This loading method handles all previous save formats for backwards compatibility
        /// </summary>
        public static void LoadSong(string path)
        {
            Song.Clear();
            bool errorWhileLoading = true;
            StreamReader sr = new StreamReader(path);
            string firstLine = sr.ReadLine() ?? string.Empty;

            #region 2.1+ save format
            if (SupportedVersionsSave.Contains(firstLine))
            {
                if(sr.ReadLine() == "DELAYS")
                {
                    if (int.TryParse(sr.ReadLine(), out var delayCount) && delayCount > 0)
                    {
                        for (int i = 0; i < delayCount; i++)
                        {
                            if (!char.TryParse(sr.ReadLine(), out var delayChar)) continue;
                            if (int.TryParse(sr.ReadLine(), out var delayTime))
                            {
                                Delays.Add(new Delay(delayChar, delayTime));
                            }
                        }
                    }
                }
                if(sr.ReadLine() == "CUSTOM NOTES")
                {
                    if (int.TryParse(sr.ReadLine(), out var noteCount) && noteCount > 0)
                    {
                        for (int i = 0; i < noteCount; i++)
                        {
                            if (!char.TryParse(sr.ReadLine(), out var origNoteChar)) continue;
                            if (!char.TryParse(sr.ReadLine(), out var replaceNoteChar)) continue;
                            VirtualKeyCode vkOld;
                            VirtualKeyCode vkNew;
                            try
                            {
                                VirtualDictionary.TryGetValue(origNoteChar, out vkOld);
                                VirtualDictionary.TryGetValue(replaceNoteChar, out vkNew);

                                if (vkOld == 0 || vkNew == 0)
                                {
                                    return;
                                }
                            }
                            catch (ArgumentNullException)
                            {
                                return;
                            }

                            CustomNotes.Add(new Note(origNoteChar, vkOld, char.IsUpper(origNoteChar)), new Note(replaceNoteChar, vkNew, char.IsUpper(replaceNoteChar)));
                        }
                    }
                }
                if(sr.ReadLine() == "SPEEDS")
                {
                    int.TryParse(sr.ReadLine(), out var normalSpeed);
                    int.TryParse(sr.ReadLine(), out var fastSpeed);
                    DelayAtNormalSpeed = normalSpeed;
                    DelayAtFastSpeed = fastSpeed;
                }
                if (sr.ReadLine() == "NOTES")
                {
                    if (int.TryParse(sr.ReadLine(), out var noteCount) && noteCount > 0)
                    {
                        AddNotesFromString(sr.ReadToEnd());
                    }
                    errorWhileLoading = false;
                }
            }
            #endregion
            #region 2.0 save format (for backwards compatibility)
            switch (firstLine)
            {
                case "DELAYS":
                {
                    if (int.TryParse(sr.ReadLine(), out var delayCount) && delayCount > 0)
                    {
                        for (int i = 0; i < delayCount; i++)
                        {
                            if (!char.TryParse(sr.ReadLine(), out var delayChar)) continue;
                            if (int.TryParse(sr.ReadLine(), out var delayTime))
                            {
                                Delays.Add(new Delay(delayChar, delayTime));
                            }
                        }
                    }
                    if (sr.ReadLine() == "NOTES")
                    {
                        if (int.TryParse(sr.ReadLine(), out var noteCount) && noteCount > 0)
                        {
                            AddNotesFromString(sr.ReadToEnd());
                        }
                        errorWhileLoading = false;
                    }

                    break;
                }
                case "CUSTOM DELAYS":
                {
                    if (int.TryParse(sr.ReadLine(), out var delayCount))
                    {
                        if (sr.ReadLine() == "NORMAL DELAY")
                        {
                            if (int.TryParse(sr.ReadLine(), out _delayAtNormalSpeed))
                            {
                                Delays.Add(new Delay(' ', DelayAtNormalSpeed));
                                if (sr.ReadLine() == "FAST DELAY")
                                {
                                    if (int.TryParse(sr.ReadLine(), out _delayAtFastSpeed))
                                    {
                                        if (delayCount != 0)
                                        {
                                            for (int i = 0; i < delayCount; i++)
                                            {
                                                if (sr.ReadLine() != "CUSTOM DELAY INDEX") continue;
                                                if (!int.TryParse(sr.ReadLine(), out _)) continue;
                                                if (sr.ReadLine() != "CUSTOM DELAY CHARACTER") continue;
                                                if (!char.TryParse(sr.ReadLine(), out var customDelayChar)) continue;
                                                if (sr.ReadLine() != "CUSTOM DELAY TIME") continue;
                                                if (int.TryParse(sr.ReadLine(), out var customDelayTime))
                                                {
                                                    Delays.Add(new Delay(customDelayChar, customDelayTime));
                                                }
                                            }
                                        }
                                        if (sr.ReadLine() == "NOTES")
                                        {
                                            AddNotesFromString(sr.ReadToEnd());
                                            errorWhileLoading = false;
                                        }
                                    }
                                }
                            }
                        }
                    }

                    break;
                }
                case "NORMAL DELAY":
                {
                    if (int.TryParse(sr.ReadLine(), out _delayAtNormalSpeed))
                    {
                        if (sr.ReadLine() == "FAST DELAY")
                        {
                            if (int.TryParse(sr.ReadLine(), out _delayAtFastSpeed))
                            {
                                if (sr.ReadLine() == "NOTES")
                                {
                                    AddNotesFromString(sr.ReadToEnd());
                                    errorWhileLoading = false;
                                }
                            }
                        }
                    }

                    break;
                }
            }
            #endregion
            sr.Close();
            if (errorWhileLoading)
            {
                LoadFailed?.Invoke();
                throw new AutoplayerLoadFailedException("No compatible save format was found!");
            }
            LoadCompleted?.Invoke();
        }
        #endregion
    }
}