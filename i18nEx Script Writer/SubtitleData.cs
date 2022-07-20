using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace i18nEx_Script_Writer
{
    internal class SubtitleData
    {
        public int addDisplayTime { get; set; }
        public int displayTime { get; set; }
        public bool isCasino { get; set; }
        public string? original { get; set; }
        public int startTime { get; set; }
        public string? translation { get; set; }
        public string? voice { get; set; }
    }
}
