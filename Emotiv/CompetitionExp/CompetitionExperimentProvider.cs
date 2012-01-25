using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MCAEmotiv.GUI.Animation;

namespace CompetitionExp
{
    class CompetitionExperimentProvider : AbstractEnumerable<View>, IViewProvider
    {
        public string Title
        {
            get { return "New Competition Experiment"; }
        }

        /// <summary>
        /// The enumerator implementation
        /// </summary>
        public override IEnumerator<View> GetEnumerator(){
            IViewResult result;
            // offer to save
            yield return new ChoiceView(new string[] 
            { 
                "Save",
                "Don't Save"
            }, out result);

        }

    }
}
