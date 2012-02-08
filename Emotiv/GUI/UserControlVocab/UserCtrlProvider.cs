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

namespace MCAEmotiv.GUI.UserControlVocab
{
    //Runs the User Control Vocab software. No EEG necessary, commented because this will be a basis for the Adaptive
    class UserCtrlProvider : AbstractEnumerable<View>, IViewProvider
    {
        private readonly IArrayView<string> presentation;
        private readonly RandomizedQueue<StudyTestPair> stp;
        private readonly UserCtrlSettings settings;
        //private readonly IEEGDataSource dataSource;
        public UserCtrlProvider(IArrayView<string> presentation, RandomizedQueue<StudyTestPair> stp,
            UserCtrlSettings settings)//, IEEGDataSource dataSource)
        {
            this.presentation = presentation;
            this.stp = stp;
            this.settings = settings;
            //this.dataSource = dataSource;
        }
        public string Title
        {
            get { return "New User Control"; }
        }

        /// <summary>
        /// The enumerator implementation
        /// </summary>
        public override IEnumerator<View> GetEnumerator()
        {
            IViewResult result;
            RandomizedQueue<string> pres = new RandomizedQueue<string>();
            RandomizedQueue<string> usedPres = new RandomizedQueue<string>();
            RandomizedQueue<StudyTestPair> studySoon = new RandomizedQueue<StudyTestPair>();
            RandomizedQueue<StudyTestPair> testSoon = new RandomizedQueue<StudyTestPair>();
            RandomizedQueue<StudyTestPair> testLate = new RandomizedQueue<StudyTestPair>();
            pres.AddRange(presentation);
            testSoon.AddRange(stp);
            yield return new ChoiceView(new string[] 
                { 
                    "Start Initial Study Phase"
                }, out result) { Text = "Click When Ready" };

            while (pres.Count > 0)
            {
                var stimulus = pres.RemoveRandom();
                yield return new TextView(stimulus, this.settings.PresentationTime, GUIUtils.Constants.DISPLAY_FONT_LARGE);
                yield return new RestView(this.settings.RestTime);
                usedPres.Add(stimulus);
            }
            pres.AddRange(usedPres);
            using (var logWriter = new StreamWriter(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "userctrl_log_" + settings.SubjectName + "_" + DateTime.Now.ToString("MM dd yyyy H mm ss") + ".txt")))
            //using (var dataWriter = new StreamWriter(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "userctrl_" + settings.SubjectName + "_" + DateTime.Now.ToString("MM dd yyyy H mm ss") + ".csv")))
            {

                yield return new ChoiceView(new string[] 
                { 
                    "Start Practice Phase"
                }, out result) { Text = "Click When Ready" };
                
                //var connected = true; // assume it's connected
                using (var invoker = new SingleThreadedInvoker())
                //using (var connectionListener = new EEGDataListener(invoker, s => connected = true, null, s => connected = false))
                {
                //    // listen for a broken connection
                //    this.dataSource.AddListener(connectionListener);
                    foreach (var view in this.GetViews(invoker, logWriter, //dataWriter, 
                        studySoon, testSoon, testLate))
                //        if (connected)
                            yield return view;
                //        else
                //        {
                //            GUIUtils.Alert("Lost connection to headset!");
                //            break;
                //        }
                //    this.dataSource.RemoveListener(connectionListener);
                }

            }
        }

