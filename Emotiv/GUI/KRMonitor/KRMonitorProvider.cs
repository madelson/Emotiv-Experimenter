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
            Random numgen = new Random();
            int a, b;
            RandomizedQueue<string> pres = new RandomizedQueue<string>();
            RandomizedQueue<string> usedPres = new RandomizedQueue<string>();
            pres.AddRange(presentation);
            using (var logWriter = new StreamWriter(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "krmon_log_" + settings.SubjectName + "_" + DateTime.Now.ToString("MM dd yyyy H mm ss") + ".txt")))
            using (var dataWriter = new StreamWriter(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "krmon_data_" + settings.SubjectName + "_" + DateTime.Now.ToString("MM dd yyyy H mm ss") + ".csv")))
            {
                //Alternating Study and Test Phases
                for (int i = 0; i < settings.NumRounds; i++)
                {
                    yield return new ChoiceView(new string[] 
                { 
                    "Start Study Phase"
                }, out result) { Text = "Click When Ready" };

                    while (pres.Count > 0)
                    {
                        var stimulus = pres.RemoveRandom();
                        yield return new TextView(stimulus, this.settings.PresentationTime, GUIUtils.Constants.DISPLAY_FONT_LARGE);
                        yield return new RestView(this.settings.RestTime);
                        usedPres.Add(stimulus);
                    }
                    pres.AddRange(usedPres);
                    usedPres.Clear();

                    a = numgen.Next(4, 13);
                    b = numgen.Next(4, 13);

                    yield return new VocabView(string.Format("{0} x {1} = {2}", a, b, a * b), "Verify", settings.DisplayTime, settings.DelayTime, true, out result);

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
                        foreach (var view in this.GetViews(invoker, logWriter, dataWriter, i, pres))
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
        }

        private IEnumerable<View> GetViews(ISynchronizeInvoke invoker, StreamWriter logWriter, StreamWriter dataWriter, int round, RandomizedQueue<string> pres)
        {
            var currentTrialEntries = new List<EEGDataEntry>();
            //To do: Save the date/time earlier and use it for both this and the dataWriter. Put it in GetEnumerator and pass to GetViews

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

                var stimulusPairs = new RandomizedQueue<StudyTestPair>();
                stimulusPairs.AddRange(this.stp);
                
                while (stimulusPairs.Count > 0)
                {
                    StudyTestPair currstp = stimulusPairs.RemoveRandom();
                    usedPairs.Add(currstp);
                    //logWriter.WriteLine("Question: " + currstp.index);
                    foreach (var view in RunTrial(round, currstp.test, currstp.answer, currstp.index, dataWriter, logWriter, currentTrialEntries, pres))
                    {
                        yield return view;
                    }

                }
                stimulusPairs.AddRange(usedPairs);
            }
        }


        public IEnumerable<View> RunTrial(int index, string tst, string ans, int numstim, StreamWriter dataWriter, StreamWriter logWriter, List<EEGDataEntry> currentTrialEntries, RandomizedQueue<string> pres)
        {
            yield return new RestView(this.settings.BlinkTime);
            yield return new FixationView(this.settings.FixationTime);
            IViewResult result;
            var vocabView = new VocabView(tst, ans, settings.DisplayTime, settings.DelayTime, false, out result);
            vocabView.DoOnDeploy(c => this.dataSource.Marker = index+1);
            //bool needToRerun = false;

            vocabView.DoOnFinishing(() =>
            {
                this.dataSource.Marker = EEGDataEntry.MARKER_DEFAULT;
                lock (currentTrialEntries)
                {
                    var trialsDuringDelay = currentTrialEntries.Where(e => e.RelativeTimeStamp <= settings.DelayTime);
                    if (this.settings.ArtifactDetectionSettings.HasMotionArtifact(trialsDuringDelay))
                    {
                        logWriter.WriteLine("Motion Artifact Detected");
                        //needToRerun = true;
                    }
                    else
                    {
                        if (this.settings.SaveTrialData)
                        {
                            foreach (var entry in trialsDuringDelay)
                            {
                                dataWriter.WriteLine(entry + ", {0}", numstim);
                            }
                        }

                    }
                    currentTrialEntries.Clear();
                }
            });
            yield return vocabView;
            //No feedback in original Karpicke and Roediger, but I'm leaving the option 
            //yield return new TextView((bool)result.Value ? "Correct" : "Incorrect", settings.FeedbackTime, GUIUtils.Constants.DISPLAY_FONT);
            int towrite = 0;
            if ((bool)result.Value)
            {
                pres.Remove(tst + Environment.NewLine + ans);
                towrite = 1;
            }
            
            logWriter.WriteLine(numstim + ", " + towrite);
            //if (needToRerun)
            //{
            //    foreach (var view in RunTrial(index, tst, ans, dataWriter, logWriter, currentTrialEntries, pres))
            //    {
            //        yield return view;
            //    }
            //}
        }

    }
}
