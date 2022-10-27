using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace MGUI.Shared.Helpers
{
    public static class StringUtils
    {
        //Taken from: https://stackoverflow.com/a/2776689/11689514
        public static string Truncate(this string value, int maxLength, string truncationSuffix = "…")
        {
            return value?.Length > maxLength
                ? value.Substring(0, maxLength) + truncationSuffix
                : value;
        }

        /// <summary>Splits this string by the given <paramref name="delimiters"/>, while keeping the delimited characters in the result set.<para/>
        /// <code>"Hello   World ".SplitAndKeepDelimiters(' ')<br/>[ "Hello", " ", " ", " ", "World", " " ]</code></summary>
        public static IEnumerable<string> SplitAndKeepDelimiters(this string s, IEnumerable<char> delimiters)
            => s.SplitAndKeepDelimiters(delimiters.ToArray());

        //Taken from: https://stackoverflow.com/a/3143036/11689514
        /// <summary>Splits this string by the given <paramref name="delimiters"/>, while keeping the delimited characters in the result set.<para/>
        /// <code>"Hello   World ".SplitAndKeepDelimiters(' ')<br/>[ "Hello", " ", " ", " ", "World", " " ]</code></summary>
        public static IEnumerable<string> SplitAndKeepDelimiters(this string s, params char[] delimiters)
        {
            int start = 0, index;

            while ((index = s.IndexOfAny(delimiters, start)) != -1)
            {
                if (index - start > 0)
                    yield return s.Substring(start, index - start);
                yield return s.Substring(index, 1);
                start = index + 1;
            }

            if (start < s.Length)
            {
                yield return s.Substring(start);
            }
        }

        //Taken from: https://stackoverflow.com/a/8809437/11689514
        public static string ReplaceFirstOccurrence(this string text, string search, string replace)
        {
            int pos = text.IndexOf(search);
            if (pos < 0)
            {
                return text;
            }
            return text.Substring(0, pos) + replace + text.Substring(pos + search.Length);
        }

        //Taken from: https://stackoverflow.com/a/14826068/11689514
        public static string ReplaceLastOccurrence(this string Source, string Find, string Replace)
        {
            int place = Source.LastIndexOf(Find);

            if (place == -1)
                return Source;

            string result = Source.Remove(place, Find.Length).Insert(place, Replace);
            return result;
        }

        #region SecureString
        //Adapted from: https://stackoverflow.com/a/38016279/11689514

        // convert a secure string into a normal plain text string
        public static string AsPlainString(this SecureString secureStr)
        {
            string plainStr = new NetworkCredential(string.Empty, secureStr).Password;
            return plainStr;
        }

        // convert a plain text string into a secure string
        public static SecureString AsSecureString(this string plainStr)
        {
            var secStr = new SecureString(); 
            secStr.Clear();
            foreach (char c in plainStr.ToCharArray())
            {
                secStr.AppendChar(c);
            }
            return secStr;
        }
        #endregion SecureString
    }
}
