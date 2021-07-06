using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FileTail
{
    class AllText
    {
        // hande ALL.TXT file from WSJTX
        // 210620_132945    14.074 Rx FT8    -18  0.9 1841 BG6VBM K9MK R-07
        public string Callsign(string line)
        {
            try
            {
                char[] term = { ' ' };
                var tokens = line.Split(term, StringSplitOptions.RemoveEmptyEntries);
                if (tokens.Count() >= 9)
                {
                    if (tokens[7].Equals("CQ") && tokens[8].Length == 2) // directed CQ
                        return tokens[9];
                    else
                        return tokens[8];
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message + "\n" + ex.StackTrace);
            }
            return "";
        }
    }
}
