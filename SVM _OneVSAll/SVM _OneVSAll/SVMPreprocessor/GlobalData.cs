using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SVMPreprocessor
{
    public static class GlobalData
    {
        public static HashSet<string> StopList;
        public static Dictionary<string, int> WordDictionary;
        public static int maxIndex = 0;
        public static string delimit = " ";
    }
}
