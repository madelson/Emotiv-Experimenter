using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using MCAEmotiv.Interop;

namespace MCAEmotiv.GUI.Configurations
{
    /// <summary>
    /// Represents a text or image stimulus
    /// </summary>
    [Serializable]
    [Description("A stimulus to be displayed to the user")]
    public class Stimulus
    {
        /// <summary>
        /// The file where this stimulus is stored or the text comprising the stimulus
        /// </summary>
        [Parameter("The file where this stimulus is stored or the text comprising the stimulus", DisplayName = "File", DefaultValue = "")]
        public string PathOrText { get; set; }

        /// <summary>
        /// Should this stimulus be used in experiments?
        /// </summary>
        [Parameter("Should this stimulus be used in experiments?", DisplayName = "Use Stimulus", DefaultValue = true)]
        public bool Used { get; set; }

        /// <summary>
        /// True if the stimulus subclass corresponds to the class's first answer, false if the subclass
        /// corresponds to the second answser, and null otherwise. Not a parameter
        /// </summary>
        public bool? Subclass { get; set; }

        /// <summary>
        /// The string corresponding to the stimulus's subclass. Not a parameter
        /// </summary>
        public string SubclassString
        {
            get { return this.Subclass.HasValue ? (this.Subclass.Value ? this.Class.Settings.Answer1 : this.Class.Settings.Answer2) : GUIUtils.Strings.UNCLASSIFIED; }
        }

        /// <summary>
        /// The stimulus class with which the stimulus is associated. Not a parameter
        /// </summary>
        public StimulusClass Class { get; private set; }

        /// <summary>
        /// Construct a Stimulus from the given class
        /// </summary>
        /// <param name="stimulusClass"></param>
        public Stimulus(StimulusClass stimulusClass)
        {
            this.Class = stimulusClass;
            this.SetParametersToDefaultValues();
        }
    }

    /// <summary>
    /// Stimulus class configuration
    /// </summary>
    [Serializable]
    [Description("Stimulus class configuration", DisplayName = "Settings")]
    public class StimulusClassSettings
    {
        /// <summary>
        /// A name used to identify the stimulus class
        /// </summary>
        [Parameter("A name used to identify the stimulus class", DefaultValue = "")]
        public string Name { get; set; }
        
        /// <summary>
        /// The tag used to identify this class when recording data
        /// </summary>
        [Parameter("The tag used to identify this class when recording data", DefaultValue = 1, MinValue = 1)]
        public int Marker { get; set; }

        /// <summary>
        /// A question which can be used to separate the images in the class into two categories
        /// </summary>
        [Parameter("A question which can be used to separate the images in the class into two categories", DefaultValue = "")]
        public string Question { get; set; }

        /// <summary>
        /// One of the possible answers to the class's question
        /// </summary>
        [Parameter("One of the possible answers to the class's question", DisplayName = "Answer 1", DefaultValue = "")]
        public string Answer1 { get; set; }

        /// <summary>
        /// One of the possible answers to the class's question
        /// </summary>
        [Parameter("One of the possible answers to the class's question", DisplayName = "Answer 2", DefaultValue = "")]
        public string Answer2 { get; set; }

        /// <summary>
        /// Construct a settings object with the default parameter values
        /// </summary>
        public StimulusClassSettings() { this.SetParametersToDefaultValues(); }
    }

    /// <summary>
    /// Represents a class of stimuli
    /// </summary>
    [Serializable]
    public class StimulusClass
    {
        /// <summary>
        /// EXTENSION is the file extension used when this object is serialized
        /// NAME is the file name used when this object is serialized
        /// </summary>
        public const string EXTENSION = ".stimclass", NAME = GUIUtils.Strings.APP_NAME + " settings";

        /// <summary>
        /// The folder where the stimuli from this class reside
        /// </summary>
        public string SourceFolder { get; private set; }

        /// <summary>
        /// The path used when this object is serialized
        /// </summary>
        public string SavePath { get { return Path.Combine(this.SourceFolder, NAME + EXTENSION); } }

        /// <summary>
        /// The settings object for this class
        /// </summary>
        public StimulusClassSettings Settings { get; set; }

        private readonly SortedDictionary<string, Stimulus> stimuli = new SortedDictionary<string, Stimulus>();
        /// <summary>
        /// The stimuli in this class
        /// </summary>
        public SortedDictionary<string, Stimulus>.ValueCollection Stimuli { get { return this.stimuli.Values; } }

