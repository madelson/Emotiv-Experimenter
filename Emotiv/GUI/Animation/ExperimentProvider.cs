using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MCAEmotiv.Interop;
using MCAEmotiv.GUI.Configurations;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using MCAEmotiv.Classification;
using System.Drawing;

namespace MCAEmotiv.GUI.Animation
{
    /// <summary>
    /// A view provider for the main experiment
    /// </summary>
    public class ExperimentProvider : AbstractEnumerable<View>, IViewProvider
    {
        /// <summary>
        /// LOG_BASE_NAME is the base file name for log files
        /// RAW_DATA_BASE_NAME is the base file name for raw data files
        /// DATA_BASE_NAME is the base file name for trial data files
        /// </summary>
        public const string LOG_BASE_NAME = "exp_log", RAW_DATA_BASE_NAME = "exp_data_raw", DATA_BASE_NAME = "exp_data";

        private ExperimentSettings Settings { get; set; }
        private StimulusClass StimulusClass1 { get; set; }
        private StimulusClass StimulusClass2 { get; set; }
        private IEEGDataSource DataSource { get; set; }
        private IArrayView<ClassificationScheme> ClassificationSchemes { get; set; }
        private readonly DateTime startTime = DateTime.Now;

        /// <summary>
        /// Construct an experiment with the given parameters
        /// </summary>
        public ExperimentProvider(ExperimentSettings settings,
            StimulusClass stimulusClass1,
            StimulusClass stimulusClass2,
            IArrayView<ClassificationScheme> classificationSchemes,
            IEEGDataSource dataSource)
        {
            this.Settings = settings;
            this.StimulusClass1 = stimulusClass1;
            this.StimulusClass2 = stimulusClass2;
            this.ClassificationSchemes = classificationSchemes;
            this.DataSource = dataSource;
        }

        /// <summary>
        /// The title of the experiment
        /// </summary>
        public string Title
        {
            get { return this.StimulusClass1.Settings.Name + " vs. " + this.StimulusClass2.Settings.Name; }
        }

        #region ---- Inner Classes ----
        private struct VolatileBool
        {
            public volatile bool value;

            public VolatileBool(bool value) : this() { this.value = value; }
        }

        private class Trial
        {
            public Runtime Runtime { get; private set; }
            public Stimulus Stimulus1 { get; set; }
            public Stimulus Stimulus2 { get; set; }
            public Stimulus TargetStimulus { get { return this.FocusOn1 ? this.Stimulus1 : this.Stimulus2; } }
            public bool IsTraining { get; set; }
            public bool FocusOn1 { get; set; }
            public bool LeftHas1 { get; set; }
            public bool UserPicked1 { get; set; }
            private readonly int trainStimuliRemaining, testStimuliRemaining;
            public int Index
            {
                get
                {
                    return this.IsTraining
                        ? 2 * this.Runtime.Provider.Settings.TrainingImagesPerClass - this.trainStimuliRemaining
                        : this.Runtime.Provider.Settings.TestImages - this.testStimuliRemaining;
                }
            }
            bool succeeded = false;
            public bool Succeeded
            {
                get { return this.succeeded; }
                set
                {
                    if (this.succeeded)
                        throw new Exception("Trial already succeeded");
                    if (this.Runtime.CurrentTrial != this)
                        throw new Exception("Not the current trial");

                    if (this.succeeded = value)
                        this.Runtime.CurrentTrialSucceeded();
                }
            }

            public Trial(Runtime runtime)
            {
                this.Runtime = runtime;
                this.trainStimuliRemaining = this.Runtime.TrainStimuliRemaining;
                this.testStimuliRemaining = this.Runtime.TestStimuliRemaining;
            }
        }

        private class Runtime
        {
            public ExperimentProvider Provider { get; private set; }
            public IArrayView<ClassifierManager> Classifiers { get; set; }
            public BlockingQueue<IArrayView<EEGDataEntry>> TrialDataQueue { get; set; }
            public TextWriter Logger { get; set; }
            public TextWriter TrialLogger { get; set; }
            public Trial CurrentTrial { get; set; }
            private int class1StimuliRemaining,
                class2StimuliRemaining,
                class1TestPicks,
                class2TestPicks;
            public int Class1StimuliRemaining { get { return this.class1StimuliRemaining; } }
            public int Class2StimuliRemaining { get { return this.class2StimuliRemaining; } }
            public int Class1TestPicks { get { return this.class1TestPicks; } }
            public int Class2TestPicks { get { return this.class2TestPicks; } }
            public int TrainStimuliRemaining { get { return this.Class1StimuliRemaining + this.Class2StimuliRemaining; } }
            public int TestStimuliRemaining { get { return this.Provider.Settings.TestImages - this.Class1TestPicks - this.Class2TestPicks; } }

