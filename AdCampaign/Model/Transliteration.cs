using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BinaryAnalysis.UnidecodeSharp;

namespace AdCampaign.Model
{
    public static class Transliteration
    {
        public static string Front(string text)
        {
            return text.Unidecode();
        }
       
        public static bool IsTranslit(string text)
        {
            foreach (Char ch in text)
            {
                if (ch > 127) return false;
            }
            return true;
        }
    }
}
