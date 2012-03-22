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

namespace MCAEmotiv.GUI.Adaptive
{
    class AdaptiveProvider : AbstractEnumerable<View>, IViewProvider
    {
        private readonly RandomizedQueue<StudyTestPair> pres;
        private readonly AdaptiveSettings settings;
        private readonly IEEGDataSource dataSource;
        MLApp.MLApp matlab;
        public AdaptiveProvider(RandomizedQueue<StudyTestPair> stp,
            AdaptiveSettings settings, IEEGDataSource dataSource)
        {
            this.pres = stp;
            this.settings = settings;
            this.dataSource = dataSource;
            matlab = new MLApp.MLApp();
        }
        public string Title
        {
            get { return "New Adaptive Experiment"; }
        }

        /// <summary>
        /// The enumerator implementation
        /// </summary>
        public override IEnumerator<View> GetEnumerator()
        {
            IViewResult result;
            Random numgen = new Random();
            RandomizedQueue<StudyTestPair> study = new RandomizedQueue<StudyTestPair>();
            RandomizedQueue<StudyTestPair> quiz = new RandomizedQueue<StudyTestPair>();
            RandomizedQueue<StudyTestPair> done = new RandomizedQueue<StudyTestPair>();
            using (var logWriter = new StreamWriter(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "adapt_log_" + settings.SubjectName + "_" + DateTime.Now.ToString("MM dd yyyy H mm ss") + ".txt")))
            using (var dataWriter = new StreamWriter(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "adapt_data_" + settings.SubjectName + "_" + DateTime.Now.ToString("MM dd yyyy H mm ss") + ".csv")))
            {

                yield return new ChoiceView(new string[] 
                { 
                    "Start Study Phase"
                }, out result) { Text = "Click When Ready" };

                while (pres.Count > 0)
                {
                    var stimulus = pres.RemoveRandom();
                    yield return new TextView(stimulus.test + "\n" + stimulus.answer, this.settings.PresentationTime, GUIUtils.Constants.DISPLAY_FONT_LARGE);
                    yield return new RestView(this.settings.RestTime);
                    quiz.Add(stimulus);
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
                    foreach (var view in this.GetViews(invoker, logWriter, dataWriter, study, quiz, done, numgen))
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

        private IEnumerable<View> GetViews(ISynchronizeInvoke invoker, StreamWriter logWriter, StreamWriter dataWriter, RandomizedQueue<StudyTestPair> study,
            RandomizedQueue<StudyTestPair> quiz, RandomizedQueue<StudyTestPair> done, Random numgen)
        {
            var currentTrialEntries = new List<EEGDataEntry>();

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
                for (int index = 0; index < settings.NumRounds; index++)
                {
                    double rand = numgen.NextDouble();
                    StudyTestPair stim;
                    if (rand < .39)
                    {
                        if (!study.IsEmpty())
                        {
                            stim = study.RemoveRandom();
                            quiz.Add(stim);
                            logWriter.WriteLine("5");
                            logWriter.WriteLine(stim.test + "\\n" + stim.answer);
                            yield return new RestView(this.settings.BlinkTime);
                            yield return new TextView(stim.test + "\n" + stim.answer, this.settings.PresentationTime, GUIUtils.Constants.DISPLAY_FONT_LARGE);
                        }
                        else if (!quiz.IsEmpty())
                        {
                            stim = quiz.RemoveRandom();
                            logWriter.WriteLine("7");
                            logWriter.WriteLine(stim.test);
                            logWriter.WriteLine(stim.answer);
                            foreach (var view in RunTrial(index, stim, dataWriter, logWriter, currentTrialEntries, study, quiz, done))
                                yield return view;
                        }
                        else
                        {
                            stim = done.RemoveRandom();
                            logWriter.WriteLine("7");
                            logWriter.WriteLine(stim.test);
                            logWriter.WriteLine(stim.answer);
                            foreach (var view in RunTrial(index, stim, dataWriter, logWriter, currentTrialEntries, study, quiz, done))
                                yield return view;
                        }
                    }
                    else if (rand < .99)
                    {
                        if (!quiz.IsEmpty())
                        {
                            stim = quiz.RemoveRandom();
                            logWriter.WriteLine("7");
                            logWriter.WriteLine(stim.test);
                            logWriter.WriteLine(stim.answer);
                            foreach (var view in RunTrial(index, stim, dataWriter, logWriter, currentTrialEntries, study, quiz, done))
                                yield return view;
                        }
                        else if (!study.IsEmpty())
                        {
                            stim = study.RemoveRandom();
                            quiz.Add(stim);
                            logWriter.WriteLine("5");
                            logWriter.WriteLine(stim.test + "\\n" + stim.answer);
                            yield return new RestView(this.settings.BlinkTime);
                            yield return new TextView(stim.test + "\n" + stim.answer, this.settings.PresentationTime, GUIUtils.Constants.DISPLAY_FONT_LARGE);
                        }
                        else
                        {
                            stim = done.RemoveRandom();
                            logWriter.WriteLine("7");
                            logWriter.WriteLine(stim.test);
                            logWriter.WriteLine(stim.answer);
                            foreach (var view in RunTrial(index, stim, dataWriter, logWriter, currentTrialEntries, study, quiz, done))
                                yield return view;
                        }
                    }
                    else
                    {
                        if (!done.IsEmpty())
                        {
                            stim = done.RemoveRandom();
                            logWriter.WriteLine("7");
                            logWriter.WriteLine(stim.test);
                            logWriter.WriteLine(stim.answer);
                            foreach (var view in RunTrial(index, stim, dataWriter, logWriter, currentTrialEntries, study, quiz, done))
                                yield return view;
                        }
                        else if (!quiz.IsEmpty())
                        {
                            stim = quiz.RemoveRandom();
                            logWriter.WriteLine("7");
                            logWriter.WriteLine(stim.test);
                            logWriter.WriteLine(stim.answer);
                            foreach (var view in RunTrial(index, stim, dataWriter, logWriter, currentTrialEntries, study, quiz, done))
                                yield return view;
                        }
                        else
                        {
                            stim = study.RemoveRandom();
                            quiz.Add(stim);
                            logWriter.WriteLine("5");
                            logWriter.WriteLine(stim.test + "\\n" + stim.answer);
                            yield return new RestView(this.settings.BlinkTime);
                            yield return new TextView(stim.test + "\n" + stim.answer, this.settings.PresentationTime, GUIUtils.Constants.DISPLAY_FONT_LARGE);
                        }
                    }
                }


            }
        }


        public IEnumerable<View> RunTrial(int index, StudyTestPair stim, StreamWriter dataWriter, StreamWriter logWriter, List<EEGDataEntry> currentTrialEntries,
            RandomizedQueue<StudyTestPair> study, RandomizedQueue<StudyTestPair> quiz, RandomizedQueue<StudyTestPair> done)
        {
            yield return new RestView(this.settings.BlinkTime);
            yield return new FixationView(this.settings.FixationTime);
            IViewResult result;
            var vocabView = new VocabView(stim.test, stim.answer, settings.DisplayTime, settings.DelayTime, false, out result);
            vocabView.DoOnDeploy(c => this.dataSource.Marker = index + 1);
            bool noWrite = false;
            double[] judge = {1};
            double[] zro = {0};
            double newcomplevel = -1;
            vocabView.DoOnFinishing(() =>
            {
                this.dataSource.Marker = EEGDataEntry.MARKER_DEFAULT;
                lock (currentTrialEntries)
                {
                    var trialsDuringDelay = currentTrialEntries.Where(e => e.RelativeTimeStamp <= settings.DelayTime);
                    if (this.settings.ArtifactDetectionSettings.HasMotionArtifact(trialsDuringDelay))
                    {
                        noWrite = true;
                    }
                    else
                    {
                        int numentries = 0;
                        foreach (var entry in trialsDuringDelay)
                        {
                            if (this.settings.SaveTrialData)
                            {
                                dataWriter.WriteLine(entry + ", {0}", stim.index);
                            }
                            numentries++;
                        }

                        //To do: figure out a better way to do this
                        double[,] data2matlab = new double[numentries,15];
                        double[,] zeros = new double[numentries,15];
                        int i = 0;
                        foreach (var entry in trialsDuringDelay)
                        {
                            //data2matlab[i, 0] = entry.Marker;
                            //data2matlab[i, 1] = entry.TimeStamp;
                            data2matlab[i, 0] = entry.RelativeTimeStamp;
                            zeros[i, 0] = 0;
                            //zeros[i, 1] = 0;
                            //zeros[i, 2] = 0;
                            int j = 1;
                            foreach (var set in entry.Data)
                            {
                                data2matlab[i,j] = set;
                                zeros[i,j] = 0.0;
                                j++;
                            }
                            //data2matlab[i, 17] = stim.index;
                            //zeros[i, 17] = 0;
                            i++;
                        }

                        matlab.PutFullMatrix("data", "base", data2matlab, zeros);
                        double[] complev = { stim.complevel };
                        double[] zero = { 0 };
                        matlab.PutFullMatrix("rating", "base", complev, zero);
                        matlab.Execute("cd c:\\Users\\Nicole\\Documents\\Matlab\\Thesis\\Adapt");
                        //matlab.Execute("csvwrite('testing.csv', data)");
                        matlab.Execute("[result rating] = adaptive(data, rating);");
                        //matlab.Execute("csvwrite('othertest.csv', result)");
                        matlab.GetFullMatrix("result", "base", judge, zro);
                        newcomplevel = matlab.GetVariable("rating", "base");

                    }
                    currentTrialEntries.Clear();
                }
            });
            yield return vocabView;
            if (!noWrite)
            {
                stim.complevel = newcomplevel;
                if ((bool)result.Value)
                {
                    if (judge[0] == 1)
                    {
                        quiz.Add(stim);
                    }
                    else
                    {
                        done.Add(stim);
                    }
                }
                else
                {
                    study.Add(stim);
                }
            }
        }

    }
}
