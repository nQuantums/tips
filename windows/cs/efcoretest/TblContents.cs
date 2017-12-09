using System;
using System.Collections.Generic;

namespace efcoretest
{
    public partial class TblContents
    {
        public int ContentsId { get; set; }
        public string Contents { get; set; }
        public string[] Tags { get; set; }
    }
}
