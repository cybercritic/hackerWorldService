using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Security.Cryptography;

namespace hackerWorldService
{
    public class Crypto
    {
        public const string passwordSalt = ".passwordHashSalt";

        /// <summary>
        /// computes sha1 hash from given string
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public string getSHA1hash(string input)
        {
            string result = "";

            input += passwordSalt;
            byte[] data = new byte[input.Length];

            int i = 0;
            foreach (char ch in input)
            {
                data[i] = (byte)ch;
                i++;
            }

            SHA1 sha = new SHA1CryptoServiceProvider();
            byte[] hash = sha.ComputeHash(data);

            foreach (byte by in hash)
                result += showHex(by);

            return result;
        }

        /// <summary>
        /// shows hex of given byte
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public string showHex(byte input)
        {
            string result = "";
            int a = input >> 4;
            int b = input & 15;
            if (a < 10)
                result += a.ToString();
            else
                result += ((hex)a).ToString();

            if (b < 10)
                result += b.ToString();
            else
                result += ((hex)b).ToString();


            return result;
        }
    }
}