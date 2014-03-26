using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Services;
using PasswordCrackerService.model;
using PasswordCrackerService.util;

namespace PasswordCrackerService
{
    /// <summary>
    /// Summary description for Cracker
    /// </summary>
    [WebService(Namespace = "http://tempuri.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]
    // To allow this Web Service to be called from script, using ASP.NET AJAX, uncomment the following line. 
    // [System.Web.Script.Services.ScriptService]
    public class Cracker : WebService
    {
//        public static int ClientCount;
        private static List<UserInfoClearText> _result;
        private static ConcurrentBag<DictionaryChunk> _chunks;
        private static readonly List<UserInfo> _passwordList = PasswordFileHandler.ReadPasswordFile("passwords.txt");

        public static List<UserInfo> PasswordList
        {
            get { return _passwordList; }
        }

        public static ConcurrentBag<DictionaryChunk> Chunks
        {
            get { return _chunks; }
        }

        static Cracker()
        {
            List<string> wholeDictionary = new List<string>();
            using (FileStream fs = new FileStream("webster-dictionary.txt", FileMode.Open, FileAccess.Read))
            using (StreamReader dictionary = new StreamReader(fs))
            {
                while (!dictionary.EndOfStream)
                {
                    wholeDictionary.Add(dictionary.ReadLine());
                }
            }
            var partitions = Partitioner.Create(0, wholeDictionary.Count, 10);
            Parallel.ForEach(partitions, range =>
            {
                int length = range.Item2 - range.Item1;
                string[] wordArray = new string[length];
                for (int i = range.Item1; i < range.Item2; ++i)
                {
                    wordArray[i - range.Item1] = wholeDictionary[i];
                }
                DictionaryChunk newChunk = new DictionaryChunk();
                newChunk.Words.AddRange(wordArray);
                Chunks.Add(newChunk);
            });
        }

        public List<UserInfoClearText> Result
        {
            get { return _result; }
        }

        [WebMethod]
        public List<UserInfo> GetPasswordList()
        {
            List<UserInfo> userInfos = PasswordFileHandler.ReadPasswordFile("passwords.txt");
            return userInfos;
        }

        [WebMethod]
        public void SendResults(UserInfoClearText result)
        {
            Result.Add(result);
        }

        [WebMethod]
        public DictionaryChunk GetDictionaryChunk()
        {
            return Chunks.FirstOrDefault(chunk => !chunk.Processed);
        }
    }
}
