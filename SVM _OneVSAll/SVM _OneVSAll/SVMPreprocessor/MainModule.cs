using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;

namespace SVMPreprocessor
{
    public class MainModule
    {
        public Dictionary<string, string> outStringsDict = new Dictionary<string, string>();
        public List<string> testStrings = new List<string>();
        public Dictionary<string, string> classLabelNames = new Dictionary<string, string>();

        public void WriteToModelInput(string posDir, string negDir, string modelInputFilePath)
        {
            List<string> outStrs = new List<string>();
            string[] allPosFiles = Directory.GetFiles(posDir, "*.*", SearchOption.AllDirectories);
            foreach (string eachPosFile in allPosFiles)
            {
                string classAppendedStr = "+1 " + outStringsDict[eachPosFile];
                outStrs.Add(classAppendedStr);
            }
            string[] allNegDirs = Directory.GetDirectories(negDir, "*", SearchOption.TopDirectoryOnly);
            foreach (string eachNegDir in allNegDirs)
            {
                if (!string.Equals(eachNegDir, posDir))//all directories except the positive directory
                {
                    string[] allNegFiles = Directory.GetFiles(eachNegDir, "*.*", SearchOption.AllDirectories);
                    foreach (string eachNegFile in allNegFiles)
                    {
                        string classAppendedStr = "-1 " + outStringsDict[eachNegFile];
                        outStrs.Add(classAppendedStr);
                    }
                }
            }
            WriteToInput(modelInputFilePath, outStrs);
        }
        public void StartPreprocessing()
        {
            Console.WriteLine("Preprocessing of the stop file done!");
            //read and populate all stop list
            ReadStopList rsl = new ReadStopList();
            rsl.PopulateStopList();

            //initialise the word dictionary before it is populated in the later methods
            GlobalData.WordDictionary = new Dictionary<string, int>();
            ReadEachData rd = new ReadEachData();

            Console.WriteLine("Building dictionaries from entire document collection..");
            string[] filePaths = Directory.GetFiles("train", "*.*", SearchOption.AllDirectories);
            foreach (string eachFile in filePaths)
            {

                outStringsDict.Add(eachFile, rd.ReadFileNConstructDictionaries(eachFile));
            }
            Console.WriteLine("Preprocessing of the train files done!");
            string[] allClassNames = File.ReadAllLines("class_name.txt");
            foreach (string eachclass in allClassNames)
            {
                string[] ecl = eachclass.Split(' ');
                classLabelNames.Add(ecl[1], ecl[0]);
            }
            Console.WriteLine("Processing of the class label files done!");
            List<string> allKeys = classLabelNames.Keys.ToList();

            Directory.CreateDirectory("ModelInputs");
            //forms modelinput for each Class Vs rest of the classes
            for (int i = 0; i < allKeys.Count; i++)
            {
                Console.WriteLine("Creating model for " + allKeys[i] + " Vs All");
                string posDir = @"train\" + allKeys[i];
                string negDir = "train";

                WriteToModelInput(posDir, negDir, @"ModelInputs\" + allKeys[i] + "_VS_All");
            }


            Console.WriteLine("Preprocessing of the test files started!");
            Dictionary<string, string> devlabelDict = new Dictionary<string, string>();
            string[] allDevLabels = File.ReadAllLines("dev_label.txt");
            foreach (string eachLine in allDevLabels)
            {
                string[] parts = eachLine.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                devlabelDict.Add(parts[0], parts[1]);
            }

            //store all dev labels
            string[] testfilePaths = Directory.GetFiles("dev", "*.*", SearchOption.AllDirectories);
            ReadEachTestInput rtd = new ReadEachTestInput();

            foreach (string eachFile in testfilePaths)
            {
                rtd.classlabel = devlabelDict[Path.GetFileName(eachFile)];
                testStrings.Add(rtd.ReadFileNConstructDictionaries(eachFile));
            }
            Directory.CreateDirectory("TestInput");
            WriteToInput(@"TestInput\test.txt", testStrings);
            Console.WriteLine("Preprocessing of the test files done!");
        }

        public void WriteToInput(string modelInput, List<string> outStrings)
        {
            System.IO.File.WriteAllLines(modelInput, outStrings);
        }
    }
}
