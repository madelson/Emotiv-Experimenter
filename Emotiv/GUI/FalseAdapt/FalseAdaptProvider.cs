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
using MCAEmotiv.GUI.UserControlVocab;

namespace MCAEmotiv.GUI.FalseAdapt
{
    class FalseAdaptProvider : AbstractEnumerable<View>, IViewProvider
    {
        private readonly RandomizedQueue<StudyTestTuple> presentation;
        private readonly FalseAdaptSettings settings;
        public FalseAdaptProvider(RandomizedQueue<StudyTestTuple> presentation,
            FalseAdaptSettings settings)
        {
            this.presentation = presentation;
            this.settings = settings;
        }
        public string Title
        {
            get { return "New F-Adaptive Experiment"; }
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

            //Present all the stimuli for study
            foreach (var stimulus in presentation)
            {
                yield return new RestView(this.settings.RestTime);
                yield return new FixationView(this.settings.FixationTime);
                if (stimulus.isStudy)
                    yield return new TextView(stimulus.test, this.settings.PresentationTime,
                        GUIUtils.Constants.DISPLAY_FONT_LARGE);
                else
                {
                    VocabView vocabView = new VocabView(stimulus.test, stimulus.answer, this.settings.PresentationTime, this.settings.DelayTime,
                        false, out result);
                    yield return vocabView;
                    yield return new TextView((bool)vocabView.Result.Value ? "Correct" : "Incorrect",
                        this.settings.FeedbackTime, GUIUtils.Constants.DISPLAY_FONT_LARGE);
                }
            }
        }

        //    //Generates the views by calling RunTrial
        //    private IEnumerable<View> GetViews(ISynchronizeInvoke invoker)
        //    {
        //        //Get a block of stimuli
        //        var blocks = this.GetBlocks(this.class1, new Random())
        //            .Zip(this.GetBlocks(this.class2, new Random()), (b1, b2) => new[] { new { stimuli = b1, cls = 1 }, new { stimuli = b2, cls = 2 } })
        //            .SelectMany(x => x);
        //        int blockCount = 1;
        //        var currentTrialEntries = new List<EEGDataEntry>();
        //        //To do: Save the date/time earlier and use it for both this and the dataWriter. Put it in GetEnumerator and pass to GetViews
        //        using (var logWriter = new StreamWriter(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "compexp_log_" + DateTime.Now.ToString("MM dd yyyy H mm ss") + ".txt")))
        //        using (var dataWriter = new StreamWriter(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "compexp_" + DateTime.Now.ToString("MM dd yyyy H mm ss") + ".csv")))
        //        using (var artifactListener = new EEGDataListener(invoker, null, data =>
        //        {
        //            foreach (var entry in data)
        //            {
        //                if (entry.HasStimulusMarker())
        //                {
        //                    lock (currentTrialEntries)
        //                    {

        //                        currentTrialEntries.Add(entry);
        //                    }
        //                }
        //            }

        //        }, null))
        //        {
        //            this.dataSource.AddListener(artifactListener);
        //            //Display each block of stimuli
        //            foreach (var block in blocks)
        //            {

        //                if (blockCount > this.settings.NumBlocks * 2)
        //                    break;
        //                logWriter.WriteLine("Current Class: {0}, Block Number: {1}", block.cls, blockCount);
        //                //yield return new TextView("Current Class: " + block.cls, 2500, GUIUtils.Constants.DISPLAY_FONT_LARGE);
        //                IViewResult result;
        //                // offer to save
        //                yield return new ChoiceView(new string[] 
        //            {   
        //                "Ready for next block"
        //                }, out result);
        //                foreach (var stimulus in block.stimuli)
        //                {
        //                    foreach (var view in RunTrial(stimulus, block.cls, dataWriter, logWriter, currentTrialEntries))
        //                        yield return view;
        //                }
        //                blockCount++;
        //            }
        //            logWriter.WriteLine("Experiment Concluded.");
        //        }
        //    }


        //    public IEnumerable<View> RunTrial(string stimulus, int cls, StreamWriter dataWriter, StreamWriter logWriter, List<EEGDataEntry> currentTrialEntries)
        //    {
        //        //Rest
        //        yield return new RestView(this.settings.BlinkTime);
        //        //Fixate
        //        yield return new FixationView(this.settings.FixationTime);
        //        //Generate stimulus view
        //        var stimulusView = new TextView(stimulus, this.settings.DisplayTime, GUIUtils.Constants.DISPLAY_FONT_LARGE);
        //        stimulusView.DoOnDeploy(c => this.dataSource.Marker = cls);
        //        bool needToRerun = false;
        //        //If there was a motion artifact, we need to rerun the trial with a different stimulus from the same class
        //        stimulusView.DoOnFinishing(() =>
        //        {
        //            this.dataSource.Marker = EEGDataEntry.MARKER_DEFAULT;
        //            lock (currentTrialEntries)
        //            {
        //                if (this.settings.ArtifactDetectionSettings.HasMotionArtifact(currentTrialEntries))
        //                {
        //                    logWriter.WriteLine("Motion Artifact Detected");
        //                    needToRerun = true;
        //                }
        //                else
        //                {
        //                    if (this.settings.SaveTrialData)
        //                    {
        //                        foreach (var entry in currentTrialEntries)
        //                        {
        //                            dataWriter.WriteLine(entry);
        //                        }
        //                    }

        //                }
        //                currentTrialEntries.Clear();
        //            }
        //        });
        //        logWriter.WriteLine(stimulus);
        //        yield return stimulusView;
        //        //Rerun if needed
        //        if (needToRerun)
        //        {
        //            var stimulusClass = (cls == 1) ? this.class1 : this.class2;
        //            var stim = stimulusClass.Shuffled().First(s => s != stimulus);
        //            foreach (var view in RunTrial(stim, cls, dataWriter, logWriter, currentTrialEntries))
        //            {
        //                yield return view;
        //            }
        //        }
        //    }

        //    //Continuously generates blocks of stimuli from each class randomly, alternating class
        //    public IEnumerable<List<string>> GetBlocks(IArrayView<string> stimulusClass, Random random)
        //    {
        //        var stimuli = new RandomizedQueue<string>(random);
        //        var usedStimuli = new List<string>();

        //        stimuli.AddRange(stimulusClass);

        //        while (true)
        //        {
        //            var block = new List<string>();
        //            for (int i = 0; i < this.settings.BlockSize; i++)
        //            {
        //                string stimulus;
        //                if (stimuli.Count == 0)
        //                {
        //                    stimuli.AddRange(usedStimuli);
        //                    usedStimuli.Clear();
        //                }
        //                stimulus = stimuli.RemoveRandom();
        //                usedStimuli.Add(stimulus);
        //                block.Add(stimulus);
        //            }

        //            yield return block;
        //        }
        //    }

        //}
    }
}
