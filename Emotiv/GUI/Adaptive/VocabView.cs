using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MCAEmotiv.GUI.Animation;
using System.Windows.Forms;

namespace MCAEmotiv.GUI.Adaptive
{
    class VocabView : MCAEmotiv.GUI.Animation.View
    {
        /// <summary>
        /// Compute the distance between two strings.
        /// </summary>
        private int Compute(string s, string t)
        {
            int n = s.Length;
            int m = t.Length;
            int[,] d = new int[n + 1, m + 1];

            // Step 1
            if (n == 0)
            {
                return m;
            }

            if (m == 0)
            {
                return n;
            }

            // Step 2
            for (int i = 0; i <= n; d[i, 0] = i++)
            {
            }

            for (int j = 0; j <= m; d[0, j] = j++)
            {
            }

            // Step 3
            for (int i = 1; i <= n; i++)
            {
                //Step 4
                for (int j = 1; j <= m; j++)
                {
                    // Step 5
                    int cost = (t[j - 1] == s[i - 1]) ? 0 : 1;

                    // Step 6
                    d[i, j] = Math.Min(
                        Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                        d[i - 1, j - 1] + cost);
                }
            }
            // Step 7
            return d[n, m];
        }
        public VocabView(string testStimulus, string correctAns, int displayTimeMillis, int delayTimeMillis, bool mchoice, out IViewResult result)
            : base()
        {
            TextView test = new TextView(testStimulus, displayTimeMillis, GUIUtils.Constants.DISPLAY_FONT_LARGE); //-1 is infinite time
            if (mchoice)
            {
                string[] answers = new string[1];
                //Currently the only option is the correct answer
                //To do: Randomly select a subset of answers OR do free response
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
                    if (frView.Result.HasValue && (string) frView.Result.Value != "")
                        if (Compute((string)frView.Result.Value, correctAns) < 3)
                            if ((correctAns == "MONKEY" && (string) frView.Result.Value == "DONKEY") || (correctAns == "DONKEY" && (string) frView.Result.Value == "MONKEY"))
                                this.SetResult(false);
                            else if (correctAns == "MANGO" || correctAns == "MAGGOT" || correctAns == "HORSE" || correctAns == "CORPSE")
                                this.SetResult(Compute((string)frView.Result.Value, correctAns) < 2);
                            else
                                this.SetResult(true);
                        else
                            this.SetResult(false);
                    else
                        this.SetResult(false);
                });
                result = this.Result;
            }
        }
    }
}
