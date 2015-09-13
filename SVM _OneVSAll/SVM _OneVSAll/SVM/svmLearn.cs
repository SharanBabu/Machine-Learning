using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace SVM
{
    public class feature
    {
        public int id;
        public int value;
    }
    public class svmLearn
    {
        #region public members

        //data structures to store the input file content
        public List<List<feature>> featureValues;
        public List<int> yValues;
        public int numExamples;
        public int maxNumFeature = 0;
        public int iterations = 0;

        //data structures for learning
        public double[] weight;
        public double[] lambda;
        public double b;
        public double c;
        public double E1;
        public double[] error;
        public int[] unboundIndicator;

        //additional data structures to improve performance
        public List<int> unboundIndices;
        public List<int> nonZeroIndices;
        public List<int> errorCache;

        public string posClass;
        public string negClass;

        #endregion public members

        #region public functions

        public double Max(double x, double y)
        {
            return (x > y) ? x : y;
        }
        public double Min(double x, double y)
        {
            return (x < y) ? x : y;
        }
        public double dotProduct(List<feature> x, List<feature> y)
        {
            double res = 0;
            int x_size = x.Count;
            int y_size = y.Count;
            int i = 0, j = 0, xid, yid;
            while (i < x_size && j < y_size)
            {
                xid = x[i].id;
                yid = y[j].id;
                if (xid == yid)
                {
                    res += (x[i].value * y[j].value);
                    i++; j++;
                }
                else if (xid < yid)
                {
                    i++;
                }
                else
                {
                    j++;
                }
            }
            return res;
        }
        public double calculateError(int n)
        {
            double svmRes = 0;
            for (int i = 0; i < numExamples; i++)
            {
                if (!(lambda[i] < Double.Epsilon))
                {
                    svmRes += lambda[i] * yValues[i] * dotProduct(featureValues[i], featureValues[n]);
                }
            }
            return (svmRes - b - yValues[n]);

        }
        public void ReadInput(string filePath)
        {
            int h = 0;
            featureValues = new List<List<feature>>();
            yValues = new List<int>();
            using (var mappedFile1 = MemoryMappedFile.CreateFromFile(filePath))
            {
                using (Stream mmStream = mappedFile1.CreateViewStream())
                {
                    using (StreamReader sr = new StreamReader(mmStream, ASCIIEncoding.ASCII))
                    {
                        while (!sr.EndOfStream)
                        {
                            var line = sr.ReadLine();
                            string[] lineWords = line.Split(' ');
                            int temp;
                            int yval = Int32.TryParse(lineWords[0], out temp) ? temp : +1;
                            yValues.Add(yval);
                            featureValues.Add(new List<feature>());
                            for (int i = 1; i < lineWords.Length; i++)
                            {
                                string[] fString = lineWords[i].Split(':');
                                feature tFeature = new feature();
                                tFeature.id = Int32.Parse(fString[0]);
                                tFeature.value = Int32.Parse(fString[1]);
                                featureValues[h].Add(tFeature);
                                if (tFeature.id > maxNumFeature)
                                {
                                    maxNumFeature = tFeature.id;
                                }
                            }
                            h++;
                        }
                    }
                }
            }
        }
        public void initialise()
        {
            numExamples = featureValues.Count;
            weight = new double[maxNumFeature + 1];//weight vector for this svm model
            for (int i = 0; i < maxNumFeature + 1; i++)
            {
                weight[i] = 0;
            }
            lambda = new double[numExamples];//lamda values for each example in the training file
            for (int i = 0; i < numExamples; i++)
            {
                lambda[i] = 0;
            }
            error = new double[numExamples];//error values for each example in the training file
            for (int i = 0; i < numExamples; i++)
            {
                error[i] = 0 - yValues[i];
            }
            unboundIndicator = new int[numExamples];//1 - if the example is unbound, 0 - if not
            for (int i = 0; i < numExamples; i++)
            {
                unboundIndicator[i] = 0;
            }

            unboundIndices = new List<int>(); //holds the indices of all unbound lambda values 
            nonZeroIndices = new List<int>(); //holds the indices of all non-zero lambda values 
            errorCache = new List<int>(); //holds the indices of error values of unbound lambdas 

            b = 0;
            c = 4.5;
        }
        public void learn()
        {
            int numchanged = 0, examineAll = 1;
            while (numchanged > 0 || examineAll == 1)
            {
                numchanged = 0;
                if (examineAll == 1)//loop I over all training examples
                {
                    for (int i = 0; i < numExamples; i++)
                    {
                        numchanged += examineExample(i);
                    }
                }
                else//loop I over examples where lambda is not 0 and not C
                {
                    //find index of all examples which are unbound
                    //List<int> allUnboundExamples = unboundIndicator.Select((item, index) => new { item, index })
                    //                                .Where(x => (x.item == 1))
                    //                                .Select(x => x.index).ToList();
                    for (int i = 0; i < unboundIndices.Count; i++)
                    {
                        numchanged += examineExample(unboundIndices[i]);
                    }
                }
                if (examineAll == 1)
                {
                    examineAll = 0;
                }
                else if (numchanged == 0)
                {
                    examineAll = 1;
                }
            }
        }

        //write the weight vector and b value to the model file
        public void WriteModelFile(string filePath)
        {
            int numSV = 0;
            string wtString = weight[0].ToString();
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(filePath))
            {
                //write the class
                string posClassStr = "+1 " + posClass;
                string negClassStr = "-1 " + negClass;
                file.WriteLine(posClassStr);
                file.WriteLine(negClassStr);
                //write weight
                for (int i = 1; i < weight.Length; i++)
                {
                    wtString += " " + weight[i].ToString();
                }
                file.WriteLine(wtString);
                //write b
                file.WriteLine(b.ToString());
                for (int i = 0; i < numExamples; i++)
                {
                    if (lambda[i] > 0)
                    {
                        numSV++;
                    }
                }
                //file.WriteLine("Number of Support vectors : "+numSV);
            }
        }
        #endregion public functions
        
        #region private
        private int examineExample(int ex1)
        {
            int y1 = yValues[ex1];
            bool isFound = false;
            int ex2 = 0, rindex;
            double lambda1 = lambda[ex1];
            double r1;
            if (unboundIndicator[ex1] == 1)
            {
                E1 = error[ex1];
            }
            else
            {
                E1 = calculateError(ex1);
            }

            r1 = E1 * y1;

            if ((r1 < -0.001 && lambda1 < c) || (r1 > 0.001 && lambda1 > 0))//checks KKT conditions
            {
                //E2 violates - so proceed
                //first option of second choice heuristic
                int numUnboundExamples = unboundIndices.Count;
                if (numUnboundExamples > 1)
                {
                    if (E1 > 0)
                    {                        
                        if (error[errorCache[0]] >= Double.Epsilon)
                        {
                            isFound = false;
                        }
                        else
                        {
                            ex2 = errorCache[0];
                            isFound = true;
                        }
                    }
                    else if (E1 < 0)
                    {                        
                        if (error[errorCache.Last()] <= Double.Epsilon)
                        {
                            isFound = false;
                        }
                        else
                        {
                            ex2 = errorCache.Last();
                            isFound = true;
                        }
                    }
                    if (isFound)
                    {
                        if (takeStep(ex1, ex2))
                        {
                            return 1;
                        }
                    }
                }
                //

                //second option of second choice heuristic
                if (numUnboundExamples > 1)
                {
                    Random rand = new Random();
                    //IEnumerable<int> allUnboundExamples = unboundIndicator.Select((item, index) => new { item, index })
                    //                                .Where(x => (x.item == 1))
                    //                                .Select(x => x.index);
                    rindex = rand.Next(unboundIndices.Count);
                    ex2 = unboundIndices[rindex];
                    for (int i = 0; i < numUnboundExamples; i++)
                    {
                        if (takeStep(ex1, ex2))
                            return 1;
                        rindex++;
                        if (rindex == numUnboundExamples)
                            rindex = 0;
                        ex2 = unboundIndices[rindex];
                    }
                }
                //

                //third option of second choice heuristic
                //IEnumerable<int> allNonZerolambdaExamples = lambda.Select((item, index) => new { item, index })
                //                                    .Where(x => (!(x.item < Double.Epsilon)))
                //                                    .Select(x => x.index);
                int numNonZeroLambda = nonZeroIndices.Count();
                if (numNonZeroLambda > 1)
                {
                    Random rand = new Random();
                    rindex = rand.Next(numNonZeroLambda);
                    ex2 = nonZeroIndices[rindex];
                    for (int i = 0; i < numNonZeroLambda; i++)
                    {
                        if (unboundIndicator[ex2] == 0)
                        {
                            if (takeStep(ex1, ex2))
                                return 1;
                        }
                        rindex++;
                        if (rindex == numNonZeroLambda)
                            rindex = 0;
                        ex2 = nonZeroIndices[rindex];
                    }
                }
                //

                //fourth option of second choice heuristic
                Random rando = new Random();
                ex2 = rando.Next(numExamples);
                for (int i = 0; i < numExamples; i++)
                {
                    if (lambda[ex2] < Double.Epsilon)
                    {
                        if (takeStep(ex1, ex2))
                        {
                            return 1;
                        }
                    }
                    ex2++;
                    if (ex2 == numExamples)
                        ex2 = 0;
                }
                //
            }
            return 0;
        }
        private bool takeStep(int e1, int e2)
        {
            double k11, k12, k22, eta;
            double L1, L2, H1, H2, a1, a2, f1, f2, Lobj, Hobj, lambda1, lambda2;
            int y1, y2, s;
            int unbound1, unbound2;
            double E2, b1, b2, oldb;
            if (e1 == e2)
            {
                return false;
            }
            lambda1 = lambda[e1];
            lambda2 = lambda[e2];
            y1 = yValues[e1];
            y2 = yValues[e2];
            s = y1 * y2;

            if (unboundIndicator[e2] == 1)
            {
                E2 = error[e2];
            }
            else
            {
                E2 = calculateError(e2);
            }

            if (y1 != y2)
            {
                L2 = Max(0, lambda2 - lambda1);
                H2 = Min(c, c + lambda2 - lambda1);
            }
            else
            {
                L2 = Max(0, lambda2 + lambda1 - c);
                H2 = Min(c, lambda2 + lambda1);
            }
            if (Math.Abs(L2 - H2) < double.Epsilon)
            {
                return false;
            }
            k11 = dotProduct(featureValues[e1], featureValues[e1]);
            k12 = dotProduct(featureValues[e1], featureValues[e2]);
            k22 = dotProduct(featureValues[e2], featureValues[e2]);

            eta = 2 * k12 - k11 - k22;
            if (eta < 0)
            {
                a2 = lambda2 - y2 * (E1 - E2) / eta;
                if (a2 < L2)
                    a2 = L2;
                else if (a2 > H2)
                    a2 = H2;
            }
            else
            {
                L1 = lambda1 + s * (lambda2 - L2);
                H1 = lambda1 + s * (lambda2 - H2);
                f1 = y1 * (E1 + b) - (lambda1 * k11) - (s * lambda2 * k12);
                f2 = y2 * (E2 + b) - (lambda2 * k22) - (s * lambda1 * k12);
                Lobj = -0.5 * L1 * L1 * k11 - 0.5 * L2 * L2 * k22 - s * L1 * L2 * k12 - L1 * f1 - L2 * f2;
                Hobj = -0.5 * H1 * H1 * k11 - 0.5 * H2 * H2 * k22 - s * H1 * H2 * k12 - H1 * f1 - H2 * f2;
                if (Lobj > Hobj + Double.Epsilon)
                    a2 = L2;
                else if (Lobj < Hobj - Double.Epsilon)
                    a2 = H2;
                else
                    a2 = lambda2;
            }
            if (Math.Abs(a2 - lambda2) < Double.Epsilon * (a2 + lambda2 + Double.Epsilon))
                return false;

            a1 = lambda1 + s * (lambda2 - a2);
            if (a1 < 0)
                a1 = 0;

            if (a1 > 0 && a1 < c)
                unbound1 = 1;
            else
                unbound1 = 0;

            if (a2 > 0 && a2 < c)
                unbound2 = 1;
            else
                unbound2 = 0;

            //update nonzero lambda cache
            if (a1 > 0)
            {
                if (lambda[e1] < Double.Epsilon)
                {//it was zero previously
                    nonZeroIndices.Add(e1);
                    nonZeroIndices.Sort();
                }
            }
            if (a2 > 0)
            {
                if (lambda[e2] < Double.Epsilon)
                {//it was zero previously
                    nonZeroIndices.Add(e2);
                    nonZeroIndices.Sort();
                }
            }
            //update b
            oldb = b;

            b1 = E1 + oldb + y1 * (a1 - lambda1) * k11 + y2 * (a2 - lambda2) * k12;
            b2 = E2 + oldb + y1 * (a1 - lambda1) * k12 + y2 * (a2 - lambda2) * k22;

            if (Math.Abs(b1 - b2) < Double.Epsilon)
                b = b1;
            else if ((unbound1 != 1) && (unbound2 != 1))
                b = (b1 + b2) / 2;
            else
                if (unbound1 == 1)
                    b = b1;
                else
                    b = b2;
            //update weight vector
            for (int i = 0; i < featureValues[e1].Count; i++)
            {
                weight[featureValues[e1][i].id] += y1 * (a1 - lambda1) * (featureValues[e1][i].value);
            }
            for (int i = 0; i < featureValues[e2].Count; i++)
            {
                weight[featureValues[e2][i].id] += y2 * (a2 - lambda2) * (featureValues[e2][i].value);
            }
            //update error
            IEnumerable<int> allUnboundExamples = unboundIndicator.Select((item, index) => new { item, index })
                                                    .Where(x => (x.item == 1))
                                                    .Select(x => x.index);
            foreach (int k in allUnboundExamples)
            {
                error[k] += y1 * (a1 - lambda1) * dotProduct(featureValues[e1], featureValues[k]) +
                    y2 * (a2 - lambda2) * dotProduct(featureValues[e2], featureValues[k]) + oldb - b;
            }
            //update lambda
            lambda[e1] = a1;
            lambda[e2] = a2;
            if (unbound1 == 1)
                error[e1] = 0;
            if (unbound2 == 1)
                error[e2] = 0;

            qsort(errorCache, 0, errorCache.Count-1,error);

            //update unboundIndicator, unboundIndices                

            if (unboundIndicator[e1] == 0 && unbound1 == 1)
            {
                unboundIndicator[e1] = unbound1;
                unboundIndices.Add(e1);
                unboundIndices.Sort();//retrieval from a sorted array is more efficient

                errorCache.Add(e1);
                qsort(errorCache, 0, errorCache.Count - 1, error);
            }
            else if (unboundIndicator[e1] == 1 && unbound1 == 0)
            {
                unboundIndicator[e1] = unbound1;
                unboundIndices.Remove(e1);
                unboundIndices.Sort();

                errorCache.Remove(e1);
                qsort(errorCache, 0, errorCache.Count - 1, error);
            }
            if (unboundIndicator[e2] == 0 && unbound2 == 1)
            {
                unboundIndicator[e2] = unbound2;
                unboundIndices.Add(e2);
                unboundIndices.Sort();//retrieval from a sorted array is more efficient

                errorCache.Add(e2);
                qsort(errorCache, 0, errorCache.Count - 1, error);
            }
            else if (unboundIndicator[e2] == 1 && unbound2 == 0)
            {
                unboundIndicator[e2] = unbound2;
                unboundIndices.Remove(e2);
                unboundIndices.Sort();

                errorCache.Remove(e2);
                qsort(errorCache, 0, errorCache.Count - 1, error);
            }

            iterations++;
            //Console.WriteLine("Iteration: " + iterations);
            return true;
        }
        private void print2DList(List<List<feature>> inp)
        {
            for (int i = 0; i < inp.Count; i++)
            {
                for (int j = 0; j < inp[i].Count; j++)
                {
                    Console.Write(inp[i][j].id + ":" + inp[i][j].value);

                }
                Console.WriteLine();
            }
        }

        private void Swap(List<int> list, int indexA, int indexB)
        {
            int tmp = list[indexA];
            list[indexA] = list[indexB];
            list[indexB] = tmp;
        }

        private void qsort(List<int> index, int left, int right, double[] values)
        {
            int i, last;
            if(left >= right){
                return;
            }
            Swap(index, left, (left+right)/2);
            last = left;
            for (i = left + 1; i <= right;i++ )
            {
                if(values[index[i]] < values[index[left]]){
                    Swap(index,++last,i);
                }
            }
            Swap(index, left, last);
            qsort(index,left,last-1,values);
            qsort(index, last+1, right, values);

        }
        #endregion private
    }
    
}
