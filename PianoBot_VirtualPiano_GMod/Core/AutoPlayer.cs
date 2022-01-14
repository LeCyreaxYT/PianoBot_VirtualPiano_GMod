using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using PianoBot_VirtualPiano_GMod.Core.Exceptions;
using PianoBot_VirtualPiano_GMod.Core.Interfaces;
using PianoBot_VirtualPiano_GMod.Core.Models.INote;
using WindowsInput.Events;
using Timer = System.Windows.Forms.Timer;

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

        public static string Version => "Version: 1.2";
        private static List<string> SupportedVersionsSave { get; } = new() {"Version: 1.0", "Version: 1.1", "Version: 1.2"};

        
        public static bool Loop { get; set; }
        public static List<INote> Song { get; } = new();
        public static List<Delay> Delays { get; } = new();
        public static Dictionary<NormalNote, NormalNote> CustomNotes { get; } = new();
        private static readonly List<NormalNote> MultiNoteBuffer = new();
        private static bool _buildingMultiNote;
        
        
        public static bool IsFastSpeed = false;
        public static bool IsPaused = false;
        
        public static int DelayAtNormalSpeed { get; set; } = 100;
        public static int DelayAtFastSpeed { get; set; } = 150;

        public static Thread? SongThread { get; set; }
        public static Stopwatch Stopwatch { get; } = new();
        public static Timer StopwatchTimer { get; set; } = new();
        public static Timer PlayerTimer { get; set; } = new();
        private static NormalNote? LastNote { get; set; }



        public static Dictionary<char, KeyCode> VirtualDictionary { get; } = new()
        {
            #region Characters

            ['A'] = KeyCode.A,
            ['B'] = KeyCode.B,
            ['C'] = KeyCode.C,
            ['D'] = KeyCode.D,
            ['E'] = KeyCode.E,
            ['F'] = KeyCode.F,
            ['G'] = KeyCode.G,
            ['H'] = KeyCode.H,
            ['I'] = KeyCode.I,
            ['J'] = KeyCode.J,
            ['K'] = KeyCode.K,
            ['L'] = KeyCode.L,
            ['M'] = KeyCode.M,
            ['N'] = KeyCode.N,
            ['O'] = KeyCode.O,
            ['P'] = KeyCode.P,
            ['Q'] = KeyCode.Q,
            ['R'] = KeyCode.R,
            ['S'] = KeyCode.S,
            ['T'] = KeyCode.T,
            ['U'] = KeyCode.U,
            ['V'] = KeyCode.V,
            ['W'] = KeyCode.W,
            ['X'] = KeyCode.X,
            ['Y'] = KeyCode.Y,
            ['Z'] = KeyCode.Z,
            ['|'] = KeyCode.Oemplus,

            #endregion

            #region Numbers

            ['0'] = KeyCode.D0,
            ['1'] = KeyCode.D1,
            ['2'] = KeyCode.D2,
            ['3'] = KeyCode.D3,
            ['4'] = KeyCode.D4,
            ['5'] = KeyCode.D5,
            ['6'] = KeyCode.D6,
            ['7'] = KeyCode.D7,
            ['8'] = KeyCode.D8,
            ['9'] = KeyCode.D9,

            #endregion

            #region Symbols

            [' '] = KeyCode.OemMinus,
            ['!'] = KeyCode.Oem1,
            ['@'] = KeyCode.Oem2,
            ['$'] = KeyCode.Oem4,
            ['%'] = KeyCode.Oem5,
            ['^'] = KeyCode.Oem6,
            ['*'] = KeyCode.Oem8,
            ['('] = KeyCode.Oem102,

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
        
        public static void AddNotesFromString(string? rawNotes)
        {
            Console.WriteLine("Adding notes from string...");
            Stopwatch sw = new();
            sw.Start();
            
            _buildingMultiNote = false;

            foreach (var note in rawNotes!)
            {
                AddNoteFromChar(note);
            }

            AddingNoteFinished?.Invoke();
            
            sw.Stop();
            Console.WriteLine($"Finished adding notes from string in {sw.ElapsedMilliseconds}ms");
        }
        
        private static void AddNoteFromChar(char note)
        {
            Console.WriteLine("Adding note from char...");
            Stopwatch sw = new();
            sw.Start();
            
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
                    Song.Add(new MultiNote(MultiNoteBuffer.ToArray()));
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
                            if (LastNote is {NoteToPlay: KeyCode.Oemplus}) return;
                            Song.Add(new NormalNote(note, KeyCode.OemMinus, false));
                            return;
                        }
                        //If it didn't match any case, it must be a normal note

                        KeyCode vk;
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
                                MultiNoteBuffer.Add(new NormalNote(note, vk, isHighNote));
                                LastNote = new NormalNote(note, vk, isHighNote);
                                break;
                            default:
                                Song.Add(new NormalNote(note, vk, isHighNote));
                                LastNote = new NormalNote(note, vk, isHighNote);
                                break;
                        }
                    }

                    break;
                }
            }
            
            sw.Stop();
            Console.WriteLine("Added note in " + sw.ElapsedTicks + "ms");
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
                throw new AutoPlayerCustomDelayException("Trying to add already existing delay");
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
                throw new AutoPlayerCustomDelayException("Trying to remove non-existent delay");
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
                throw new AutoPlayerCustomDelayException("Trying to modify non-existent delay");
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
        public static bool CheckNoteExists(NormalNote normalNote)
        {
            return CustomNotes.ContainsKey(normalNote);
        }

        /// <summary>
        /// This method will check if a note exists in the list of custom notes
        /// as well as the connection between the note and the new note of the pair
        /// </summary>
        private static bool CheckNoteExists(NormalNote normalNote, NormalNote newNormalNote)
        {
            CustomNotes.TryGetValue(normalNote, out var checkNote);
            return checkNote != null && Equals(checkNote, newNormalNote);
        }

        /// <summary>
        /// This method will add a note to the list of custom notes
        /// </summary>
        public static void AddNote(NormalNote normalNote, NormalNote newNormalNote)
        {
            if (!CheckNoteExists(normalNote, newNormalNote))
            {
                CustomNotes.Add(normalNote, newNormalNote);
            }
            else
            {
                throw new AutoPlayerCustomNoteException("Trying to add already existing note");
            }
        }

        /// <summary>
        /// This method will remove a note from the list of custom notes
        /// </summary>
        public static void RemoveNote(NormalNote normalNote)
        {
            if (CheckNoteExists(normalNote))
            {
                CustomNotes.Remove(normalNote);
            }
            else
            {
                throw new AutoPlayerCustomNoteException("Trying to remove non-existent note");
            }
        }

        /// <summary>
        /// This method will set a new note to the value of a specified note from the list of custom notes
        /// </summary>
        public static void ChangeNote(NormalNote normalNote, NormalNote newNormalNote)
        {
            if (CheckNoteExists(normalNote))
            {
                CustomNotes.Remove(normalNote);
                CustomNotes.Add(normalNote, newNormalNote);
            }
            else
            {
                throw new AutoPlayerCustomNoteException("Trying to modify non-existent note");
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
        
        public static void ChangeSpeed(bool changeToFast)
        {
            IsFastSpeed = changeToFast;
        }
        
        public static void PlaySong()
        {
            foreach (INote note in Song)
            {
                try
                {
                    if (note is NormalNote note1)
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
                                case {NoteToPlay: KeyCode.Oemplus}:
                                    if (IsFastSpeed)
                                    {
                                        int atFast = 60000 / (DelayAtFastSpeed * 2);
                                        Thread.Sleep(atFast);
                                    }
                                    else
                                    {
                                        int atNormal = 60000 / (DelayAtNormalSpeed * 2);
                                        Thread.Sleep(atNormal);
                                    }
                                    continue;
                                case {NoteToPlay: KeyCode.OemMinus}:
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

                    if (note is not NormalNote && note is not MultiNote) continue;

                    if (IsFastSpeed)
                    {
                        int atFast = 60000 / (DelayAtFastSpeed * 2);
                        Thread.Sleep(atFast);
                    }
                    else
                    {
                        int atNormal = 60000 / (DelayAtNormalSpeed * 2);
                        Thread.Sleep(atNormal);
                    }
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
        public static void SaveSong(string path, string ext)
        {
            switch (ext)
            {
                case ".txt":
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
                        foreach (KeyValuePair<NormalNote, NormalNote> note in CustomNotes)
                        {
                            sw.WriteLine(note.Value.Character);
                            sw.WriteLine(note.Key.Character);
                        }
                    }
                    sw.WriteLine("SPEEDS");
                    sw.WriteLine(DelayAtNormalSpeed);
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
                                case SpeedChangeNote {TurnOnFast: false}:
                                    sw.Write("}");
                                    break;
                                case NormalNote note1:
                                    sw.Write(note1.Character);
                                    break;
                                case MultiNote note1:
                                {
                                    sw.Write("[");
                                    foreach (NormalNote multiNote in note1.Notes)
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
                    break;
                }
                case ".json":
                    string saveVersion = Version;
                    int customDelay = DelayAtNormalSpeed;
                    string notes = "";

                    if (Song.Count != 0)
                    {
                        foreach (INote note in Song)
                        {
                            switch (note)
                            {
                                case DelayNote delayNote:
                                    notes += delayNote.Character;
                                    break;
                                case SpeedChangeNote {TurnOnFast: true}:
                                    notes += "{";
                                    break;
                                case SpeedChangeNote {TurnOnFast: false}:
                                    notes += "}";
                                    break;
                                case NormalNote note1:
                                    notes += note1.Character;
                                    break;
                                case MultiNote note1:
                                {
                                    notes += "[";
                                    notes = note1.Notes.Aggregate(notes, (current, multiNote) => current + multiNote.Character);
                                    notes += "]";
                                    break;
                                }
                            }
                        }
                    }

                    SavingModel savingModel = new()
                    {
                        SaveVersion = saveVersion,
                        CustomBpm = customDelay,
                        NotesLength = notes.Length,
                        Notes = notes
                    };
            
                    string json = savingModel.GetSerailizedData();
            
                    File.WriteAllText(path, json);
                    break;
            }
            SaveCompleted?.Invoke();
        }
        /// <summary>
        /// This method will load a song and its settings from a file at the "path" variable's destination
        /// This loading method handles all previous save formats for backwards compatibility
        /// </summary>
        public static void LoadSong(string path, string ext)
        {
            Song.Clear();

            switch (ext)
            {
                case ".txt":
                    Console.WriteLine("Loading .txt file at " + path);
                    Stopwatch sw = new();
                    sw.Start();

                    bool errorWhileLoading = true;
                    StreamReader sr = new(path);
                    string firstLine = sr.ReadLine() ?? string.Empty;

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
                                    KeyCode vkOld;
                                    KeyCode vkNew;
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

                                    CustomNotes.Add(new NormalNote(origNoteChar, vkOld, char.IsUpper(origNoteChar)), new NormalNote(replaceNoteChar, vkNew, char.IsUpper(replaceNoteChar)));
                                }
                            }
                        }
                        if(sr.ReadLine() == "SPEEDS")
                        {
                            int.TryParse(sr.ReadLine(), out var normalSpeed);
                            DelayAtNormalSpeed = normalSpeed;
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
                    
                    sr.Close();
                    if (errorWhileLoading)
                    {
                        LoadFailed?.Invoke();
                        throw new AutoplayerLoadFailedException("No compatible save format was found!");
                    } 
                    
                    sw.Stop();
                    Console.WriteLine("Loaded in " + sw.ElapsedMilliseconds + "ms");
                    break;
                    
                case ".json":
                    string jsonstring = File.ReadAllText(path);
                    SavingModel? model = JsonConvert.DeserializeObject<SavingModel>(jsonstring);
                    if (model == null)
                    {
                        LoadFailed?.Invoke();
                        throw new AutoplayerLoadFailedException("No compatible save format was found!");
                    }
                    else
                    {
                        DelayAtNormalSpeed = model.CustomBpm;
                        
                        if(model.NotesLength > 0)
                        {
                            AddNotesFromString(model.Notes);
                        }
                    }

                    break;
            }
            
            LoadCompleted?.Invoke();
        }
        #endregion
    }
}