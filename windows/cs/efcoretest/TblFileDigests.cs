using System;
using System.Collections.Generic;

namespace efcoretest
{
    public partial class TblFileDigests
    {
        public Guid UpdateId { get; set; }
        public string FileDigest { get; set; }
    }
}
