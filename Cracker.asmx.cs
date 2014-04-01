using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.ServiceModel;
using System.Web.Services;
using System.Web.Services.Protocols;
using log4net;
using PasswordCrackerService.model;
using PasswordCrackerService.util;

namespace PasswordCrackerService
{
    /// <summary>
    /// A password cracking webservice that uses a dictionary and common variations
    /// </summary>
    [WebService(Namespace = "http://www.tempuri.org")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, ConcurrencyMode = ConcurrencyMode.Single)]
    [SoapDocumentService(RoutingStyle = SoapServiceRoutingStyle.RequestElement)]
    public class Cracker : WebService
    {
        private static readonly ILog Log;
        private const int ChunkSize = 10000;
        private const string PasswordFilePath = "C:/temp/passwords.txt";
        private const string DictionaryFilePath = "C:/temp/webster-dictionary.txt";
        private static readonly ConcurrentBag<List<string>> Chunks;
        private static readonly List<UserInfo> PasswordList;

        static Cracker()

        {
            Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
            PasswordList = PasswordFileHandler.ReadPasswordFile(PasswordFilePath);
            List<string> wholeDictionary = new List<string>();
            using (FileStream fs = new FileStream(DictionaryFilePath, FileMode.Open, FileAccess.Read))
            using (StreamReader dictionary = new StreamReader(fs))
            {
                while (!dictionary.EndOfStream)
                {
                    wholeDictionary.Add(dictionary.ReadLine());
                }
            }
            Chunks = new ConcurrentBag<List<string>>();
            Chunks = Batch(Chunks, wholeDictionary, ChunkSize);
        }

        public static ConcurrentBag<List<string>> Batch(ConcurrentBag<List<string>> result, List<string> collection, int batchSize)
        {
            List<string> nextChunk = new List<string>(batchSize);
            foreach (string item in collection)
            {
                nextChunk.Add(item);
                if (nextChunk.Count == batchSize)
                {
                    result.Add(nextChunk);
                    nextChunk = new List<string>(batchSize);
                }
            }
            if (nextChunk.Count > 0)
                result.Add(nextChunk);
            return result;
        }

        [WebMethod]
        public List<UserInfo> GetPasswordList()
        {
            return PasswordList;
        }

        [WebMethod]
        public List<string> GetDictionaryChunk()
        {
            List<string> chunk;
            if (Chunks.TryTake(out chunk))
            {
//                Log.Info("Sent a chunk. Wordcount: " + chunk.Count + "First Word: " + chunk[0] +
//                         " Last word: " + chunk[chunk.Count - 1] + ".");
                return chunk;
            }
//            Log.Info("Got null.");
            return null;
        }

        [WebMethod]
        public void LogIt()
        {
            foreach (List<string> dictionaryChunk in Chunks)
            {
                Log.Info("Count: " + dictionaryChunk.Count + " Last: " + dictionaryChunk[dictionaryChunk.Count - 1] + ".");
            }
        }

        [WebMethod]
        public void LogResults(List<UserInfoClearText> result)
        {
            foreach (var item in result)
            {
                Log.Info(item);
            }
        }
    }
}