            private readonly Random random = new Random();
            public Random Random { get { return this.random; } }

            public Runtime(ExperimentProvider provider)
            {
                this.Provider = provider;
                this.class1StimuliRemaining = this.class2StimuliRemaining = this.Provider.Settings.TrainingImagesPerClass;
                this.class1TestPicks = this.class2TestPicks = 0;
            }

            public string LogLine(object obj = null)
            {
                string toLog = (obj ?? string.Empty).ToString();

                if (this.Logger != null)
                    try { this.Logger.WriteLine(obj ?? string.Empty); }
                    catch (Exception) { }

                return toLog;
            }

            public void LogTrial(IArrayView<EEGDataEntry> trial)
            {
                if (this.TrialLogger != null)
                    try { this.TrialLogger.WriteLine(trial.ConcatToString(Environment.NewLine)); }
                    catch (Exception) { }
            }

            public void CurrentTrialSucceeded()
            {
                if (!this.CurrentTrial.Succeeded)
                    throw new Exception("Trial did not succeed");

                if (this.CurrentTrial.IsTraining)
                {
                    if (this.CurrentTrial.FocusOn1)
                        this.class1StimuliRemaining--;
                    else
                        this.class2StimuliRemaining--;
                }
                else
                {
                    if (this.CurrentTrial.UserPicked1)
                        this.class1TestPicks++;
                    else
                        this.class2TestPicks++;
                }
            }
        }
        #endregion

        #region ---- Iterator Blocks ----
        /// <summary>
        /// Yields the experiment as a sequence of views
        /// </summary>
        public override IEnumerator<View> GetEnumerator()
        {
            // wait to begin
            yield return new ChoiceView(new string[] { "Click anywhere to begin" });

            var connected = new VolatileBool(true); // assume it's connected
            using (var invoker = new SingleThreadedInvoker())
            using (var connectionListener = new EEGDataListener(invoker, s => connected.value = true, null, s => connected.value = false))
            using (var logger = this.LogExperimentAndGetLogger()) // log the experiment
            using (var trialLogger = this.GetTrialWriter()) // logs each trial
            {
                // create the runtime
                var runtime = new Runtime(this)
                {
                    Classifiers = this.ClassificationSchemes.Select(cs => new ClassifierManager(cs)).ToIArray(),
                    Logger = logger,
                    TrialLogger = trialLogger,
                };

                // listen for a broken connection
                this.DataSource.AddListener(connectionListener);

                foreach (var view in this.GetViews(runtime))
                    if (connected.value)
                        yield return view;
                    else
                    {
                        GUIUtils.Alert(runtime.LogLine("Lost connection to headset!"));
                        break;
                    }

                this.DataSource.RemoveListener(connectionListener);
            }
        }

