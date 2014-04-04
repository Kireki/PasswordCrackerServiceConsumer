using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using PasswordCrackerServiceConsumer.Cracker;
using PasswordCrackerServiceConsumer.util;

namespace PasswordCrackerServiceConsumer
{
    public class Cracking
    {
        /// <summary>
        /// Runs the password cracking algorithm
        /// </summary>
        public void RunCracking()
        {
            Stopwatch sw = new Stopwatch();
            Console.WriteLine("Started!");
            sw.Start();
            CrackerSoapClient cracker = new CrackerSoapClient();
            Console.WriteLine("Initialized cracker web-service.");
            List<UserInfo> userInfos;
            try
            {
                userInfos = cracker.GetPasswordList();
            }
            catch (Exception)
            {
                Console.WriteLine("Couldn't get the password file. Check if the service really has it and you can connect to it.");
                return;
            }
            Console.WriteLine("Got " + userInfos.Count + " credentials to process. Usernames: ");
            foreach (var userInfo in userInfos)
            {
                Console.Write(userInfo.Username + " ");
            }
            Console.WriteLine();
            List<UserInfoClearText> result = new List<UserInfoClearText>();
            List<string> dictChunk = cracker.GetDictionaryChunk();
            double[] processedWords = {0};
            while (dictChunk != null)
            {
                Console.WriteLine("\nGot a chunk: " + dictChunk.Count + ". First: " + dictChunk[0] + ", last: " + dictChunk[dictChunk.Count - 1]);
                var partitions = Partitioner.Create(0, dictChunk.Count, 2000);
                List<string> chunk = dictChunk;
                Parallel.ForEach(partitions, range =>
                {
                    for (int i = range.Item1; i < range.Item2; i++)
                    {
                        processedWords[0]++;
                        Console.Write("\r{0}% of words processed.", Math.Ceiling(processedWords[0]/chunk.Count * 100));
                        IEnumerable<UserInfoClearText> partialResult = CheckWordWithVariations(chunk[i], userInfos);
                        result.AddRange(partialResult);
                    }
                });
                processedWords[0] = 0;
                Console.WriteLine("Done!");
                dictChunk = cracker.GetDictionaryChunk();
            }
            cracker.LogResults(result);
            Console.WriteLine("No more chunks!");
            sw.Stop();
            Console.WriteLine("Time elapsed: {0}", sw.Elapsed);
        }

        /// <summary>
        /// Generates a lot of variations, encrypts each of the and compares it to all entries in the password file
        /// </summary>
        /// <param name="dictionaryEntry">A single word from the dictionary</param>
        /// <param name="userInfos">List of (username, encrypted password) pairs from the password file</param>
        /// <returns>A list of (username, readable password) pairs. The list might be empty</returns>
        private IEnumerable<UserInfoClearText> CheckWordWithVariations(String dictionaryEntry, List<UserInfo> userInfos)
        {
            List<UserInfoClearText> result = new List<UserInfoClearText>();

            String possiblePassword = dictionaryEntry;
            IEnumerable<UserInfoClearText> partialResult = CheckSingleWord(userInfos, possiblePassword);
            result.AddRange(partialResult);

            String possiblePasswordUpperCase = dictionaryEntry.ToUpper();
            IEnumerable<UserInfoClearText> partialResultUpperCase = CheckSingleWord(userInfos, possiblePasswordUpperCase);
            result.AddRange(partialResultUpperCase);

            String possiblePasswordCapitalized = StringUtilities.Capitalize(dictionaryEntry);
            IEnumerable<UserInfoClearText> partialResultCapitalized = CheckSingleWord(userInfos, possiblePasswordCapitalized);
            result.AddRange(partialResultCapitalized);

            String possiblePasswordReverse = StringUtilities.Reverse(dictionaryEntry);
            IEnumerable<UserInfoClearText> partialResultReverse = CheckSingleWord(userInfos, possiblePasswordReverse);
            result.AddRange(partialResultReverse);

            for (int i = 0; i < 100; i++)
            {
                String possiblePasswordEndDigit = dictionaryEntry + i;
                IEnumerable<UserInfoClearText> partialResultEndDigit = CheckSingleWord(userInfos, possiblePasswordEndDigit);
                result.AddRange(partialResultEndDigit);
            }

            for (int i = 0; i < 100; i++)
            {
                String possiblePasswordStartDigit = i + dictionaryEntry;
                IEnumerable<UserInfoClearText> partialResultStartDigit = CheckSingleWord(userInfos, possiblePasswordStartDigit);
                result.AddRange(partialResultStartDigit);
            }

            for (int i = 0; i < 10; i++)
            {
                for (int j = 0; j < 10; j++)
                {
                    String possiblePasswordStartEndDigit = i + dictionaryEntry + j;
                    IEnumerable<UserInfoClearText> partialResultStartEndDigit = CheckSingleWord(userInfos, possiblePasswordStartEndDigit);
                    result.AddRange(partialResultStartEndDigit);
                }
            }

            return result;
        }

        /// <summary>
        /// Checks a single word (or rather a variation of a word): Encrypts and compares to all entries in the password file
        /// </summary>
        /// <param name="userInfos"></param>
        /// <param name="possiblePassword">List of (username, encrypted password) pairs from the password file</param>
        /// <returns>A list of (username, readable password) pairs. The list might be empty</returns>
        private IEnumerable<UserInfoClearText> CheckSingleWord(IEnumerable<UserInfo> userInfos, String possiblePassword)
        {
            HashAlgorithm messageDigest = new SHA1CryptoServiceProvider();
            char[] charArray = possiblePassword.ToCharArray();
            byte[] passwordAsBytes = Array.ConvertAll(charArray, StringUtilities.GetConverter());
            byte[] encryptedPassword = null;
            try
            {
                encryptedPassword = messageDigest.ComputeHash(passwordAsBytes);
            }
            catch (OutOfMemoryException)
            {
                Console.WriteLine("Couldn't handle the load");
            }
            //string encryptedPasswordBase64 = System.Convert.ToBase64String(encryptedPassword);

            List<UserInfoClearText> results = new List<UserInfoClearText>();
            foreach (UserInfo userInfo in userInfos)
            {
                if (CompareBytes(userInfo.EncryptedPassword, encryptedPassword))
                {
                    if (userInfo.Username == null)
                    {
                        throw new ArgumentException("username");
                    }
                    UserInfoClearText clearTextInfo = new UserInfoClearText
                    {
                        UserName = userInfo.Username,
                        Password = possiblePassword
                    };
                    results.Add(clearTextInfo);
                    Console.WriteLine("\nFound: " + userInfo.Username + " " + possiblePassword);
                }
            }
            return results;
        }

        /// <summary>
        /// Compares to byte arrays. Encrypted words are byte arrays
        /// </summary>
        /// <param name="firstArray"></param>
        /// <param name="secondArray"></param>
        /// <returns></returns>
        private static bool CompareBytes(IList<byte> firstArray, IList<byte> secondArray)
        {
            //if (secondArray == null)
            //{
            //    throw new ArgumentNullException("firstArray");
            //}
            //if (secondArray == null)
            //{
            //    throw new ArgumentNullException("secondArray");
            //}
            if (firstArray.Count != secondArray.Count)
            {
                return false;
            }
            for (int i = 0; i < firstArray.Count; i++)
            {
                if (firstArray[i] != secondArray[i])
                    return false;
            }
            return true;
        }

    }
}
