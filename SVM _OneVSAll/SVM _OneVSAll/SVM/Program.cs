using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using System.Diagnostics;

namespace SVM
{
    class Program
    {
        static void Main(string[] args)
        {
            svmLearn svm = new svmLearn();
            Directory.CreateDirectory("Models");
            string[] filePaths = Directory.GetFiles("ModelInputs","*.*", SearchOption.AllDirectories);
            Console.WriteLine("Building svm models..");
            foreach (string eachFile in filePaths)
            {
                //string eachFile = @"ModelInputs\alt.atheism_VS_comp.graphics";
                string filename = Path.GetFileName(eachFile);
                Console.WriteLine("Starting " + filename);
                svm.ReadInput(eachFile);
                svm.initialise();
                Stopwatch sw = Stopwatch.StartNew();
                svm.learn();
                sw.Stop();
                string[] parts = filename.Split(new string[] { "_VS_" }, StringSplitOptions.None);
                svm.posClass = parts[0];
                svm.negClass = parts[1];
                svm.WriteModelFile(@"Models\"+filename);
                Console.WriteLine("Building " + filename + " model done in " + sw.Elapsed);
                Console.WriteLine();
            }            
            
            Console.ReadLine();
        }
    }
}
