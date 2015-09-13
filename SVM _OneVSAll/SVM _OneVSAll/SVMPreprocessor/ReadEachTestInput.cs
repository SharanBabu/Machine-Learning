using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text.RegularExpressions;

using System.Text;
using System.Threading.Tasks;

namespace SVMPreprocessor
{
    public class ReadEachTestInput
    {
        public string classlabel;
        private Stemmer stemmer;
        public SortedDictionary<int, int> WordFrequency;

        public void Init()
        {
            WordFrequency = new SortedDictionary<int, int>();
            stemmer = new Stemmer();
        }
        public string GetOutputString()
        {
            string res = classlabel;
            foreach (KeyValuePair<int, int> entry in WordFrequency)
            {
                if (entry.Value > 0)
                {
                    res += GlobalData.delimit + entry.Key + ":" + entry.Value;
                }
            }
            return res;
        }
        public string ReadFileNConstructDictionaries(string inputfile)
        {
            Init();
            //read each line
            //read each word -> space delimit
            //trim each word for special characters at the begin and end
            //check for numbers, stop words
            //else check if already available
            //if not available add new word to dictionary 
            //else increment count

            var fileContent = File.ReadAllText(inputfile);
            var array = fileContent.Split((string[])null, StringSplitOptions.RemoveEmptyEntries);
            foreach (var lexicon in array)
            {
                string lex = lexicon.ToString();
                string tmp = Regex.Replace(lex, "[^a-zA-Z]+", "");//remove all special characters and numbers
                if (string.IsNullOrWhiteSpace(tmp) || tmp.Length < 2)
                {//remove all empty or single characters
                    continue;
                }
                tmp = tmp.ToLower();//convert all chars into lowercase irrespective of their case
                if (!GlobalData.StopList.Contains(tmp))
                {
                    stemmer.add(tmp.ToCharArray(), tmp.Length);
                    stemmer.stem();
                    tmp = stemmer.ToString();
                    if (GlobalData.WordDictionary.Keys.Contains(tmp))//already found and present in the dictionaries
                    {
                        int index = GlobalData.WordDictionary[tmp];
                        if (WordFrequency.Keys.Contains(index))
                        {
                            WordFrequency[index]++;//increment count
                        }
                        else//not found in the frequency dictionary
                        {
                            WordFrequency.Add(index, 1);
                        }
                    }
                }
            }
            return GetOutputString();
        }

    }
}