        private IEnumerable<View> GetViews(Runtime runtime)
        {
            BlockingQueue<IArrayView<EEGDataEntry>> trialDataQueue;
            using (var trialListener = this.GetTrialDataListener(out trialDataQueue)) // create the listeners
            using (var rawDataListener = this.GetRawDataRecordingListener())
            using (var stimuli1 = this.GetStimuli(this.StimulusClass1, runtime.Random)) // create stimulus enumerators
            using (var stimuli2 = this.GetStimuli(this.StimulusClass2, runtime.Random))
            {
                // save the trial queue
                runtime.TrialDataQueue = trialDataQueue;

                // add listeners
                var listeners = new IEEGDataListener[] { trialListener, rawDataListener }.Where(l => l != null);
                this.DataSource.AddListeners(listeners);

                // training phase
                if (this.Settings.TrainingImagesPerClass > 0)
                {
                    yield return new TextView(runtime.LogLine("Training Phase"), this.Settings.InstructionTime);
                    runtime.LogLine();

                    while (runtime.TrainStimuliRemaining > 0)
                    {
                        stimuli1.MoveNext();
                        stimuli2.MoveNext();
                        runtime.CurrentTrial = new Trial(runtime)
                        {
                            IsTraining = true,
                            Stimulus1 = stimuli1.Current,
                            Stimulus2 = stimuli2.Current,
                            FocusOn1 = this.GetFocusTarget(runtime),
                            LeftHas1 = runtime.Random.NextBool()
                        };

                        foreach (var view in this.ProcessStimulusPair(runtime))
                            yield return view;
                        runtime.LogLine();
                    }
                }

                // initial training
                if (runtime.Classifiers.Count > 0)
                {
                    yield return new TrainView(runtime.Classifiers) { Text = runtime.LogLine("Training Classifiers") };
                    runtime.LogLine();
                }

                // test phase
                if (this.Settings.TestImages > 0)
                {
                    yield return new TextView(runtime.LogLine("Test Phase"), this.Settings.InstructionTime);
                    runtime.LogLine();

                    while (runtime.TestStimuliRemaining > 0)
                    {
                        stimuli1.MoveNext();
                        stimuli2.MoveNext();
                        runtime.CurrentTrial = new Trial(runtime)
                        {
                            IsTraining = false,
                            Stimulus1 = stimuli1.Current,
                            Stimulus2 = stimuli2.Current,
                            LeftHas1 = runtime.Random.NextBool()
                        };

                        foreach (var view in this.ProcessStimulusPair(runtime))
                            yield return view;
                        runtime.LogLine();

                        // periodic training
                        if (runtime.CurrentTrial.Succeeded
                            && (runtime.CurrentTrial.Index + 1) % this.Settings.TrainFrequency == 0
                            && runtime.TestStimuliRemaining > 0
                            && runtime.Classifiers.Count > 0)
                        {
                            yield return new TrainView(runtime.Classifiers) { Text = runtime.LogLine("Re-training Classifiers") };
                            runtime.LogLine();
                        }
                    }
                }

                this.DataSource.RemoveListeners(listeners);
            }
        }

        private IEnumerable<View> ProcessStimulusPair(Runtime runtime)
        {
            IViewResult result;

            // rest
            yield return new RestView(this.Settings.RestTime);

            yield return new InstructionView(runtime, this.Settings.InstructionTime);

            // a tiny random rest
            yield return new RestView(runtime.Random.Next(1, 500));

            // fixation
            yield return new FixationView(this.Settings.FixationTime)
            {
                SplitView = !this.Settings.ImageDisplaySettings.SuperimposeImages,
                MaxImageSize = this.Settings.ImageDisplaySettings.ImageSize
            };

            // display the image
            runtime.LogLine(string.Format("Presenting {0} and {1}",
                Path.GetFileNameWithoutExtension(runtime.CurrentTrial.Stimulus1.PathOrText),
                Path.GetFileNameWithoutExtension(runtime.CurrentTrial.Stimulus2.PathOrText)));
            bool swapImageLocations = !this.Settings.ImageDisplaySettings.SuperimposeImages && !runtime.CurrentTrial.LeftHas1;
            yield return new ImageView(this.Settings.DisplayTime, out result)
            {
                ImagePath = (swapImageLocations ? runtime.CurrentTrial.Stimulus2 : runtime.CurrentTrial.Stimulus1).PathOrText,
                SecondaryImagePath = (swapImageLocations ? runtime.CurrentTrial.Stimulus1 : runtime.CurrentTrial.Stimulus2).PathOrText,
                ImageDisplaySettings = this.Settings.ImageDisplaySettings,
                DataSource = this.DataSource,
                Marker = runtime.CurrentTrial.IsTraining
                    ? runtime.CurrentTrial.TargetStimulus.Class.Settings.Marker
                    : EEGDataEntry.MARKER_UNKNOWN
            };
            if (this.Settings.ImageDisplaySettings.Beep)
                yield return new SoundView(System.Media.SystemSounds.Beep);
            if (!(bool)result.Value)
            {
                yield return new TextView(runtime.LogLine("Failed to load image: ignoring trial"), this.Settings.InstructionTime);
                yield break;
            }

            // training
            if (runtime.CurrentTrial.IsTraining)
                foreach (var view in this.DoTrainingClassification(runtime))
                    yield return view;
            else // test
                foreach (var view in this.DoTestClassification(runtime))
                    yield return view;
        }

