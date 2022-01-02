using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using PianoBot_VirtualPiano_GMod.Core;
using PianoBot_VirtualPiano_GMod.Core.Exceptions;
using PianoBot_VirtualPiano_GMod.Core.Interfaces;
using PianoBot_VirtualPiano_GMod.Core.Notes;
using PianoBot_VirtualPiano_GMod.Imported;
using Timer = System.Windows.Forms.Timer;

namespace PianoBot_VirtualPiano_GMod.GUI
{
    public partial class GraphicalUserInterface : Form
    {
        //This is the hook for the key bindings, until I find a better solution
        GlobalKeyboardHook gkh = new GlobalKeyboardHook();

        //The key bind to start playing
        Keys startKey;

        //The key bind to stop playing
        Keys stopKey;
        
        //The key bind to pause playing
        Keys pauseKey;

        //This variable is used to ignore changes to the note box when loading a saved file
        bool isLoading = false;
        
        /// <summary>
        /// This is the constructor for the GUI
        /// It is called when the GUI starts
        /// </summary>
        public GraphicalUserInterface()
        {
            InitializeComponent();
            ErrorLabel.Hide();
            VersionLabel.Text = AutoPlayer.Version;
            AutoPlayer.AddingNoteFinished += EnablePlayButton;
            AutoPlayer.SongFinishedPlaying += EnableClearButton;
            AutoPlayer.SongFinishedPlaying += EnablePlayButton;
            AutoPlayer.SongFinishedPlaying += SongStopped;
            AutoPlayer.SongWasStopped += EnableClearButton;
            AutoPlayer.SongWasStopped += EnablePlayButton;
            AutoPlayer.SongWasStopped += SongStopped;
            AutoPlayer.SongWasInteruptedByException += ExceptionHandler;
            
            //Subscribe the method "GKS_KeyDown" to the KeyDown event of the GlobalKeyboardHook
            gkh.KeyDown += GKS_KeyDown;

            //This converts the text from the keybind settings window to actual keys
            //Then we add them to the global hook (This is done so the keypresses will be detected when the application is not in focus)
            KeysConverter keysConverter = new KeysConverter();
            startKey = (Keys) keysConverter.ConvertFromString(StartKeyTextBox.Text);
            stopKey = (Keys) keysConverter.ConvertFromString(StopKeyTextBox.Text);
            pauseKey = (Keys) keysConverter.ConvertFromString(PauseKeyTextBox.Text);
            gkh.HookedKeys.Add(startKey);
            gkh.HookedKeys.Add(stopKey);
            gkh.HookedKeys.Add(pauseKey);
        }

        /// <summary>
        /// This method handles the key bind presses
        /// NOTE: This might trigger anti-virus software
        /// as this is a popular method used in keyloggers
        /// </summary>
        private void GKS_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == startKey)
            {
                PlayButton.PerformClick();
            }
            else if (e.KeyCode == stopKey)
            {
                StopButton.PerformClick();
            }
            else if (e.KeyCode == pauseKey)
            {
                PauseButton.PerformClick();
            }

