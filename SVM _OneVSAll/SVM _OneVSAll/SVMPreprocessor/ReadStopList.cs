using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Threading.Tasks;

namespace SVMPreprocessor
{
    public class ReadStopList
    {        
        public string stopListFile = "StopWords.txt";        

        public void PopulateStopList()
        {
            GlobalData.StopList = new HashSet<string>();
            var lines = File.ReadLines(stopListFile);
           foreach (var line in lines)
           {
               GlobalData.StopList.Add(line.ToString());
           }
        }
    }
}