        private IEnumerable<View> DoTrainingClassification(Runtime runtime)
        {
            IViewResult result = null;

            // ask question
            if (this.Settings.QuestionMode != QuestionMode.None)
            {
                yield return new ChoiceView(new string[] 
                { 
                    runtime.CurrentTrial.TargetStimulus.Class.Settings.Answer1, 
                    runtime.CurrentTrial.TargetStimulus.Class.Settings.Answer2, 
                    GUIUtils.Strings.DONT_KNOW 
                },
                    out result) { Text = runtime.CurrentTrial.TargetStimulus.Class.Settings.Question };

                // don't log this in verify mode, since then the answer is implicit based on the behavior
                if (this.Settings.QuestionMode != QuestionMode.AskAndVerify)
                    runtime.LogLine("User answered " + result.Value);
            }

            // try to get the trial
            IArrayView<EEGDataEntry> trialData;
            if (!this.TryGetTrialData(runtime, out trialData))
                yield return new TextView(runtime.LogLine("Failed to get trial data: ignoring trial"), this.Settings.InstructionTime);
            // either throw out the trial or add it to the classifier
            else if (result != null && (string)result.Value == GUIUtils.Strings.DONT_KNOW)
                yield return new TextView(runtime.LogLine(GUIUtils.Strings.DONT_KNOW + ": ignoring trial"), this.Settings.InstructionTime);
            else if (this.Settings.QuestionMode == QuestionMode.AskAndVerify
                && ((string)result.Value == runtime.CurrentTrial.TargetStimulus.Class.Settings.Answer1) != runtime.CurrentTrial.TargetStimulus.Subclass.Value)
                yield return new TextView(runtime.LogLine("Answer was incorrect: ignoring trial"), this.Settings.InstructionTime);
            else if (this.Settings.ArtifactDetectionSettings.HasMotionArtifact(trialData))
                yield return new TextView(runtime.LogLine("Motion artifact detected: ignoring trial"), this.Settings.InstructionTime);
            else if (trialData != null)
            {
                foreach (var classifier in runtime.Classifiers)
                    classifier.AddTrial(trialData);
                runtime.LogTrial(trialData);

                // record the success
                runtime.CurrentTrial.Succeeded = true;
            }
        }

        private IEnumerable<View> DoTestClassification(Runtime runtime)
        {
            // try to get the trial
            IArrayView<EEGDataEntry> trialData;
            if (!this.TryGetTrialData(runtime, out trialData))
            {
                yield return new TextView(runtime.LogLine("Failed to get trial data: ignoring trial"), this.Settings.InstructionTime);
                yield break;
            }
            if (this.Settings.ArtifactDetectionSettings.HasMotionArtifact(trialData))
            {
                yield return new TextView(runtime.LogLine("Motion artifact detected: ignoring trial"), this.Settings.InstructionTime);
                yield break;
            }

            // predict            
            yield return new PredictView(runtime, this.Settings.QuestionMode, trialData);
            if (!runtime.CurrentTrial.Succeeded)
                yield return new TextView(runtime.LogLine("Answer was incorrect: ignoring trial"), this.Settings.InstructionTime);
            else
            {
                var fixedTrial = trialData.SelectArray(e => e.WithMarker((runtime.CurrentTrial.UserPicked1
                    ? runtime.Provider.StimulusClass1
                    : runtime.Provider.StimulusClass2).Settings.Marker));
                foreach (var classifier in runtime.Classifiers)
                    classifier.AddTrial(fixedTrial);
                runtime.LogTrial(fixedTrial);
            }
        }
        #endregion

        #region ---- Helpers ----
        private bool TryGetTrialData(Runtime runtime, out IArrayView<EEGDataEntry> trial)
        {
            // get rid of excess trials
            while (runtime.TrialDataQueue.Count > 1)
            {
                runtime.TrialDataQueue.Dequeue();
                runtime.LogLine("Found extra trial data!");
            }

            // try to get the trial
            return runtime.TrialDataQueue.TryDequeue(2000, out trial);
        }