            e.Handled = true;
        }

        /// <summary>
        /// This method will run all the update methods.
        /// </summary>
        public void UpdateEverything()
        {
            UpdateNoteBox();
            UpdateDelayListBox();
            NormalDelayBox.Value = AutoPlayer.DelayAtNormalSpeed;
            isLoading = false;
        }

        /// <summary>
        /// This method updates the DelayListBox with all delays from the Delays list in the main program
        /// Each custom delay has its own line
        /// </summary>
        public void UpdateDelayListBox()
        {
            DelayListBox.Clear();
            foreach (Delay delay in AutoPlayer.Delays)
            {
                DelayListBox.Text += $"Character: '{delay.Character}' : Delay: '{delay.Time}'\n";
            }
        }

        /// <summary>
        /// This method updates the CustomNotesListBox with all notes from the CustomNotes list in the main program
        /// Each custom note has its own line
        /// </summary>
        private void UpdateCustomNoteListBox()
        {
            CustomNoteListBox.Clear();
            foreach (Note note in AutoPlayer.CustomNotes.Keys)
            {
                AutoPlayer.CustomNotes.TryGetValue(note, out Note newNote);
                CustomNoteListBox.Text += $"Changed from '{note}' to '{newNote}'\n";
            }
        }

        /// <summary>
        /// This method updates the NoteTextBox with all notes from the Song list in the main program
        /// </summary>
        private void UpdateNoteBox()
        {
            NoteTextBox.Clear();
            foreach (INote note in AutoPlayer.Song)
            {
                switch (note)
                {
                    case DelayNote delayNote:
                        NoteTextBox.Text += (delayNote.Character);
                        break;
                    case SpeedChangeNote {TurnOnFast: true}:
                        NoteTextBox.Text += "{";
                        break;
                    case SpeedChangeNote _:
                        NoteTextBox.Text += "}";
                        break;
                    case Note note1:
                        NoteTextBox.Text += note1.Character;
                        break;
                    case MultiNote note1:
                    {
                        NoteTextBox.Text += "[";
                        foreach (Note multiNote in note1.Notes)
                        {
                            NoteTextBox.Text += multiNote.Character;
                        }

                        NoteTextBox.Text += "]";
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// This method makes or remakes the song by clearing all notes and adding the ones from NoteTextBox
        /// </summary>
        private void MakeSong()
        {
            try
            {
                ErrorLabel.Hide();
                DisablePlayButton();
                AutoPlayer.ClearAllNotes();
                AutoPlayer.AddNotesFromString(NoteTextBox.Text);
            }
            catch (AutoplayerNoteCreationFailedException e)
            {
                ErrorLabel.Text = $"ERROR: {e.Message}";
                ErrorLabel.Show();
            }
        }

        /// <summary>
        /// This method disables the play button so the user cannot press it
        /// </summary>
        private void DisablePlayButton()
        {
            PlayButton.Enabled = false;
        }

        /// <summary>
        /// This method enables the play button and makes it interactable
        /// </summary>
        private void EnablePlayButton()
        {
            PlayButton.Enabled = true;
        }

        /// <summary>
        /// This method disables the clear button so the user cannot press it
        /// </summary>
        private void DisableClearButton()
        {
            ClearNotesButton.Enabled = false;
        }

        /// <summary>
        /// This method enables the clear button and makes it interactable
        /// </summary>
        private void EnableClearButton()
        {
            if (ClearNotesButton.InvokeRequired)
            {
                void MethodInvokerDelegate()
                {
                    ClearNotesButton.Enabled = true;
                }

                ClearNotesButton.Invoke((MethodInvoker) MethodInvokerDelegate);
            }
            else
            {
                ClearNotesButton.Enabled = true;
            }
        }

        /// <summary>
        /// This method will handle exceptions thrown from other threads than the current one
        /// This was added because I had some problems with exceptions from other threads not being catched
        /// </summary>
        private void ExceptionHandler(AutoplayerException exception)
        {
            ErrorLabel.Text = exception.Message;
            ErrorLabel.Show();
        }

        #region Custom delay buttons

        /// <summary>
        /// This is called when we click the AddDelayButton
        /// </summary>
        private void AddDelayButton_Click(object sender, EventArgs e)
        {
            try
            {
                if (AutoPlayer.CheckDelayExists(CustomDelayCharacterBox.Text.ToCharArray()[0]))
                {
                    //If the delay already exists, just update the time value
                    AutoPlayer.ChangeDelay(CustomDelayCharacterBox.Text.ToCharArray()[0], (int) CustomDelayTimeBox.Value);
                }
                else
                {
                    //If the delay does not exist, add a new entry
                    AutoPlayer.AddDelay(CustomDelayCharacterBox.Text.ToCharArray()[0], (int) CustomDelayTimeBox.Value);
                }

                //Update the GUI element to show the delays in the GUI
                UpdateDelayListBox();

                //Update the current notes to with the new rules
                MakeSong();
            }
            catch (AutoPlayerCustomDelayException error)
            {
                MessageBox.Show(error.Message);
            }
        }

        /// <summary>
        /// This is called when we click the RemoveDelayButton
        /// </summary>
        private void RemoveDelayButton_Click(object sender, EventArgs e)
        {
            try
            {
                //Remove the delay from the list
                AutoPlayer.RemoveDelay(CustomDelayCharacterBox.Text.ToCharArray()[0]);
                //Update the GUI
                UpdateDelayListBox();

                //Update the current notes wtih the new rules
                MakeSong();
            }
            catch (AutoPlayerCustomDelayException error)
            {
                MessageBox.Show(error.Message);
            }
        }

        /// <summary>
        /// This is called when we click the RemoveAllDelayButton
        /// </summary>
        private void RemoveAllDelayButton_Click(object sender, EventArgs e)
        {
            //Clear the list of delays
            AutoPlayer.ResetDelays();

            //Update the GUI
            UpdateDelayListBox();

            //Update the current notes wtih the new rules
            MakeSong();
        }

        #endregion

        #region Custom note buttons

        /// <summary>
        /// This is called when we click the AddNoteButton
        /// </summary>
        private void AddNoteButton_Click(object sender, EventArgs e)
        {
            try
            {
                char character = CustomNoteCharacterBox.Text.ToCharArray()[0];
                char newCharacter = CustomNoteNewCharacterBox.Text.ToCharArray()[0];

                WindowsInput.Native.VirtualKeyCode vkOld;
                WindowsInput.Native.VirtualKeyCode vkNew;
                try
                {
                    AutoPlayer.VirtualDictionary.TryGetValue(character, out vkOld);
                    AutoPlayer.VirtualDictionary.TryGetValue(newCharacter, out vkNew);

                    if (vkOld == 0 || vkNew == 0)
                    {
                        return;
                    }
                }
                catch (ArgumentNullException)
                {
                    return;
                }

                //This will check if the note is an uppercase letter, or if the note is in the list of high notes
                bool isOldHighNote = char.IsUpper(character) || AutoPlayer.AlwaysHighNotes.Contains(character);
                bool isNewHighNote = char.IsUpper(newCharacter) || AutoPlayer.AlwaysHighNotes.Contains(newCharacter);

                Note note = new(character, vkOld, isOldHighNote);
                Note newNote = new(newCharacter, vkNew, isNewHighNote);

                if (AutoPlayer.CheckNoteExists(note))
                {
                    //If the note already exists, just update it
                    AutoPlayer.ChangeNote(note, newNote);
                }
                else
                {
                    //If the note does not exist, add a new entry
                    AutoPlayer.AddNote(note, newNote);
                }

                //Update the GUI element to show the delays in the GUI
                UpdateCustomNoteListBox();

                //Update the current notes to with the new rules
                MakeSong();
            }
            catch (AutoPlayerCustomNoteException error)
            {
                MessageBox.Show(error.Message);
            }
        }

        /// <summary>
        /// This is called when we click the RemoveNoteButton
        /// </summary>
        private void RemoveNoteButton_Click(object sender, EventArgs e)
        {
            try
            {
                char character = CustomNoteCharacterBox.Text.ToCharArray()[0];

                WindowsInput.Native.VirtualKeyCode vk;
                try
                {
                    AutoPlayer.VirtualDictionary.TryGetValue(character, out vk);

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
                bool isHighNote = char.IsUpper(character) || AutoPlayer.AlwaysHighNotes.Contains(character);

                Note note = new(character, vk, isHighNote);

                //Remove the note from the dictonary
                AutoPlayer.RemoveNote(note);
                //Update the GUI
                UpdateCustomNoteListBox();

                //Update the current notes wtih the new rules
                MakeSong();
            }
            catch (AutoPlayerCustomNoteException error)
            {
                MessageBox.Show(error.Message);
            }
        }

        /// <summary>
        /// This is called when we click the RemoveAllNotesButton
        /// </summary>
        private void RemoveAllNotesButton_Click(object sender, EventArgs e)
        {
            //Clear the dictonary of custom notes
            AutoPlayer.ResetNotes();

            //Update the GUI
            UpdateCustomNoteListBox();

            //Update the current notes with the new rules
            MakeSong();
        }

        #endregion

        /// <summary>
        /// This is called when we click the ClearNotesButton
        /// </summary>
        private void ClearNotesButton_Click(object sender, EventArgs e)
        {
            AutoPlayer.ClearAllNotes();
            //Updates the note box GUI element to show the notes in the GUI
            UpdateNoteBox();
        }

        private void StartTimeCounter()
        {
            // Start a Timer and update every second a label
            AutoPlayer.Timer = new Timer();
            AutoPlayer.Timer.Interval = 250;
            AutoPlayer.Timer.Tick += (_, _) =>
            {
                // Update the label text
                UpdateStopwatchCounter();
            };
            AutoPlayer.Timer.Start();
            
            //Start the Stopwatch
            AutoPlayer.Stopwatch.Start();
        }

        private void StopTimeCounter()
        {
            //Stop the Timer
            AutoPlayer.Timer.Stop();

            //Stop the Stopwatch
            AutoPlayer.Stopwatch.Stop();
            AutoPlayer.Stopwatch.Reset();
        }

        private void UpdateStopwatchCounter()
        {
            PlayedTimeCurrent.Text = AutoPlayer.Stopwatch.Elapsed.ToString(@"mm\:ss");
        }
        
        /// <summary>
        /// This is called when we click the PlayButton
        /// </summary>
        private void PlayButton_Click(object sender, EventArgs e)
        {
            DisableClearButton();
            DisablePlayButton();
            
            AutoPlayer.Pause = false;

            AutoPlayer.SongThread = new Thread(AutoPlayer.PlaySong);
            AutoPlayer.SongThread.Start();
            
            StartTimeCounter();
        }

        /// <summary>
        /// This is called when we click the StopButton
        /// </summary>
        private void StopButton_Click(object sender, EventArgs e)
        {
            if (AutoPlayer.SongThread == null) return;
            EnableClearButton();
            EnablePlayButton();
            
            AutoPlayer.Pause = false;

            AutoPlayer.SongThread.Abort();
            AutoPlayer.SongThread = null;
            
            StopTimeCounter();
        }

        private void SongStopped()
        {
            if (AutoPlayer.SongThread == null) return;
            EnableClearButton();
            EnablePlayButton();
            
            AutoPlayer.Pause = false;

            AutoPlayer.SongThread.Abort();
            AutoPlayer.SongThread = null;
            
            StopTimeCounter();
        }

        /// <summary>
        /// This is called when we click the LoadButton
        /// </summary>
        private void LoadButton_Click(object sender, EventArgs e)
        {
            OpenFileDialog fileDialog = new OpenFileDialog();

            //This sets the load dialog to filter on .txt files.
            fileDialog.Filter = "Text File | *.txt";

            if (fileDialog.ShowDialog() != DialogResult.OK) return;
            try
            {
                isLoading = true;
                AutoPlayer.ResetDelays();
                AutoPlayer.LoadSong(fileDialog.FileName);
                //Update everything when we are done loading
                UpdateEverything();
                MessageBox.Show("Loading completed");
            }
            catch (AutoplayerLoadFailedException error)
            {
                isLoading = false;
                MessageBox.Show($"Loading failed: {error.Message}");
            }
        }

        /// <summary>
        /// This is called when we click the SaveButton
        /// </summary>
        private void SaveButton_Click(object sender, EventArgs e)
        {
            SaveFileDialog fileDialog = new SaveFileDialog();

            //This sets the save dialog to filter on .txt files.
            fileDialog.Filter = "Text File | *.txt";

            if (fileDialog.ShowDialog() != DialogResult.OK) return;
            AutoPlayer.SaveSong(fileDialog.FileName);
            MessageBox.Show($"Notes saved at {fileDialog.FileName}");
        }

        /// <summary>
        /// This is called when we change the state of the loop checkbox
        /// </summary>
        private void LoopCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            AutoPlayer.Loop = LoopCheckBox.Checked;
        }

        /// <summary>
        /// This is called when the text in StartKeyTextBox is changed
        /// </summary>
        private void StartKeyTextBox_TextChanged(object sender, EventArgs e)
        {
            //Using a try catch here in case the user inputs wrong key binds.
            KeysConverter keysConverter = new KeysConverter();
            try
            {
                //Remember to reset the hooked key to the new one.
                gkh.HookedKeys.Remove(startKey);
                startKey = (Keys) keysConverter.ConvertFromString(StartKeyTextBox.Text.ToString());
                if (startKey == stopKey)
                    MessageBox.Show("This key is already bound to another action!");
                gkh.HookedKeys.Add(startKey);
                PlayButton.Text = $"Start ({StartKeyTextBox.Text.ToString()})";
            }
            catch (ArgumentException)
            {
                MessageBox.Show($"ERROR: Invalid key {StartKeyTextBox.Text.ToString()}");
            }
        }
        
        private void PauseKeyTextBox_TextChanged(object sender, EventArgs e)
        {
            //Using a try catch here in case the user inputs wrong key binds.
            KeysConverter keysConverter = new KeysConverter();
            try
            {
                //Remember to reset the hooked key to the new one.
                gkh.HookedKeys.Remove(pauseKey);
                pauseKey = (Keys) keysConverter.ConvertFromString(PauseKeyTextBox.Text.ToString());
                gkh.HookedKeys.Add(pauseKey);
                PauseButton.Text = $"Start ({PauseKeyTextBox.Text.ToString()})";
            }
            catch (ArgumentException)
            {
                MessageBox.Show($"ERROR: Invalid key {PauseKeyTextBox.Text.ToString()}");
            }
        }

        /// <summary>
        /// This is called when the text in StopKeyTextBox is changed
        /// </summary>
        private void StopKeyTextBox_TextChanged(object sender, EventArgs e)
        {
            //Using a try catch here in case the user inputs wrong key binds.
            KeysConverter keysConverter = new KeysConverter();
            try
            {
                //Remember to reset the hooked key to the new one.
                gkh.HookedKeys.Remove(stopKey);
                stopKey = (Keys) keysConverter.ConvertFromString(StopKeyTextBox.Text.ToString());
                if (stopKey == startKey)
                    MessageBox.Show("This key is already bound to another action!");
                gkh.HookedKeys.Add(stopKey);
                StopButton.Text = $"Stop ({StopKeyTextBox.Text.ToString()})";
            }
            catch (ArgumentException)
            {
                MessageBox.Show($"ERROR: Invalid key {StopKeyTextBox.Text.ToString()}");
            }
        }

        /// <summary>
        /// This is called when the value of NormalDelayBox is changed
        /// </summary>
        private void NormalDelayBox_ValueChanged(object sender, EventArgs e)
        {
            AutoPlayer.DelayAtNormalSpeed = (int) NormalDelayBox.Value;
        }

        private void NoteTextBox_TextChanged(object sender, EventArgs e)
        {
            if (!isLoading)
            {
                MakeSong();
            }

        }
    }
}
