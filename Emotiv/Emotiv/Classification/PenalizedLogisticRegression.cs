using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MathNet.Numerics.LinearAlgebra;

namespace MCAEmotiv.Classification
{
    /// <summary>
    /// Classifies via logistic regression, penalizing large weight values to avoid over-fitting
    /// </summary>
    [Serializable]
    [Description("Classifies via logistic regression, penalizing large weight values to avoid over-fitting", DisplayName = "Penalized Logistic Regression")] 
    public class PenalizedLogisticRegression : AbstractBinaryClassifier
    {
        /// <summary>
        /// Returns true
        /// </summary>
        public override bool ComputesConfidence { get { return true; } }

        private Vector weights;

        /// <summary>
        /// The weights resulting from training
        /// </summary>
        public IEnumerable<double> Weights
        {
            get { return this.weights; }
        }

        /// <summary>
        /// A term used to penalize large weight values
        /// </summary>
        [Parameter("A term used to penalize large weight values", DefaultValue = 1e-4, MinValue = 0.0, MaxValue = 1.0)]
        public double Lambda { get; set; }

        /// <summary>
        /// Stop training when the distance between the new and previous weight vectors is less than this value
        /// </summary>
        [Parameter("Stop training when the distance between the new and previous weight vectors is less than this value",
            DefaultValue = 1e-6, MinValue = 10.0 * double.Epsilon)]
        public double MinDistance { get; set; }

        /// <summary>
        /// If this value is greater than 0, train for no more than that many iterations
        /// </summary>
        [Parameter("If this value is greater than 0, train for no more than that many iterations", DefaultValue = 100)]
        public int MaxIterations { get; set; }

        private static Vector ComputeWeights(Matrix X, Matrix d, double lambda, double minDistance, int maxIterations)
        {
            // k+1, n
            int k = X.RowCount, n = X.ColumnCount;

            // Precompute the penalty term
            var penalty = Matrix.Identity(k, k);
            penalty.Multiply(lambda);
            penalty[k - 1, k - 1] = 0;

            Matrix p, g, H, pMinusD = new Matrix(n, 1), w, wNew = new Matrix(k, 1), Xt;
            double[] diag = new double[n];
            int iters = 0;
            do
            {
                w = wNew;

                // p = f(wT * X)
                w.Transpose();
                p = w.Multiply(X);
                w.Transpose();
                for (int i = 0; i < n; i++)
                {
                    p[0, i] = CalcLogistic(p[0, i]);
                    pMinusD[i, 0] = p[0, i] - d[0, i];
                    diag[i] = p[0, i] * (1 - p[0, i]);
                }

                // g = X * (d - p) - penalty * w
                g = X.Multiply(pMinusD);
                g.Subtract(penalty.Multiply(w));

                // H = X*diag(p.*(1-p))*XT + penalty
                Xt = X.Clone();
                Xt.Transpose();
                Xt.Multiply(diag);
                H = X.Multiply(Xt);
                H.Add(penalty);

                // w = w + H^-1 * g
                try { wNew = H.Inverse().Multiply(g); }
                catch (Exception) { break; }
                //wNew = H.Inverse().Multiply(g);
                wNew.Add(w);
            } while (++iters != maxIterations && GetDistance(w, wNew) >= minDistance);

            return wNew.GetColumnVector(0);
        }

        private static double GetDistance(Matrix w, Matrix wNew)
        {
            // this alternative code uses a Euclidean distance metric to determine convergence
            //double diff, dist = 0;
            //for (int i = 0; i < w.RowCount; i++)
            //{
            //    diff = w[i, 0] - wNew[i, 0];
            //    dist += diff * diff;
            //}

            //return dist;

            return Enumerable.Range(0, w.RowCount).Select(i => Math.Abs(w[i, 0] - wNew[i, 0])).Max();
        }

        private static double CalcLogistic(double input)
        {
            return 1.0 / (1.0 + Math.Exp(input));
        }

        /// <summary>
        /// Returns 0
        /// </summary>
        public override int NegativeExampleValue
        {
            get { return 0; }
        }

        /// <summary>
        /// Returns 1
        /// </summary>
        public override int PositiveExampleValue
        {
            get { return 1; }
        }

        /// <summary>
        /// Performs the regression
        /// </summary>
        protected override void TrainBinary(IArrayView<Example> binaryExamples)
        {
            Matrix X = new Matrix(binaryExamples[0].Features.Count + 1, binaryExamples.Count), d = new Matrix(1, X.ColumnCount);
            int i = 0, j = 0;
            foreach (var example in binaryExamples)
            {
                d[0, i] = example.Class;
                foreach (var dataPoint in example.Features)
                    X[j++, i] = dataPoint;
                X[j, i] = 1; // pad with 1s to incorporate b

                j = 0;
                i++;
            }

            this.weights = ComputeWeights(X, d, this.Lambda, this.MinDistance, this.MaxIterations);
        }

        /// <summary>
        /// Performs classification
        /// </summary>
        protected override int PredictBinary(Example binaryExample, out double confidence)
        {
            double p1 = this.Project(binaryExample.Features);

            if (p1 >= 0.5)
            {
                confidence = p1;
                return this.PositiveExampleValue;
            }
            else
            {
                confidence = 1 - p1;
                return this.NegativeExampleValue;
            }
        }

        /// <summary>
        /// Projects features onto weights
        /// </summary>
        public double Project(IEnumerable<double> features)
        {
            return CalcLogistic(this.weights.InnerProduct(features.Then(1)));
        }
    }
}
