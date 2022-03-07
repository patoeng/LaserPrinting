using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaserPrinting.Model
{
    public class LaserPrintingProduct
    {
        public string Barcode { get; set; }
        public DateTime PrintedEndDateTime { get; set; }
        public DateTime PrintedStartDateTime { get; set; }
        public int MarkCount { get; set; }
        public Guid DatalogFileId { get; set; }
    }
}
