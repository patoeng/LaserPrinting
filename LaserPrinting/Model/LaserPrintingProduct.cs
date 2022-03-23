using System;
using System.Globalization;
using MesData.LaserMarking;

namespace LaserPrinting.Model
{
    public class LaserPrintingProduct
    {
        public LaserPrintingProduct(LaserMarkingData data)
        {
            Barcode = data.Barcode.Value;
            ArticleNumber = data.ArticleNumber.Value;
            LaserMarkingData = data;
            DateTime.TryParseExact(data.PrintedDateTime.Value, LaserMarkingDataConfig.DateTimeStringFormat,
                CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime tempDateTime);
            PrintedDateTime = tempDateTime;
        }
        public string Barcode { get; protected set; }
        public DateTime PrintedDateTime { get; protected set; }
        public LaserMarkingData LaserMarkingData { get; protected set; }
        public string ArticleNumber { get; protected set; }
    }
}