        private bool GetFocusTarget(Runtime runtime)
        {
            if (runtime.Class1StimuliRemaining == 0
                && runtime.Class2StimuliRemaining == 0)
                throw new Exception("Ran out of both types!");

            if (runtime.Class1StimuliRemaining == 0)
                return false; // class 2
            else if (runtime.Class2StimuliRemaining == 0)
                return true; // class 1
            else
                return runtime.Random.NextBool();
        }

        /// <summary>
        /// An infinite enumerator that progresses correctly through the stimuli
        /// </summary>
        private IEnumerator<Stimulus> GetStimuli(StimulusClass stimulusClass, Random rand)
        {
            var validStimuli = stimulusClass.UsedStimuli(this.Settings.QuestionMode).ToIArray();
            if (validStimuli.Count == 0)
                throw new Exception("At least 1 valid stimulus required!");

            while (true)
            {
                validStimuli.Shuffle(rand);
                foreach (var stimulus in validStimuli)
                    yield return stimulus;
            }
        }

        private static string GetFileName(string baseName, DateTime startTime, string extension)
        {
            string basePath = string.Format("{0}_{1}-{2}-{3}_{4}-{5}", baseName,
                startTime.Year,
                startTime.Month,
                startTime.Day,
                startTime.Hour,
                startTime.Minute);
            if (File.Exists(basePath + extension))
            {
                int i = 1;
                while (File.Exists(basePath + " " + i + extension))
                    i++;
                return basePath + " " + i + extension;
            }

            return basePath + extension;
        }

        private TextWriter LogExperimentAndGetLogger()
        {
            if (!this.Settings.LogExperiment)
                return null;

            try
            {
                var writer = new StreamWriter(
                    Path.Combine(this.Settings.OutputFolder, GetFileName(LOG_BASE_NAME, this.startTime, GUIUtils.Strings.TEXT_EXTENSION)),
                    false);

                writer.WriteLine(string.Format(GUIUtils.Strings.LOGGING_TITLE_FORMAT, "General Settings"));
                writer.WriteLine();
                writer.WriteLine(this.Settings);
                writer.WriteLine();
                writer.WriteLine(string.Format(GUIUtils.Strings.LOGGING_TITLE_FORMAT, "First Stimulus Class"));
                writer.WriteLine();
                writer.WriteLine(this.StimulusClass1);
                writer.WriteLine();
                writer.WriteLine(string.Format(GUIUtils.Strings.LOGGING_TITLE_FORMAT, "Second Stimulus Class"));
                writer.WriteLine();
                writer.WriteLine(this.StimulusClass2);
                writer.WriteLine();
                writer.WriteLine(string.Format(GUIUtils.Strings.LOGGING_TITLE_FORMAT, "Classifiers"));
                writer.WriteLine();
                foreach (var classifier in this.ClassificationSchemes)
                {
                    writer.WriteLine(classifier);
                    writer.WriteLine();
                }
                writer.WriteLine(string.Format(GUIUtils.Strings.LOGGING_TITLE_FORMAT, "Artifact Detection"));
                writer.WriteLine();
                writer.WriteLine(this.Settings.ArtifactDetectionSettings.UseArtifactDetection
                    ? this.Settings.ArtifactDetectionSettings.PrettyPrint()
                    : "Disabled");
                writer.WriteLine();
                writer.WriteLine(string.Format(GUIUtils.Strings.LOGGING_TITLE_FORMAT, "Beginning Experiment"));
                writer.WriteLine();

                return writer;
            }
            catch (Exception ex)
            {
                GUIUtils.Alert("Failed to log experiment to " + this.Settings.OutputFolder + ": " + ex.Message, MessageBoxIcon.Warning);
                return null;
            }
        }

        private EEGDataListener GetRawDataRecordingListener()
        {
            if (!this.Settings.SaveRawData)
                return null;

            StreamWriter writer;
            try
            {
                writer = new StreamWriter(
                    Path.Combine(this.Settings.OutputFolder, GetFileName(RAW_DATA_BASE_NAME, this.startTime, GUIUtils.Strings.CSV_EXTENSION)),
                    false);
            }
            catch (Exception)
            {
                GUIUtils.Alert("Failed to save raw data to " + this.Settings.OutputFolder, MessageBoxIcon.Warning);
                return null;
            }

            var invoker = new SingleThreadedInvoker();

            return new EEGDataListener(invoker,
                null,
                data =>
                {
                    try { writer.WriteLine(data.ConcatToString(Environment.NewLine)); }
                    catch (Exception) { }
                },
                null,
                () => { writer.Dispose(); invoker.Dispose(); });
        }

