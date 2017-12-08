using System;
using System.Collections.Generic;

namespace efcoretest
{
    public partial class TblFiles
    {
        public string Digest { get; set; }
        public string Name { get; set; }
        public string ContentPath { get; set; }
        public string Muurl { get; set; }
        public string DecryptionKey { get; set; }
    }
}
