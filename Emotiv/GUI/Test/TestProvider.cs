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
using MCAEmotiv.GUI.KRMonitor;

namespace MCAEmotiv.GUI.Test
{
    class TestProvider : AbstractEnumerable<View>, IViewProvider
    {
        private readonly IArrayView<string> presentation;
        private readonly RandomizedQueue<StudyTestPair> stp;
        private readonly TestSettings settings;
        public TestProvider(IArrayView<string> presentation, RandomizedQueue<StudyTestPair> stp,
            TestSettings settings)
        {
            this.presentation = presentation;
            this.stp = stp;
            this.settings = settings;
        }
        public string Title
        {
            get { return "New Test"; }
        }

        /// <summary>
        /// The enumerator implementation
        /// </summary>
        public override IEnumerator<View> GetEnumerator()
        {
            IViewResult result;
            RandomizedQueue<string> pres = new RandomizedQueue<string>();
            pres.AddRange(presentation);
            using (var logWriter = new StreamWriter(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "test_log_" + settings.SubjectName + "_" + DateTime.Now.ToString("MM dd yyyy H mm ss") + ".txt")))
            using (var anslog = new StreamWriter(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "test_anslog_" + settings.SubjectName + "_" + DateTime.Now.ToString("MM dd yyyy H mm ss") + ".txt")))
            {
                //Click to Start
                  yield return new ChoiceView(new string[] 
                { 
                    "Start Test"
                }, out result) { Text = "Click When Ready" };

                       foreach (var view in this.GetViews(logWriter, anslog))
                       {
                                yield return view;
                       }    
                
            }
        }

        private IEnumerable<View> GetViews(StreamWriter logWriter, StreamWriter anslog)
        {
            
                RandomizedQueue<StudyTestPair> usedPairs = new RandomizedQueue<StudyTestPair>();

                var stimulusPairs = new RandomizedQueue<StudyTestPair>();
                stimulusPairs.AddRange(this.stp);
                
                while (stimulusPairs.Count > 0)
                {
                    StudyTestPair currstp = stimulusPairs.RemoveRandom();
                    usedPairs.Add(currstp);
                    foreach (var view in RunTrial(currstp.test, currstp.answer, currstp.index, logWriter, anslog))
                    {
                        yield return view;
                    }

                }
                stimulusPairs.AddRange(usedPairs);
            }
        


        public IEnumerable<View> RunTrial(string tst, string ans, int numstim, StreamWriter logWriter, StreamWriter anslog)
        {
            IViewResult result;
            var vocabView = new VocabView(tst, ans, settings.DisplayTime, settings.DelayTime, false, anslog, out result);
            
             yield return vocabView;
            //No feedback in original Karpicke and Roediger, but I'm leaving the option 
            //yield return new TextView((bool)result.Value ? "Correct" : "Incorrect", settings.FeedbackTime, GUIUtils.Constants.DISPLAY_FONT);
            int towrite = 0;
            if ((bool)result.Value)
            {
                
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
