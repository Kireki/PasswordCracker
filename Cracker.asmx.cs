using System;
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
        private const int ChunkSize = 5000;
        private const string PasswordFile = "PasswordCrackerService.passwords.txt";
        private const string DictionaryFile = "PasswordCrackerService.webster-dictionary.txt";
//        private const string DictionaryFile = "PasswordCrackerService.webster-dictionary-reduced.txt"; //using reduced for faster local testing

        private static ConcurrentBag<List<string>> _chunks;
        private static readonly List<UserInfo> PasswordList;

        /// <summary>
        /// A constructor that initializes all the required variables.
        /// A static constructor and the "ServiceBehavior" tag ensure that there is only one instance of the object.
        /// </summary>
        /// <exception cref="ArgumentNullException"></exception>
        static Cracker()

        {
            Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
            Assembly assembly = Assembly.GetExecutingAssembly();
            PasswordList = PasswordFileHandler.ReadPasswordFile(PasswordFile);
            List<string> wholeDictionary = new List<string>();
            Stream dictStream = assembly.GetManifestResourceStream(DictionaryFile);
            if (dictStream != null)
                using (StreamReader dictionary = new StreamReader(dictStream))
                {
                    while (!dictionary.EndOfStream)
                    {
                        wholeDictionary.Add(dictionary.ReadLine());
                    }
                }
            else
            {
                throw new ArgumentNullException("Dictionary" + " is null.");
            }
            _chunks = new ConcurrentBag<List<string>>();
            _chunks = Batch(_chunks, wholeDictionary, ChunkSize);
        }

        /// <summary>
        /// Batches the dictionary into chunks.
        /// </summary>
        /// <param name="result">The container to be filled up with chunks.</param>
        /// <param name="collection">The whole dictionary.</param>
        /// <param name="batchSize">The size of a single batch.</param>
        /// <returns>The "result" argument.</returns>
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

        /// <summary>
        /// Gets the list of usernames and passwords.
        /// </summary>
        /// <returns>The PasswordList property of the class.</returns>
        [WebMethod(Description = "Get the list of usernames and encrypted passwords.")]
        public List<UserInfo> GetPasswordList()
        {
            List<UserInfo> result = new List<UserInfo>();
            foreach (var item in PasswordList)
            {
                if (!item.IsDecrypted)
                {
                    result.Add(item);
                }
            }
            return result;
        }

        /// <summary>
        /// Gets a single dictionary chunk from the container, removes it and sends it to client.
        /// </summary>
        /// <returns>A chunk of a dictionary.</returns>
        [WebMethod(Description = "Gets a single dictionary chunk from the container, removes it and sends it to client.")]
        public List<string> GetDictionaryChunk()
        {
            List<string> chunk;
            if (_chunks.TryTake(out chunk))
            {
                return chunk;
            }
            return null;
        }

        /// <summary>
        /// Logs the chunks inside the container for debugging purposes.
        /// </summary>
        [WebMethod(Description = "Logs the chunks inside the container for debugging purposes.")]
        public void LogIt()
        {
            foreach (List<string> dictionaryChunk in _chunks)
            {
                Log.Info("Count: " + dictionaryChunk.Count + " Last: " + dictionaryChunk[dictionaryChunk.Count - 1] + ".");
            }
        }

        /// <summary>
        /// Logs the username and password received from client in clear text.
        /// </summary>
        /// <param name="result">Usernames and decrypted passwords.</param>
        [WebMethod(Description = "Logs the username and password received from client in clear text.")]
        public void LogResults(List<UserInfoClearText> result)
        {
            foreach (var item in result)
            {
                Log.Info(item);
            }
        }

        [WebMethod(Description = "Temporary method for resetting the dictionary chunks")]
        public void Reset()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            List<string> wholeDictionary = new List<string>();
            Stream dictStream = assembly.GetManifestResourceStream(DictionaryFile);
            if (dictStream != null)
                using (StreamReader dictionary = new StreamReader(dictStream))
                {
                    while (!dictionary.EndOfStream)
                    {
                        wholeDictionary.Add(dictionary.ReadLine());
                    }
                }
            else
            {
                throw new ArgumentNullException("Dictionary" + " is null.");
            }
            _chunks = new ConcurrentBag<List<string>>();
            _chunks = Batch(_chunks, wholeDictionary, ChunkSize);
            foreach (var item in PasswordList)
            {
                item.IsDecrypted = false;
            }
        }

        [WebMethod(Description = "Tells that the specific userInfo was decripted")]
        public void Decrypted(string name)
        {
            if (name == null)
                return;
            foreach (var userInfo in PasswordList)
            {
                if (name == userInfo.Username)
                {
                    userInfo.IsDecrypted = true;
                }
            }
        }
    }
}
