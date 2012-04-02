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
        private readonly IArrayView<string> study;
        private readonly IArrayView<string> comp, class1, class2;
        RandomizedQueue<string>[] blocks;
        private readonly FalseAdaptSettings settings;
        public FalseAdaptProvider(RandomizedQueue<StudyTestTuple> presentation, IArrayView<string> comp, IArrayView<string> class1,
            IArrayView<string> class2, IArrayView<string> study,
            FalseAdaptSettings settings)
        {
            this.presentation = presentation;
            this.comp = comp;
            this.class1 = class1;
            this.class2 = class2;
            this.settings = settings;
            this.study = study;
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
            get { return "New F-Adaptive Experiment"; }
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
                    yield return new TextView(stimulus, 200, GUIUtils.Constants.DISPLAY_FONT_LARGE);
                    yield return new TextView(stimulus + "*", 100, GUIUtils.Constants.DISPLAY_FONT_LARGE);
                }
                yield return new ChoiceView(new string[] 
                {   
                    "Ready for next block"
                    }, out result);
            }
                
    yield return new ChoiceView(new string[] 
            { 
                "Start Study Phase"
            }, out result);

        foreach (var stim in study)
        {
        yield return new TextView(stim, this.settings.PresentationTime, GUIUtils.Constants.DISPLAY_FONT_LARGE);
        yield return new RestView(this.settings.RestTime);
        }
    

            yield return new ChoiceView(new string[] 
            { 
                "Start Practice Phase"
            }, out result);
            //Present all the stimuli for practice
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
                }
            }
        }

        
    }
}
