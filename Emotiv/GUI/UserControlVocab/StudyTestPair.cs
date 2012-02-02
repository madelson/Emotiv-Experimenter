using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MCAEmotiv.GUI.UserControlVocab
{
    //A class for binding questions and answers
    class StudyTestPair
    {
        public string test;
        public string answer;
        public StudyTestPair(string tst, string ans)
        {
            this.test = tst;
            this.answer = ans;
        }
    }
}
