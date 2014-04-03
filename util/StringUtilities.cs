using System;
using System.Linq;
using System.Text;

namespace PasswordCrackerServiceConsumer.util
{
    class StringUtilities
    {
        private static readonly Converter<char, byte> Converter = CharToByte;
        public static String Capitalize(String str)
        {
            if (str == null)
            {
                throw new ArgumentNullException("str");
            }
            if (str.Trim().Length == 0)
            {
                return str;
            }
            String firstLetterUppercase = str.Substring(0, 1).ToUpper();
            String theRest = str.Substring(1);
            return firstLetterUppercase + theRest;
        }

        public static String Reverse(String str)
        {
            if (str == null)
            {
                throw new ArgumentNullException("str");
            }
            if (str.Trim().Length == 0)
            {
                return str;
            }
            StringBuilder reverseString = new StringBuilder();
            for (int i = 0; i < str.Length; i++)
            {
                reverseString.Append(str.ElementAt(str.Length - 1 - i));
            }
            return reverseString.ToString();
        }

        private static byte CharToByte(char ch)
        {
            return Convert.ToByte(ch);
        }

        public static Converter<char, byte> GetConverter()
        {
            return Converter;
        }
    }
}
