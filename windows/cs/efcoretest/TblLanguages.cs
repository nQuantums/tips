using System;
using System.Collections.Generic;

namespace efcoretest
{
    public partial class TblLanguages
    {
        public int LanguageId { get; set; }
        public string ShortLanguage { get; set; }
        public string LongLanguage { get; set; }
        public bool? Enabled { get; set; }
    }
}
