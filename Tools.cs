using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServiceReportProgram
{
    internal class Tools
    {
        public static string Splitter(string source, string start, string end)
        {
            int startIndex = source.IndexOf(start);
            if (startIndex == -1)
            {
                // Jos aloitusmerkkijonoa ei löydy, palautetaan tyhjä merkkijono
                return string.Empty;
            }
            startIndex += start.Length;

            int endIndex = source.IndexOf(end, startIndex);
            if (endIndex == -1)
            {
                // Jos lopetusmerkkijonoa ei löydy, palautetaan tyhjä merkkijono
                return string.Empty;
            }

            return source.Substring(startIndex, endIndex - startIndex);
        }

    }
}
