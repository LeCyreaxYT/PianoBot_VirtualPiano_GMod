using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace PianoBot_VirtualPiano_GMod.Core.Interfaces
{
    internal class SavingModel
    {

        [JsonProperty("SaveVersion")]
        public string? SaveVersion { get; set; }
        
        [JsonProperty("CustomBpm")]
        public int CustomBpm { get; set; }
        

        // private int CustomNotesLength { get; set; }
        // private string CustomNotes { get; set; }
        
        [JsonProperty("NotesLength")]
        public int NotesLength { get; set; }
        
        [JsonProperty("Notes")]
        public string? Notes { get; set; }
        
        public string GetSerailizedData()
        {
            return JsonConvert.SerializeObject(new
            {
                SaveVersion,
                CustomBpm,
                NotesLength,
                Notes
            }, Formatting.Indented);
        }
        
        
    }
}