using System;
using System.Collections.Generic;

namespace efcoretest
{
    public partial class TblUpdates
    {
        public Guid UpdateId { get; set; }
        public int RevisionNumber { get; set; }
        public int? RevisionId { get; set; }
        public string Title { get; set; }
        public string Xml { get; set; }
        public Guid[] Classifications { get; set; }
        public Guid[] Categories { get; set; }
        public string[] FileDigests { get; set; }
    }
}
