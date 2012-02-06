using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MCAEmotiv.GUI.FalseAdapt
{
    class StudyTestTuple
    {
        public string test;
        public string answer;
        public bool isStudy;
        public StudyTestTuple(string tst, string ans, bool isstdy)
        {
            this.test = tst;
            this.answer = ans;
            this.isStudy = isstdy;
        }
    }
}
