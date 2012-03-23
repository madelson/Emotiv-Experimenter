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

namespace MCAEmotiv.GUI.CompetitionExperiment
{
    class CompetitionExperimentProvider : AbstractEnumerable<View>, IViewProvider
    {
        private readonly IArrayView<string> presentation, class1, class2;
        RandomizedQueue<string>[] blocks;
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
            blocks = new RandomizedQueue<string>[settings.NumBlocks*2];
            int limit = 0;
            for (int i = 0; i < settings.NumBlocks*2; i += 2)
            {
                blocks[i] = new RandomizedQueue<string>();
                blocks[i + 1] = new RandomizedQueue<string>();

                for (int j = 0 + limit * settings.BlockSize; j < (limit + 1) * settings.BlockSize; j++)
                {
                    blocks[i].Add(this.class1[j]);
                    blocks[i + 1].Add(this.class2[j]);
                }
                limit++;

            }
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
            using (var logWriter = new StreamWriter(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "compexp_log_" + settings.SubjectName + DateTime.Now.ToString("MM dd yyyy H mm ss") + ".txt")))
            using (var dataWriter = new StreamWriter(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "compexp_" + settings.SubjectName + DateTime.Now.ToString("MM dd yyyy H mm ss") + ".csv")))
            {
                for (int i = 0; i < 2; i++)
                {
                    yield return new ChoiceView(new string[] 
            { 
                "Ready for Study Phase"
            }, out result);

                    //Present half the stimuli for study
                    for (int j = 0 + i * (presentation.Count/2); j < (presentation.Count / 2) * (i + 1); j++)
                    {
                        yield return new TextView(presentation[j], this.settings.PresentationTime, GUIUtils.Constants.DISPLAY_FONT_LARGE);
                        yield return new RestView(this.settings.RestTime);
                    }

                    //Begin the practice phase
                    yield return new ChoiceView(new string[] 
            { 
                "Start EEG Recording"
            }, out result) { Text = "Click When Ready" };

                    var connected = true; // assume it's connected
                    using (var invoker = new SingleThreadedInvoker())
                    using (var connectionListener = new EEGDataListener(invoker, s => connected = true, null, s => connected = false))
                    {
                        // listen for a broken connection
                        this.dataSource.AddListener(connectionListener);
                        foreach (var view in this.GetViews(invoker, logWriter, dataWriter, i))
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

        //Generates the views by calling RunTrial
        private IEnumerable<View> GetViews(ISynchronizeInvoke invoker, StreamWriter logWriter, StreamWriter dataWriter, int round)
        {
            //Get a block of stimuli
            //var blocks = this.GetBlocks(this.class1, new Random())
            //    .Zip(this.GetBlocks(this.class2, new Random()), (b1, b2) => new[] { new { stimuli = b1, cls = 1 }, new { stimuli = b2, cls = 2 } })
            //    .SelectMany(x => x);
            //int blockCount = 1;

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
                //Display each block of stimuli
                for (int j = 0 + round * (settings.NumBlocks); j < (settings.NumBlocks) * (round + 1); j++)
                {

                    logWriter.WriteLine("Current Class: {0}, Block Number: {1}", (j % 2 + 1), j);
                    //yield return new TextView("Current Class: " + block.cls, 2500, GUIUtils.Constants.DISPLAY_FONT_LARGE);
                    IViewResult result;
                    
                    yield return new ChoiceView(new string[] 
                {   
                    "Ready for next block"
                    }, out result);
                    int limit = blocks[j].Count;
                    for (int k = 0; k < limit; k++)
                    {
                        foreach (var view in RunTrial(blocks[j].RemoveRandom(), (j % 2 + 1), dataWriter, logWriter, currentTrialEntries))
                            yield return view;
                    }
                    //blockCount++;
                }
                logWriter.WriteLine("Phase {0} Concluded.", round + 1);
            }
        }


        public IEnumerable<View> RunTrial(string stimulus, int cls, StreamWriter dataWriter, StreamWriter logWriter, List<EEGDataEntry> currentTrialEntries)
        {
            //Rest
            yield return new RestView(this.settings.BlinkTime);
            //Fixate
            yield return new FixationView(this.settings.FixationTime);
            //Generate stimulus view
            var stimulusView = new TextView(stimulus, this.settings.DisplayTime, GUIUtils.Constants.DISPLAY_FONT_LARGE);
            stimulusView.DoOnDeploy(c => this.dataSource.Marker = cls);
            bool needToRerun = false;
            //HERE IT IS
            bool feedback = true;
            //If there was a motion artifact, we need to rerun the trial with a different stimulus from the same class
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
            yield return new TextView(stimulus + "*", settings.SpeakTime, GUIUtils.Constants.DISPLAY_FONT_LARGE);
            //Rerun if needed
            if (needToRerun && feedback)
                yield return new TextView("You moved!", 1000, GUIUtils.Constants.DISPLAY_FONT_LARGE);
            //{
            //    var stimulusClass = (cls == 1) ? this.class1 : this.class2;
            //    var stim = stimulusClass.Shuffled().First(s => s != stimulus);
            //    foreach (var view in RunTrial(stim, cls, dataWriter, logWriter, currentTrialEntries))
            //    {
            //        yield return view;
            //    }
            //}
        }

        //Continuously generates blocks of stimuli from each class randomly, alternating class
        //public IEnumerable<List<string>> GetBlocks(IArrayView<string> stimulusClass, Random random)
        //{
        //    var stimuli = new RandomizedQueue<string>(random);
        //    var usedStimuli = new List<string>();

        //    stimuli.AddRange(stimulusClass);

        //    while (true)
        //    {
        //        var block = new List<string>();
        //        for (int i = 0; i < this.settings.BlockSize; i++)
        //        {
        //            string stimulus;
        //            if (stimuli.Count == 0)
        //            {
        //                stimuli.AddRange(usedStimuli);
        //                usedStimuli.Clear();
        //            }
        //            stimulus = stimuli.RemoveRandom();
        //            usedStimuli.Add(stimulus);
        //            block.Add(stimulus);
        //        }

        //        yield return block;
        //    }
        //}

    }
}
