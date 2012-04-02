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
        private readonly IArrayView<string> comp, class1, class2;
        RandomizedQueue<string>[] blocks;
        public UserCtrlProvider(IArrayView<string> presentation, IArrayView<string> comp, IArrayView<string> class1,
            IArrayView<string> class2, RandomizedQueue<StudyTestPair> stp,
            UserCtrlSettings settings)//, IEEGDataSource dataSource)
        {
            this.presentation = presentation;
            this.comp = comp;
            this.class1 = class1;
            this.class2 = class2;
            this.stp = stp;
            this.settings = settings;

            blocks = new RandomizedQueue<string>[settings.NumBlocks * 2];
            int limit = 0;
            for (int i = 0; i < settings.NumBlocks * 2; i += 2)
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
            get { return "New User Control"; }
        }

        /// <summary>
        /// The enumerator implementation
        /// </summary>
        public override IEnumerator<View> GetEnumerator()
        {
            IViewResult result;


            yield return new ChoiceView(new string[] 
                { 
                    "Start Training Phase"
                }, out result) { Text = "Click When Ready" };
            for (int j = 0; j < comp.Count; j++)
            {
                yield return new TextView(comp[j], 3000, GUIUtils.Constants.DISPLAY_FONT_LARGE);
                yield return new RestView(1500);
            }
            yield return new ChoiceView(new string[] 
                { 
                    "Begin Testing"
                }, out result) { Text = "Click When Ready" };
            //Display each block of stimuli
            for (int j = 0; j < (settings.NumBlocks*2); j++)
            {
                
                int limit = blocks[j].Count;
                for (int k = 0; k < limit; k++)
                {
                    //Rest
                    yield return new RestView(this.settings.BlinkTime);
                    //Fixate
                    yield return new FixationView(this.settings.FixationTime);
                    var stimulus = blocks[j].RemoveRandom();
                    //Generate stimulus view
                    yield return new TextView(stimulus, 2000, GUIUtils.Constants.DISPLAY_FONT_LARGE);
                    yield return new TextView(stimulus + "*", 1000, GUIUtils.Constants.DISPLAY_FONT_LARGE);
                }
                yield return new ChoiceView(new string[] 
                {   
                    "Ready for next block"
                    }, out result);
                }
                
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
            using (var logWriterV = new StreamWriter(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "userctrl_logv_" + settings.SubjectName + "_" + DateTime.Now.ToString("MM dd yyyy H mm ss") + ".txt")))
            using (var logWriter = new StreamWriter(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "userctrl_log_" + settings.SubjectName + "_" + DateTime.Now.ToString("MM dd yyyy H mm ss") + ".csv")))
            {

                yield return new ChoiceView(new string[] 
                { 
                    "Start Practice Phase"
                }, out result) { Text = "Click When Ready" };
                
                
                using (var invoker = new SingleThreadedInvoker())
                {
                    foreach (var view in this.GetViews(invoker, logWriterV, logWriter, 
                        studySoon, testSoon, testLate))
                
                            yield return view;
                
                }

            }
        }

        private IEnumerable<View> GetViews(ISynchronizeInvoke invoker, StreamWriter logWriterV, StreamWriter logWriter,
            RandomizedQueue<StudyTestPair> studySoon, RandomizedQueue<StudyTestPair> testSoon, RandomizedQueue<StudyTestPair> testLate)
        {
            
            //Presents stimuli from the three categories randomly with set probabilities
                Random numgen = new Random();

                for (int index = 0; index < settings.NumTrials; index++)
                {
                    double choice = numgen.NextDouble();
                    if ((choice < 0.05) && !testLate.IsEmpty())
                    {
                        StudyTestPair currstp = testLate.RemoveRandom();
                        logWriterV.WriteLine("Question: " + currstp.test);
                        logWriterV.WriteLine("Correct Answer: " + currstp.answer);
                        foreach (var view in RunTrial(index, false, currstp, logWriter, 
                            logWriterV,
                            studySoon, testSoon, testLate))
                        {
                            yield return view;
                        }
                    }
                    else if ((choice < 0.4) && !studySoon.IsEmpty())
                    {
                        StudyTestPair study = studySoon.RemoveRandom();
                        logWriterV.WriteLine("Study Trial");
                        logWriterV.WriteLine(study.test);
                        logWriterV.WriteLine(study.answer);
                        foreach (var view in RunTrial(index, true, study, logWriter, 
                            logWriterV, 
                            studySoon, testSoon, testLate))
                        {
                            yield return view;
                        }
                    }
                    else if (!testSoon.IsEmpty())
                    {
                        StudyTestPair currstp = testSoon.RemoveRandom();
                        logWriterV.WriteLine("Question: " + currstp.test);
                        logWriterV.WriteLine("Correct Answer: " + currstp.answer);
                        foreach (var view in RunTrial(index, false, currstp, logWriter, 
                            logWriterV,
                            studySoon, testSoon, testLate))
                        {
                            yield return view;
                        }
                    }
                    else
                        yield break;


                }
            }
        


        public IEnumerable<View> RunTrial(int index, bool isStudy, StudyTestPair stp, StreamWriter logWriter, 
            StreamWriter logWriterV, //List<EEGDataEntry> currentTrialEntries, 
    RandomizedQueue<StudyTestPair> studySoon, 
            RandomizedQueue<StudyTestPair> testSoon, RandomizedQueue<StudyTestPair> testLate)
        {
            yield return new RestView(this.settings.BlinkTime);
            yield return new FixationView(this.settings.FixationTime);
            IViewResult result;
            View vocabView;
            if (isStudy)
            {
                vocabView = new TextView(stp.test + Environment.NewLine + stp.answer, settings.DisplayTime, GUIUtils.Constants.DISPLAY_FONT_LARGE);
            }
            else
            {
                vocabView = new VocabView(stp.test, stp.answer, settings.DisplayTime, settings.DelayTime, false, out result);
            }
            
            yield return vocabView;
            //The user controls which group the stimulus goes into after the trial
            StudyTestPair toAdd = stp; //new StudyTestPair(stp.test, stp.answer, stp.number);
            if (!isStudy)
            {
                yield return new TextView((bool)vocabView.Result.Value ? "Correct" : "Incorrect", settings.FeedbackTime, GUIUtils.Constants.DISPLAY_FONT_LARGE);
                int toWrite = (bool)vocabView.Result.Value ? 1 : 0;
                logWriter.WriteLine(toAdd.number + ", " + toWrite);
                string[] options = { "Study Soon", "Test Soon", "Test Later" };
                ChoiceView choice = new ChoiceView(options);
                yield return choice;
                if ((string)choice.Result.Value == options[0])
                    studySoon.Add(toAdd);
                else if ((string)choice.Result.Value == options[1])
                    testSoon.Add(toAdd);
                else
                    testLate.Add(toAdd);
                logWriterV.WriteLine("User Answer: " + vocabView.Result.Value);
                logWriterV.WriteLine("User Choice: " + choice.Result.Value);
            }
            else
            {
                testSoon.Add(toAdd);
                logWriterV.WriteLine("Study Trial");
            }

        }

    }
}