        private IEnumerable<View> GetViews(ISynchronizeInvoke invoker, StreamWriter logWriter, //StreamWriter dataWriter,
            RandomizedQueue<StudyTestPair> studySoon, RandomizedQueue<StudyTestPair> testSoon, RandomizedQueue<StudyTestPair> testLate)
        {
            //var currentTrialEntries = new List<EEGDataEntry>();
            //using (var artifactListener = new EEGDataListener(invoker, null, data =>
            //{
            //    foreach (var entry in data)
            //    {
            //        if (entry.HasStimulusMarker())
            //        {
            //            lock (currentTrialEntries)
            //            {
            //                currentTrialEntries.Add(entry);
            //            }
            //        }
            //    }

            //}, null))
            //{
            //    this.dataSource.AddListener(artifactListener);


            //Presents stimuli from the three categories randomly with set probabilities
                Random numgen = new Random();

                for (int index = 0; index < settings.NumTrials; index++)
                {
                    double choice = numgen.NextDouble();
                    if ((choice < 0.1) && !testLate.IsEmpty())
                    {
                        StudyTestPair currstp = testLate.RemoveRandom();
                        logWriter.WriteLine("Question: " + currstp.test);
                        logWriter.WriteLine("Correct Answer: " + currstp.answer);
                        foreach (var view in RunTrial(index, false, currstp.test, currstp.answer, //dataWriter, 
                            logWriter, //currentTrialEntries, 
                            studySoon, testSoon, testLate))
                        {
                            yield return view;
                        }
                    }
                    else if ((choice < 0.4) && !studySoon.IsEmpty())
                    {
                        StudyTestPair study = studySoon.RemoveRandom();
                        logWriter.WriteLine("Study Trial");
                        logWriter.WriteLine(study.test);
                        logWriter.WriteLine(study.answer);
                        foreach (var view in RunTrial(index, true, study.test, study.answer, //dataWriter, 
                            logWriter, //currentTrialEntries, 
                            studySoon, testSoon, testLate))
                        {
                            yield return view;
                        }
                    }
                    else if (!testSoon.IsEmpty())
                    {
                        StudyTestPair currstp = testSoon.RemoveRandom();
                        logWriter.WriteLine("Question: " + currstp.test);
                        logWriter.WriteLine("Correct Answer: " + currstp.answer);
                        foreach (var view in RunTrial(index, false, currstp.test, currstp.answer, //dataWriter, 
                            logWriter, //currentTrialEntries, 
                            studySoon, testSoon, testLate))
                        {
                            yield return view;
                        }
                    }
                    else
                        yield break;


                }
            }
        


        public IEnumerable<View> RunTrial(int index, bool isStudy, string tst, string ans, // StreamWriter dataWriter, 
            StreamWriter logWriter, //List<EEGDataEntry> currentTrialEntries, 
    RandomizedQueue<StudyTestPair> studySoon, 
            RandomizedQueue<StudyTestPair> testSoon, RandomizedQueue<StudyTestPair> testLate)
        {
            yield return new RestView(this.settings.BlinkTime);
            yield return new FixationView(this.settings.FixationTime);
            IViewResult result;
            View vocabView;
            if (isStudy)
            {
                vocabView = new TextView(tst + Environment.NewLine + ans, settings.DisplayTime, GUIUtils.Constants.DISPLAY_FONT_LARGE);
            }
            else
            {
                vocabView = new VocabView(tst, ans, settings.DisplayTime, settings.DelayTime, false, out result);
            }
            //vocabView.DoOnDeploy(c => this.dataSource.Marker = index);
            //bool needToRerun = false;
            //ISSUE: first, the artifact detection should stop after the delay is over. Secondly, the artifact detection is throwing an
            //exception (see comment keyphrase "THROWING EXCEPTION" in AnalysisExtensions.cs
            //vocabView.DoOnFinishing(() =>
            //{
            //    this.dataSource.Marker = EEGDataEntry.MARKER_DEFAULT;
            //    lock (currentTrialEntries)
            //    {
            //        if (this.settings.ArtifactDetectionSettings.HasMotionArtifact(currentTrialEntries))
            //        {
            //            logWriter.WriteLine("Motion Artifact Detected");
            //            needToRerun = true;
            //        }
            //        else
            //        {
            //            if (this.settings.SaveTrialData)
            //            {
            //                foreach (var entry in currentTrialEntries)
            //                {
            //                    dataWriter.WriteLine(entry);
            //                }
            //            }

            //        }
            //        currentTrialEntries.Clear();
            //    }
            //});
            yield return vocabView;
            //The user controls which group the stimulus goes into after the trial
            StudyTestPair toAdd = new StudyTestPair(tst, ans);
            if (!isStudy)
            {
                yield return new TextView((bool)vocabView.Result.Value ? "Correct" : "Incorrect", settings.FeedbackTime, GUIUtils.Constants.DISPLAY_FONT_LARGE);
                string[] options = { "Study Soon", "Test Soon", "Test Later" };
                ChoiceView choice = new ChoiceView(options);
                yield return choice;
                if ((string)choice.Result.Value == options[0])
                    studySoon.Add(toAdd);
                else if ((string)choice.Result.Value == options[1])
                    testSoon.Add(toAdd);
                else
                    testLate.Add(toAdd);
                logWriter.WriteLine("User Answer: " + vocabView.Result.Value);
                logWriter.WriteLine("User Choice: " + choice.Result.Value);
            }
            else
            {
                testSoon.Add(toAdd);
                logWriter.WriteLine("Study Trial");
            }
//            if (needToRerun)
//            {
//                foreach (var view in RunTrial(index, isStudy, tst, ans, //dataWriter, 
//logWriter, //currentTrialEntries, 
//studySoon, testSoon, testLate))
//                {
//                    yield return view;
//                }
//            }
        }

    }
}
