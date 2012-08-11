using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MCAEmotiv.GUI.Adaptive
{
    //A class for binding questions and answers
    class StudyTestPair
    {
        public string test;
        public string answer;
        public int index;
        public double complevel;
        public int times;
        public StudyTestPair(string tst, string ans, int index)
        {
            this.test = tst;
            this.answer = ans;
            this.index = index;
            this.times = 0;
            this.complevel = -1;
        }
    }
}