        private StimulusClass(string sourceFolder)
        {
            this.SourceFolder = sourceFolder;
            this.Settings = new StimulusClassSettings();
            try { this.Settings.Name = Path.GetFileName(this.SourceFolder); }
            catch (Exception) { this.Settings.Name = this.SourceFolder; }
            this.RefreshStimuli();
        }

        /// <summary>
        /// Retrieves the stimuli from this class that are valid when used with the given question mode
        /// </summary>
        public IEnumerable<Stimulus> UsedStimuli(QuestionMode questionMode)
        {
            var usedStimuli = this.Stimuli.Where(s => s.Used);
            
            return questionMode == QuestionMode.AskAndVerify
                ? usedStimuli.Where(s => s.Subclass != null)
                : usedStimuli;
        }

        /// <summary>
        /// Reloads all stimuli from disk
        /// </summary>
        public void RefreshStimuli()
        {
            try
            {
                if (!Directory.Exists(this.SourceFolder))
                {
                    this.stimuli.Clear();
                    return;
                }

                var currentStimuli = new HashSet<string>((from fn in Directory.EnumerateFiles(this.SourceFolder)
                                                   let extension = Path.GetExtension(fn)
                                                   let isText = extension == GUIUtils.Strings.TEXT_EXTENSION
                                                   let isImage = GUIUtils.Strings.ImageExtensions.Contains(extension)
                                                   where isText || isImage
                                                   select isText
                                                     ? File.ReadAllLines(fn).Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.Trim())
                                                     : fn.Enumerate()).Concatenated());
                foreach (var toRemove in this.stimuli.Where(p => !currentStimuli.Contains(p.Key)).ToArray())
                    this.stimuli.Remove(toRemove.Key);
                foreach (var toAdd in currentStimuli.Where(s => !this.stimuli.ContainsKey(s)))
                    this.stimuli[toAdd] = new Stimulus(this) { PathOrText = toAdd, Subclass = null };
            }
            catch (Exception) { /* give up */ }
        }

        /// <summary>
        /// Returns a loggable textual representation of this object
        /// </summary>
        public override string ToString()
        {
            return ("Source Folder=" + this.SourceFolder)
                .Then("Save Path=" + this.SavePath)
                .Then("Settings {")
                .Then(this.Settings.PrettyPrint().Indent())
                .Then("}")
                .Then("Stimuli {")
                .Concat(this.Stimuli.Select(s => string.Format("\t{0} ({1})",
                    GUIUtils.Strings.ImageExtensions.Contains(Path.GetExtension(s.PathOrText)) ? Path.GetFileName(s.PathOrText) : s.PathOrText,
                    s.Subclass == null ? GUIUtils.Strings.UNCLASSIFIED : ((bool)s.Subclass ? this.Settings.Answer1 : this.Settings.Answer2))))
                .ConcatToString(Environment.NewLine);
        }

        /// <summary>
        /// Returns true if this object was successfully saved to disk
        /// </summary>
        public bool TrySave()
        {
            return this.TrySerializeToFile(this.SavePath);
        }

        /// <summary>
        /// Might path be used to load a stimulus class?
        /// </summary>
        public static bool IsValidLoadPath(string path)
        {
            return Directory.Exists(path) || (File.Exists(path) && Path.GetExtension(path) == EXTENSION);
        }

        /// <summary>
        /// Returns true if a stimulus class was successfully loaded from path
        /// </summary>
        public static bool TryLoad(string path, out StimulusClass stimulusClass)
        {
            if (Directory.Exists(path))
            {
                var saveFile = Directory.EnumerateFiles(path)
                    .Where(f => Path.GetExtension(f) == EXTENSION)
                    .FirstOrDefault();

                // try to load a saved version
                if (saveFile != null && Utils.TryDeserializeFile(saveFile, out stimulusClass))
                {
                    stimulusClass.RefreshStimuli();
                    // this should always be true, so we'll assure it just in case
                    // a serialization versioning issue messed things up
                    foreach (var pair in stimulusClass.stimuli)
                        pair.Value.PathOrText = pair.Key;
                    return true;
                }

                // load a new config from the directory
                stimulusClass = new StimulusClass(path);
                return true;
            }

            // else attempt to deserialize path as a save file
            if (Utils.TryDeserializeFile(path, out stimulusClass))
                return true;

            stimulusClass = null;
            return false;
        }
    }
}
