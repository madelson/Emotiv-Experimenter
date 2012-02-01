using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MCAEmotiv.GUI.Animation;
using System.Threading;
using MCAEmotiv.Interop;
using MCAEmotiv.Common;
using System.IO;
using System.ComponentModel;

namespace MCAEmotiv.GUI.KRMonitor
{
    class KRMonitorProvider : AbstractEnumerable<View>, IViewProvider
    {
        private readonly IArrayView<string> presentation;
        private readonly RandomizedQueue<StudyTestPair> stp;
        private readonly KRMonitorSettings settings;
        private readonly IEEGDataSource dataSource;
        public KRMonitorProvider(IArrayView<string> presentation, RandomizedQueue<StudyTestPair> stp,
            KRMonitorSettings settings, IEEGDataSource dataSource)
        {
            this.presentation = presentation;
            this.stp = stp;
            this.settings = settings;
            this.dataSource = dataSource;
        }
        public string Title
        {
            get { return "New KR Monitor"; }
        }

        /// <summary>
        /// The enumerator implementation
        /// </summary>
        public override IEnumerator<View> GetEnumerator()
        {
            IViewResult result;

            //Alternating Study and Test Phases
            for (int i = 0; i < settings.NumRounds; i++)
            {
                yield return new ChoiceView(new string[] 
                { 
                    "Start Study Phase"
                }, out result) { Text = "Click When Ready" };

                foreach (var stimulus in presentation)
                {
                    yield return new TextView(stimulus, this.settings.PresentationTime, GUIUtils.Constants.DISPLAY_FONT_LARGE);
                    yield return new RestView(this.settings.RestTime);
                }
                yield return new ChoiceView(new string[] 
                { 
                    "Start Test Phase"
                }, out result) { Text = "Click When Ready" };

                var connected = true; // assume it's connected
                using (var invoker = new SingleThreadedInvoker())
                using (var connectionListener = new EEGDataListener(invoker, s => connected = true, null, s => connected = false))
                {
                    // listen for a broken connection
                    this.dataSource.AddListener(connectionListener);
                    foreach (var view in this.GetViews(invoker))
                        if (connected)
                            yield return view;
                        else
                        {
                            GUIUtils.Alert("Lost connection to headset!");
                            break;
                        }

                    this.dataSource.RemoveListener(connectionListener);
                }
            }
        }

        private IEnumerable<View> GetViews(ISynchronizeInvoke invoker)
        {
            var currentTrialEntries = new List<EEGDataEntry>();
            //To do: Save the date/time earlier and use it for both this and the dataWriter. Put it in GetEnumerator and pass to GetViews
            /*
             * NOTE MA 1/31/12 we're creating a new logWriter on each call to GetViews, which gets called multiple times in GetEnumerator (its in a loop)
             * to avoid creating multiple files, we can move this using statement into GetEnumerator (outside the loop) and pass it in as an argument
             * to GetViews instead. The same goes for the dataWriter. If you feel like you're passing around too many arguments, feel free to lump them
             * together in a Runtime object like I do in ExperimentProvider.cs
             */
            using (var logWriter = new StreamWriter(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "krmon_log_" + settings.SubjectName + "_" + DateTime.Now.ToString("MM dd yyyy H mm ss") + ".txt")))
            using (var dataWriter = new StreamWriter(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "krmon_" + settings.SubjectName + "_" + DateTime.Now.ToString("MM dd yyyy H mm ss") + ".csv")))
            using (var artifactListener = new EEGDataListener(invoker, null, data =>
            {
                foreach (var entry in data)
                {
                    if (entry.HasStimulusMarker())
                    {
                        lock (currentTrialEntries)
                        {
                            currentTrialEntries.Add(entry);
                        }
                    }
                }

            }, null))
            {
                this.dataSource.AddListener(artifactListener);
                //DIFFERENCE: Instead of getting blocks, we want to go through the entire set once for each test phase, but we
                //want items within that set to be random
                RandomizedQueue<StudyTestPair> usedPairs = new RandomizedQueue<StudyTestPair>();
                /* 
                 * NOTE MA 1/31/12 this loop seems a little sketchy. We're ostensibly counting from 0 to stp.Count, but stp.Count
                 * will decrement on each call to RemoveRandom(), so we'll only get half of our stimuli. Consider while (this.stp.Count > 0)
                 * or int count = this.stp.Count; for (int i = 0; i < count; i++)
                 */
                /*
                 * NOTE MA 1/31/12 another thing to note here is that you probably don't want to modify stp because it is global to the provider,
                 * not just to this particular run. That means that you risk getting into an inconsistent state if the run restarts or hits an error.
                 * It would probably better to make this.stp an IArrayView (which cannot be modified) and do:
                 * 
                 * var stimulusPairs = new RandomizedQueue<StudyTestPair>();
                 * stimulusPairs.AddRange(this.stp);
                 * 
                 * and then work with stimulusPairs.
                 */
                for (int index = 0; index < this.stp.Count; index++)
                {
                    StudyTestPair currstp = stp.RemoveRandom();
                    usedPairs.Add(currstp);
                    logWriter.WriteLine("Question: " + currstp.test);
                    logWriter.WriteLine("Correct Answer: " + currstp.answer);
                    foreach (var view in RunTrial(index, currstp.test, currstp.answer, dataWriter, logWriter, currentTrialEntries))
                    {
                        yield return view;
                    }

                }
                stp.AddRange(usedPairs);
            }
        }


        public IEnumerable<View> RunTrial(int index, string tst, string ans, StreamWriter dataWriter, StreamWriter logWriter, List<EEGDataEntry> currentTrialEntries)
        {
            yield return new RestView(this.settings.BlinkTime);
            yield return new FixationView(this.settings.FixationTime);
            //DIFFERENCE: use a vocabView instead of a textview
            IViewResult result;
            var vocabView = new VocabView(tst, ans, settings.DisplayTime, 1500, out result);
            vocabView.DoOnDeploy(c => this.dataSource.Marker = index);
            bool needToRerun = false;
            vocabView.DoOnFinishing(() =>
            {
                this.dataSource.Marker = EEGDataEntry.MARKER_DEFAULT;
                lock (currentTrialEntries)
                {
                    if (this.settings.ArtifactDetectionSettings.HasMotionArtifact(currentTrialEntries))
                    {
                        logWriter.WriteLine("Motion Artifact Detected");
                        needToRerun = true;
                    }
                    else
                    {
                        if (this.settings.SaveTrialData)
                        {
                            foreach (var entry in currentTrialEntries)
                            {
                                dataWriter.WriteLine(entry);
                            }
                        }

                    }
                    currentTrialEntries.Clear();
                }
            });
            yield return vocabView;
            //DIFFERENCE: provide the user and the log with feedback 
            yield return new TextView((bool)result.Value ? "Correct" : "Incorrect", 2000);
            logWriter.WriteLine("User Answer: " + result.Value);
            if (needToRerun)
            {
                foreach (var view in RunTrial(index, tst, ans, dataWriter, logWriter, currentTrialEntries))
                {
                    yield return view;
                }
            }
        }

    }
}
