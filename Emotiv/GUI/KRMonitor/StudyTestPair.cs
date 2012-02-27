using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MCAEmotiv.GUI.KRMonitor
{
    class StudyTestPair
    {
        public string test;
        public string answer;
        public int index;
        public StudyTestPair(string tst, string ans, int i)
        {
            this.test = tst;
            this.answer = ans;
            this.index = i;
        }
    }
}
