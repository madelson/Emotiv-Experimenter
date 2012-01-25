﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MCAEmotiv.GUI.Animation;
using System.Threading;
using MCAEmotiv.Interop;
using MCAEmotiv.Common;
using System.IO;
using System.ComponentModel;

namespace MCAEmotiv.GUI.CompetitionExperiment
{
    class CompetitionExperimentProvider : AbstractEnumerable<View>, IViewProvider
    {
        private readonly IArrayView<string> presentation, class1, class2;
        private readonly CompetitionExperimentSettings settings;
        private readonly IEEGDataSource dataSource;
        public CompetitionExperimentProvider(IArrayView<string> presentation, IArrayView<string> class1, IArrayView<string> class2,
            CompetitionExperimentSettings settings, IEEGDataSource dataSource)
        {
            this.presentation = presentation;
            this.class1 = class1;
            this.class2 = class2;
            this.settings = settings;
            this.dataSource = dataSource;
        }
        public string Title
        {
            get { return "New Competition Experiment"; }
        }

        /// <summary>
        /// The enumerator implementation
        /// </summary>
        public override IEnumerator<View> GetEnumerator()
        {
            IViewResult result;
            // offer to save
            yield return new ChoiceView(new string[] 
            { 
                "Ready"
            }, out result);

            //while (true)
            //{
            //    yield return new VocabView("mashua", "boat", new[] { "boat", "train", "car", "plane" }, 400, out result);

            //    yield return new TextView(result.Value.ToString(), 2000);
            //}

            foreach (var stimulus in presentation)
            {
                yield return new TextView(stimulus, this.settings.PresentationTime, GUIUtils.Constants.DISPLAY_FONT_LARGE);
                yield return new RestView(this.settings.RestTime);
            }


            yield return new ChoiceView(new string[] 
            { 
                "Start EEG Recording"
            }, out result) { Text = "Click When Ready" };

            var connected = true; // assume it's connected
            using (var invoker = new SingleThreadedInvoker())
            using (var connectionListener = new EEGDataListener(invoker, s => connected = true, null, s => connected = false))
            //using (var logger = this.LogExperimentAndGetLogger()) // log the experiment
            //using (var trialLogger = this.GetTrialWriter()) // logs each trial
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

        private IEnumerable<View> GetViews(ISynchronizeInvoke invoker)
        {
            var blocks = this.GetBlocks(this.class1, new Random())
                .Zip(this.GetBlocks(this.class2, new Random()), (b1, b2) => new[] { new { stimuli = b1, cls = 1 }, new { stimuli = b2, cls = 2 } })
                .SelectMany(x => x);
            int blockCount = 1;
            var currentTrialEntries = new List<EEGDataEntry>();
            //To do: Save the date/time earlier and use it for both this and the dataWriter. Put it in GetEnumerator and pass to GetViews
            using (var logWriter = new StreamWriter(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "compexp_log_" + DateTime.Now.ToString("MM dd yyyy H mm ss") + ".txt")))
            using (var dataWriter = new StreamWriter(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "compexp_" + DateTime.Now.ToString("MM dd yyyy H mm ss") + ".csv")))
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
                foreach (var block in blocks)
                {

                    if (blockCount > this.settings.NumBlocks * 2)
                        break;
                    logWriter.WriteLine("Current Class: {0}, Block Number: {1}", block.cls, blockCount);
                    foreach (var stimulus in block.stimuli)
                    {
                        foreach (var view in RunTrial(stimulus, block.cls, dataWriter, logWriter, currentTrialEntries))
                            yield return view;
                    }
                    yield return new TextView("Please Clear Your Mind", 2500, GUIUtils.Constants.DISPLAY_FONT_LARGE);
                    blockCount++;
                }
                logWriter.WriteLine("Experiment Concluded.");
            }
        }


        public IEnumerable<View> RunTrial(string stimulus, int cls, StreamWriter dataWriter, StreamWriter logWriter, List<EEGDataEntry> currentTrialEntries)
        {
            yield return new TextView("Blink Now", this.settings.BlinkTime, GUIUtils.Constants.DISPLAY_FONT_LARGE);
            yield return new FixationView(this.settings.FixationTime);
            var stimulusView = new TextView(stimulus, this.settings.DisplayTime, GUIUtils.Constants.DISPLAY_FONT_LARGE);
            stimulusView.DoOnDeploy(c => this.dataSource.Marker = cls);
            bool needToRerun = false;
            stimulusView.DoOnFinishing(() =>
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
            logWriter.WriteLine(stimulus);
            yield return stimulusView;
            if (needToRerun)
            {
                var stimulusClass = (cls == 1) ? this.class1 : this.class2;
                var stim = stimulusClass.Shuffled().First(s => s != stimulus);
                foreach (var view in RunTrial(stim, cls, dataWriter, logWriter, currentTrialEntries))
                {
                    yield return view;
                }
            }
        }

        public IEnumerable<List<string>> GetBlocks(IArrayView<string> stimulusClass, Random random)
        {
            var stimuli = new RandomizedQueue<string>(random);
            var usedStimuli = new List<string>();

            stimuli.AddRange(stimulusClass);

            while (true)
            {
                var block = new List<string>();
                for (int i = 0; i < this.settings.BlockSize; i++)
                {
                    string stimulus;
                    if (stimuli.Count == 0)
                    {
                        stimuli.AddRange(usedStimuli);
                        usedStimuli.Clear();
                    }
                    stimulus = stimuli.RemoveRandom();
                    usedStimuli.Add(stimulus);
                    block.Add(stimulus);
                }

                yield return block;
            }
        }

    }
}
