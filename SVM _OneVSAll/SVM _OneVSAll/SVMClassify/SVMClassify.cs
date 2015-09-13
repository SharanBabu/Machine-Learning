using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace SVMClassify
{
    public class feature
    {
        public int id;
        public int value;
    }

    public class SVMClassify
    {
        public List<List<feature>> featureValues;
        public Dictionary<string, string> classLabelNames = new Dictionary<string, string>();
        public List<string> yValues;
        public int numExamples;
        public int maxNumFeature = 0;
        public List<SVMModel> modelList = new List<SVMModel>();
        public Dictionary<string, SVMModel> backupModelList = new Dictionary<string, SVMModel>();

        public void readTestFile(string filePath)
        {
            int c = 0;
            featureValues = new List<List<feature>>();
            yValues = new List<string>();
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
                            string yval = lineWords[0];
                            yValues.Add(yval);
                            featureValues.Add(new List<feature>());
                            for (int i = 1; i < lineWords.Length; i++)
                            {
                                string[] fString = lineWords[i].Split(':');
                                feature tFeature = new feature();
                                tFeature.id = Int32.Parse(fString[0]);
                                tFeature.value = Int32.Parse(fString[1]);
                                featureValues[c].Add(tFeature);
                                if (tFeature.id > maxNumFeature)
                                {
                                    maxNumFeature = tFeature.id;
                                }
                            }
                            c++;
                        }
                    }
                }
            }
        }

        public SVMModel readModelFile(string modelFile)
        {
            SVMModel mm = new SVMModel();
            string posClassName;
            string negClassName;
            double[] weight;
            double b;
            string[] allLines = File.ReadAllLines(modelFile);
            string[] posClassLineParts = allLines[0].Split(' ');
            posClassName = posClassLineParts[1];
            string[] negClassLineParts = allLines[1].Split(' ');
            negClassName = negClassLineParts[1];

            string[] wtstr = allLines[2].Split(' ');
            weight = new double[wtstr.Count()];
            for (int w = 0; w < wtstr.Count(); w++)
            {
                if (!string.IsNullOrWhiteSpace(wtstr[w]))
                {
                    weight[w] = double.Parse(wtstr[w]);
                }
            }

            string bstr = allLines[3];
            b = 0.0;
            if (!string.IsNullOrWhiteSpace(bstr))
            {
                b = double.Parse(bstr);
            }

            mm.b = b;
            mm.weight = weight;
            mm.posClassName = posClassName;
            mm.negClassName = negClassName;
            return mm;
        }

        public void ReadAllModels(string[] modelFiles)
        {            
            foreach (string eachModel in modelFiles)
            {
                modelList.Add(readModelFile(eachModel));
            }
        }
        public void ReadAllBackupModels(string[] modelFiles)
        {
            foreach (string eachModel in modelFiles)
            {
                backupModelList.Add(Path.GetFileName(eachModel), readModelFile(eachModel));
            }
        }
        public string classifyUsingModel(List<feature> featurevalues, SVMModel sm)
        {
            double svmOut = weightDotProduct(sm.weight, featurevalues);
            return ((svmOut - sm.b) > Double.Epsilon ? sm.posClassName : sm.negClassName);
        }

        public double classify(string classfile, string testFile, string[] modelFiles, string[] backupModelFiles)
        {           
            double accuracy = 0.0;
            int numCorrect = 0;
            readTestFile(testFile);
            readClassnames(classfile);
            ReadAllModels(modelFiles);
            ReadAllBackupModels(backupModelFiles);
            numExamples = featureValues.Count();
            for (int i = 0; i < numExamples;i++ )
            {
                //double svmOut = weightDotProduct(weight, featureValues[i]);
                //Console.WriteLine((svmOut - b) > Double.Epsilon ? posClassName:negClassName);
                Dictionary<string, int> eachClassFreq = getAllClassFreqDict();
                
                foreach(SVMModel eachModel in modelList)
                {                   
                    string resClass = classifyUsingModel(featureValues[i], eachModel);
                    eachClassFreq[resClass]++;
                }
                //var max = eachClassFreq.Values.Max();
                List<string> finalClassNames = eachClassFreq.Where(pair => pair.Value > 0)
                                    .Select(pair => pair.Key).ToList();
                finalClassNames.Remove("All");
                if(finalClassNames.Count() == 1)
                {
                    string classified_classlabel = classLabelNames[finalClassNames[0]];
                    string actual_classlabel = yValues[i];
                    if (classified_classlabel == actual_classlabel)
                    {
                        numCorrect++;
                    }                    
                }
                else if (finalClassNames.Count() > 1)
                {
                    Dictionary<string, int> eachBackupClassFreq = getAllClassFreqDict();
                    for (int m = 0; m < finalClassNames.Count - 1; m++)
                    {
                        for (int n = m + 1; n < finalClassNames.Count; n++)
                        {                            
                            string resClass = classifyUsingModel(featureValues[i], backupModelList[finalClassNames[m]+"_VS_"+finalClassNames[n]]);
                            eachBackupClassFreq[resClass]++;
                        }
                    }
                    var max = eachBackupClassFreq.Values.Max();
                    string finalClassName = eachBackupClassFreq.Where(pair => max.Equals(pair.Value))
                                    .Select(pair => pair.Key).FirstOrDefault();
                    string classified_classlabel = classLabelNames[finalClassName];
                    string actual_classlabel = yValues[i];
                    if (classified_classlabel == actual_classlabel)
                    {
                        numCorrect++;
                    }
                }          
                                
                Console.WriteLine("Number of examples completed : " + (i + 1));
            }                        
            accuracy = ((double)numCorrect / numExamples) * 100;
            return accuracy;
        }
        

        private void readClassnames(string classFile)
        {
            string[] allClassNames = File.ReadAllLines(classFile);
            foreach (string eachclass in allClassNames)
            {
                string[] ecl = eachclass.Split(' ');
                classLabelNames.Add(ecl[1], ecl[0]);//<name> - <label>
            }            
        }

        private Dictionary<string, int> getAllClassFreqDict()
        {
            Dictionary<string, int> eachClassFreq = new Dictionary<string, int>();
            foreach (KeyValuePair<string, string> kv in classLabelNames)
            {
                eachClassFreq.Add(kv.Key, 0);
            }
            eachClassFreq.Add("All", 0);
            return eachClassFreq;
        }


        private double weightDotProduct(double[] weight, List<feature> y)
        {
            double res = 0;
            int weight_size = weight.Length;
            int y_size = y.Count;
            int i = 0, j = 0, wid, yid;
            while (i < weight_size && j < y_size)
            {
                wid = i;
                yid = y[j].id;
                if (wid == yid)
                {
                    res += (weight[i] * y[j].value);
                    i++; j++;
                }
                else if (wid < yid)
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

    }
}