        private TextWriter GetTrialWriter()
        {
            if (!this.Settings.SaveTrialData)
                return null;

            try
            {
                return new StreamWriter(
                    Path.Combine(this.Settings.OutputFolder, GetFileName(DATA_BASE_NAME, this.startTime, GUIUtils.Strings.CSV_EXTENSION)),
                    false);
            }
            catch (Exception)
            {
                GUIUtils.Alert("Failed to save trial data to " + this.Settings.OutputFolder, MessageBoxIcon.Warning);
                return null;
            }
        }

        private EEGDataListener GetTrialDataListener(out BlockingQueue<IArrayView<EEGDataEntry>> trialQueue)
        {
            var entryList = new List<EEGDataEntry>();
            var queue = new BlockingQueue<IArrayView<EEGDataEntry>>();
            trialQueue = queue;

            var invoker = new SingleThreadedInvoker();
            return new EEGDataListener(invoker,
                null,
                entries =>
                {
                    foreach (var entry in entries)
                    {
                        // got a new marker, so flush!
                        if (entryList.Count > 0 && entry.Marker != entryList[0].Marker)
                        {
                            queue.Enqueue(entryList.AsIArray());
                            entryList = new List<EEGDataEntry>(entryList.Count);
                        }

                        // save stimulus entries
                        if (entry.HasStimulusMarker())
                            entryList.Add(entry);
                    }
                },
                null,
                () => invoker.Dispose());
        }
        #endregion

        #region ---- Private Views ----
        private class InstructionView : AbstractTimedView
        {
            private const string LEFT = "<-", RIGHT = "->", UNKNOWN = "?";
            private const int MIN_WIDTH = 200;

            public InstructionView(Runtime runtime, int displayTimeMillis)
                : base(displayTimeMillis)
            {
                string title = runtime.LogLine((runtime.CurrentTrial.IsTraining ? "Training" : "Test") + " Trial " + runtime.CurrentTrial.Index);

                string class1Name = runtime.Provider.StimulusClass1.Settings.Name.ToUpper(),
                    class2Name = runtime.Provider.StimulusClass2.Settings.Name.ToUpper(),
                    choice1, choice2, target, leftBox, rightBox;
                if (runtime.Provider.Settings.ImageDisplaySettings.SuperimposeImages)
                {
                    choice1 = class1Name;
                    choice2 = class2Name;
                    target = runtime.CurrentTrial.FocusOn1 ? choice1 : choice2;
                    leftBox = rightBox = string.Empty;
                    if (runtime.CurrentTrial.IsTraining)
                        runtime.LogLine("Target is " + target);
                }
                else // side-by-side
                {
                    choice1 = LEFT;
                    choice2 = RIGHT;
                    target = (runtime.CurrentTrial.FocusOn1 == runtime.CurrentTrial.LeftHas1)
                        ? choice1
                        : choice2;
                    string leftClassName = (runtime.CurrentTrial.LeftHas1 ? class1Name : class2Name),
                        rightClassName = (runtime.CurrentTrial.LeftHas1 ? class2Name : class1Name);
                    leftBox = runtime.Provider.Settings.AllowAnticipation ? leftClassName : UNKNOWN;
                    rightBox = runtime.Provider.Settings.AllowAnticipation ? rightClassName : UNKNOWN;
                    runtime.LogLine(string.Format("Setup is [ {0} {1} {2} ]",
                        leftClassName,
                        runtime.CurrentTrial.IsTraining ? target : "<->",
                        rightClassName));
                }

                var backPanel = this.RegisterDisposable(new Panel() { Dock = DockStyle.Fill });
                var panel = new Panel() { Dock = DockStyle.Fill };
                backPanel.Controls.Add(panel);
                backPanel.Controls.Add(new Label() { Text = title, Font = GUIUtils.Constants.DISPLAY_FONT, TextAlign = ContentAlignment.MiddleCenter, Dock = DockStyle.Top });

                this.DoOnDeploy(c =>
                {
                    if (runtime.Provider.Settings.ImageDisplaySettings.SuperimposeImages)
                    {
                        c.Controls.Add(backPanel);
                        this.DeploySubView(new TextView(runtime.CurrentTrial.IsTraining
                            ? target
                            : string.Format("{0} or {1} (Choose one)", choice1, choice2), -1), panel);
                    }
                    else
                    {
                        panel.Paint += (sender, args) =>
                        {
                            Rectangle rectangle1, rectangle2;
                            GUIUtils.GetSplitModeImageRectangles(panel.ClientRectangle, runtime.Provider.Settings.ImageDisplaySettings.ImageSize, out rectangle1, out rectangle2);

                            RectangleF layoutRectangle = rectangle1;
                            args.Graphics.DrawString(leftBox, GUIUtils.Constants.DISPLAY_FONT_LARGE, Brushes.Black, layoutRectangle,
                                new StringFormat() { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center });

                            layoutRectangle = rectangle2;
                            args.Graphics.DrawString(rightBox, GUIUtils.Constants.DISPLAY_FONT_LARGE, Brushes.Black, layoutRectangle,
                                new StringFormat() { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center });

                            var centerRectangle = new Rectangle(rectangle1.X + rectangle1.Width,
                                0,
                                rectangle2.X - rectangle1.X - rectangle1.Width,
                                panel.ClientSize.Height);
                            if (centerRectangle.Width <= 0)
                            {
                                centerRectangle.X -= (MIN_WIDTH - centerRectangle.Width) / 2;
                                centerRectangle.Width = MIN_WIDTH;
                            }
                            var size = new Size(Math.Min(rectangle1.Width / 2, centerRectangle.Width),
                                Math.Max(GUIUtils.Constants.DISPLAY_FONT_LARGE.Height, rectangle1.Height / 4));
                            var arrowRectangle = new Rectangle(size.CenteredAround(centerRectangle.Center()), size);
                            args.Graphics.DrawArrow(arrowRectangle, runtime.CurrentTrial.IsTraining
                                ? (runtime.CurrentTrial.LeftHas1 == runtime.CurrentTrial.FocusOn1 ? ArrowType.Left : ArrowType.Right)
                                : ArrowType.Bidi);
                        };
                        c.Controls.Add(backPanel);
                    }
                });
            }
        }

