using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;

namespace SVMClassify
{
    class Program
    {
        static void Main(string[] args)
        {
            string classfile = "class_name.txt";
            string testfile = @"TestInput\test.txt";
            string[] allModelfilePaths = Directory.GetFiles("Models", "*.*", SearchOption.AllDirectories);
            string[] allBackupModelfilePaths = Directory.GetFiles("Models_backup", "*.*", SearchOption.AllDirectories);
            SVMClassify svmclassify = new SVMClassify();
            double accuracy = svmclassify.classify(classfile, testfile, allModelfilePaths, allBackupModelfilePaths);
            Console.WriteLine("SVM Accuracy : " + accuracy + "%");
            Console.ReadLine();
        }
    }
}
