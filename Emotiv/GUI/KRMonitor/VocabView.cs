using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MCAEmotiv.GUI.Animation;
using System.Windows.Forms;

namespace MCAEmotiv.GUI.KRMonitor
{
    class VocabView : MCAEmotiv.GUI.Animation.View
    {
        public VocabView(string testStimulus, string correctAns, int displayTimeMillis, int delayTimeMillis, bool mchoice, out IViewResult result)
            : base()
        {
            TextView test = new TextView(testStimulus, displayTimeMillis, GUIUtils.Constants.DISPLAY_FONT_LARGE); //-1 is infinite time
            //mchoice is a bool that indicates whether the vocab view should be multiple choice or not. I haven't actually handled this properly
            if (mchoice)
            {
                string[] answers = new string[1];
                //Currently the only option is the correct answer
                answers[0] = correctAns;
                ChoiceView choice = new ChoiceView(answers);
                var timer = this.RegisterDisposable(new Timer() { Interval = delayTimeMillis, Enabled = false });
                var rows = GUIUtils.CreateTable(new[] { .5, .5 }, Direction.Vertical);
                var testPanel = new Panel { Dock = DockStyle.Fill };
                var choicePanel = new Panel { Dock = DockStyle.Fill, Enabled = false };
                rows.Controls.Add(testPanel, 0, 0);
                timer.Tick += (sender, args) =>
                {
                    choicePanel.Enabled = true;
                    rows.Controls.Add(choicePanel, 0, 1);
                    timer.Enabled = false;
                };
                this.DoOnDeploy(c =>
                {
                    c.Controls.Add(rows);
                    this.DeploySubView(test, testPanel, causesOwnerToFinish: false);
                    this.DeploySubView(choice, choicePanel, causesOwnerToFinish: true);
                    timer.Enabled = true;
                });
                this.DoOnFinishing(() =>
                    {
                        var answer = choice.Result.HasValue ? choice.Result.Value : null;
                        this.SetResult(((string)answer) == correctAns);
                    });
                result = this.Result;
            }
                //ISSUE: In the free response version, no matter what I try, I can't get the cursor to automatically be in the text box
            else
            {
                FreeResponseView frView = new FreeResponseView();
                
                var timer = this.RegisterDisposable(new Timer() { Interval = delayTimeMillis, Enabled = false });
                var rows = GUIUtils.CreateTable(new[] { .5, .5 }, Direction.Vertical);
                var testPanel = new Panel { Dock = DockStyle.Fill };
                var frPanel = new Panel { Dock = DockStyle.Fill, Enabled = false };
                rows.Controls.Add(testPanel, 0, 0);
                timer.Tick += (sender, args) =>
                {
                    frPanel.Enabled = true;
                    
                    rows.Controls.Add(frPanel, 0, 1);
                    timer.Enabled = false;
                };
                this.DoOnDeploy(c =>
                {
                    
                    c.Controls.Add(rows);
                    this.DeploySubView(test, testPanel, causesOwnerToFinish: false);
                    this.DeploySubView(frView, frPanel, causesOwnerToFinish: true);
                    
                    timer.Enabled = true;
                });
                this.DoOnFinishing(() =>
                {
                    if (frView.Result.HasValue)
                        this.SetResult(((string) frView.Result.Value) == correctAns);
                    else
                        this.SetResult(false);
                });
                result = this.Result;
            }
        }
    }
}
