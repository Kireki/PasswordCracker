using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.ServiceModel;
using System.Web.Services;
using log4net;
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
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, ConcurrencyMode = ConcurrencyMode.Single)]
    // To allow this Web Service to be called from script, using ASP.NET AJAX, uncomment the following line. 
    // [System.Web.Script.Services.ScriptService]
    public class Cracker : WebService
    {
        private static readonly ILog log;
        private static List<UserInfoClearText> _result;
        private static ConcurrentBag<DictionaryChunk> _chunks;
        private static readonly List<UserInfo> _passwordList;

        public static List<UserInfo> PasswordList
        {
            get { return _passwordList; }
        }

        public static ConcurrentBag<DictionaryChunk> Chunks
        {
            get { return _chunks; }
            private set { _chunks = value; }
        }



        static Cracker()
        {
            log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
            _passwordList = PasswordFileHandler.ReadPasswordFile("passwords.txt");
            List<string> wholeDictionary = new List<string>();
            using (FileStream fs = new FileStream("C:/temp/webster-dictionary.txt", FileMode.Open, FileAccess.Read))
            using (StreamReader dictionary = new StreamReader(fs))
            {
                while (!dictionary.EndOfStream)
                {
                    wholeDictionary.Add(dictionary.ReadLine());
                }
            }
            Chunks = new ConcurrentBag<DictionaryChunk>();
            Chunks = Batch(Chunks, wholeDictionary, 30000);


//            var partitions = Partitioner.Create(0, wholeDictionary.Count);
//            Parallel.ForEach(partitions, range =>
//            {
//                DictionaryChunk newChunk = new DictionaryChunk();
//                newChunk.Processed = false;
//                for (int i = range.Item1; i < range.Item2; ++i)
//                {
//                    newChunk.Words.Add(wholeDictionary[i]);
//                }
//                Chunks.Add(newChunk);
//            });
        }

        public static ConcurrentBag<DictionaryChunk> Batch(ConcurrentBag<DictionaryChunk> result, List<string> collection, int batchSize)
        {
            DictionaryChunk nextChunk = new DictionaryChunk(batchSize);
            foreach (string item in collection)
            {
                nextChunk.Words.Add(item);
                if (nextChunk.Words.Count == batchSize)
                {
                    result.Add(nextChunk);
                    nextChunk = new DictionaryChunk(batchSize);
                }
            }
            if (nextChunk.Words.Count > 0)
                result.Add(nextChunk);
            return result;
        }

        public List<UserInfoClearText> Result
        {
            get { return _result; }
        }

        [WebMethod]
        public List<UserInfo> GetPasswordList()
        {
            List<UserInfo> userInfos = PasswordFileHandler.ReadPasswordFile("C:/temp/passwords.txt");
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
            DictionaryChunk chunk;
            if (Chunks.TryTake(out chunk))
            {
                log.Info("Got a chunk. Wordcount: " + chunk.Words.Count + "First Word: " + chunk.Words[0] + " Last word: " + chunk.Words[chunk.Words.Count - 1] + ".");
                return chunk;
            }
            log.Info("Got null.");
            return null;
        }

        [WebMethod]
        public void LogIt()
        {
            foreach (DictionaryChunk dictionaryChunk in Chunks)
            {
                log.Info("Count: " + dictionaryChunk.Words.Count + " Last: " + dictionaryChunk.Words[dictionaryChunk.Words.Count - 1] + ".");
            }
        }
        
    }
}