        private class PredictView : View
        {
            private class Display
            {
                public string Classifier { get; set; }
                public string Prediction { get; set; }
                public string Confidence { get; set; }
                public double Accuracy { get; set; }
            }

            public string Text { get; set; }

            public PredictView(Runtime runtime, QuestionMode questionMode, IArrayView<EEGDataEntry> trial)
                : base()
            {
                var panel = this.RegisterDisposable(new Panel { Dock = DockStyle.Fill });

                var displayList = runtime.Classifiers.Select(c => new Display()
                {
                    Classifier = c.Settings.Name,
                    Prediction = "predicting...",
                    Confidence = c.Classifier.ComputesConfidence ? "predicting... " : "n/a",
                    Accuracy = c.Accuracy
                }).ToList();
                var grid = new DataGridView()
                {
                    Dock = DockStyle.Fill,
                    EditMode = DataGridViewEditMode.EditProgrammatically,
                    DataSource = displayList,
                    Font = GUIUtils.Constants.DISPLAY_FONT,
                    AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                    ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize,
                };
                panel.Controls.Add(grid);

                int count = runtime.Classifiers.Count;
                Stimulus selectedStimulus = null;
                Func<bool> succeeded = () => false;
                this.DoOnDeploy(c =>
                {
                    if (!string.IsNullOrWhiteSpace(this.Text))
                        panel.Controls.Add(this.Text.ToLabel(DockStyle.Top, ContentAlignment.MiddleCenter));
                    c.Controls.Add(panel);

                    foreach (int index in (runtime.Classifiers.Count > 0 ? runtime.Classifiers.Indices() : (0).Enumerate()))
                    {
                        int i = index;
                        ThreadPool.QueueUserWorkItem(ignored =>
                        {
                            double confidence = 0;
                            StimulusClass predicted;
                            try
                            {
                                predicted = runtime.Classifiers[i].Predict(trial, out confidence) == runtime.Provider.StimulusClass1.Settings.Marker
                                    ? runtime.Provider.StimulusClass1
                                    : runtime.Provider.StimulusClass2;
                            }
                            catch (Exception) { predicted = runtime.Provider.StimulusClass1; }

                            this.Invoke(() =>
                            {
                                if (displayList.Count > 0)
                                {
                                    displayList[i].Prediction = predicted.Settings.Name;
                                    displayList[i].Confidence = runtime.Classifiers[i].Classifier.ComputesConfidence
                                        ? confidence.ToString()
                                        : "n/a";
                                    grid.InvalidateRow(i);
                                }

                                if (--count > 0)
                                    return;

                                // log prediction results
                                foreach (var display in displayList)
                                    runtime.LogLine(string.Format("Classifier {0} predicted {1} (accuracy so far = {2})", display.Classifier, display.Prediction, display.Accuracy));

                                Panel choicePanel1 = new Panel() { Dock = DockStyle.Bottom },
                                    choicePanel2 = new Panel() { Dock = DockStyle.Bottom, Visible = false };
                                var choicesSoFarLabel = new Label() { Dock = DockStyle.Bottom, TextAlign = ContentAlignment.MiddleCenter, Font = GUIUtils.Constants.DISPLAY_FONT, Visible = false };

                                var classChoice = new ChoiceView(new string[] { runtime.Provider.StimulusClass1.Settings.Name, runtime.Provider.StimulusClass2.Settings.Name })
                                {
                                    Text = "Which did you choose?"
                                };
                                classChoice.DoOnFinishing(() =>
                                {
                                    if (!classChoice.Result.HasValue)
                                        return;
                                    choicePanel1.Enabled = false;

                                    bool picked1 = classChoice.Result.Value.Equals(runtime.Provider.StimulusClass1.Settings.Name);

                                    choicesSoFarLabel.Text = string.Format("(Choices so far: {0} {1}/{2} {3})",
                                        runtime.Class1TestPicks + (picked1 ? 1 : 0),
                                        runtime.Provider.StimulusClass1.Settings.Name,
                                        runtime.Class2TestPicks + (picked1 ? 0 : 1),
                                        runtime.Provider.StimulusClass2.Settings.Name);
                                    choicesSoFarLabel.Visible = true;

                                    selectedStimulus = picked1 ? runtime.CurrentTrial.Stimulus1 : runtime.CurrentTrial.Stimulus2;
                                    if (questionMode == QuestionMode.None)
                                    {
                                        succeeded = () => true;
                                        this.DeploySubView(new RestView(750), choicePanel1, true);
                                    }
                                    else
                                    {
                                        var choiceView = new ChoiceView(new string[] 
                                        { 
                                            selectedStimulus.Class.Settings.Answer1,
                                            selectedStimulus.Class.Settings.Answer2,
                                            GUIUtils.Strings.DONT_KNOW
                                        });
                                        succeeded = () => questionMode == QuestionMode.AskAndVerify
                                            ? selectedStimulus.SubclassString.Equals(choiceView.Result.Value)
                                            : !GUIUtils.Strings.DONT_KNOW.Equals(choiceView.Result.Value);
                                        choicePanel2.Visible = true;
                                        this.DeploySubView(choiceView, choicePanel2);
                                    }
                                });
                                this.DeploySubView(classChoice, choicePanel1, false);

                                panel.Controls.Add(choicePanel1);
                                panel.Controls.Add(choicesSoFarLabel);
                                panel.Controls.Add(new Label() { Dock = DockStyle.Bottom });
                                panel.Controls.Add(choicePanel2);
                            });
                        });
                    }
                });

                this.DoOnFinishing(() =>
                {
                    if (succeeded())
                    {
                        for (int i = 0; i < displayList.Count; i++)
                            runtime.Classifiers[i].RecordResult(selectedStimulus.Class.Settings.Marker,
                                displayList[i].Prediction == runtime.Provider.StimulusClass1.Settings.Name
                                    ? runtime.Provider.StimulusClass1.Settings.Marker
                                    : runtime.Provider.StimulusClass2.Settings.Marker, 0);
                        runtime.CurrentTrial.UserPicked1 = (selectedStimulus == runtime.CurrentTrial.Stimulus1);
                        runtime.CurrentTrial.Succeeded = true;
                        runtime.LogLine("User chose " + selectedStimulus.Class.Settings.Name);
                    }
                });
            }
        }
        #endregion
    }
}
