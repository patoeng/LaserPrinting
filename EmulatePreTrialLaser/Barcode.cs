using System;
using System.Linq;

namespace EmulatePreTrialLaser
{
    public class Barcode
    {
        public Barcode()
        {
            Id = Guid.NewGuid();
        }

        public Barcode(string barcode)
        {
            MachineSerialNumber = barcode;
            Id = Guid.NewGuid();
        }
        public Guid Id { get; set; }
        public string MachineSerialNumber { get; set; }

        public bool LengthIsValid => MachineSerialNumber.Length == 19;
        public bool BrandCodeValid { get; protected set; }
        public bool LineIdValid { get; protected set; }
        public bool YearStringValid { get; protected set; }
        public bool MonthCodeValid { get; protected set; }
        public bool DayStringValid { get; protected set; }
        public bool CounterValid { get; protected set; }
        public bool IsValid { get; protected set; }
        public bool ColorValid { get; protected set; }

        public bool CheckSumValid { get; protected set; }

        public string PlugCode => LengthIsValid ? MachineSerialNumber[15].ToString() : "";
        public string VoltageCode => LengthIsValid ? MachineSerialNumber[14].ToString() : "";
        public string PartnerCode => LengthIsValid ? MachineSerialNumber[13].ToString() : "";
        public string LineId => LengthIsValid ? MachineSerialNumber[8].ToString() : "";
        public string YearString => LengthIsValid ? MachineSerialNumber.Substring(0, 2) : "00";
        public string MonthCode => LengthIsValid ? MachineSerialNumber[2].ToString() : "";
        public string DayString => LengthIsValid ? MachineSerialNumber.Substring(3, 2) : "00";
        public string BrandCode => LengthIsValid ? MachineSerialNumber.Substring(5, 3) : "000";
        public string Color => LengthIsValid ? MachineSerialNumber[16].ToString() : "";
        public string CheckSum => LengthIsValid ? MachineSerialNumber[18].ToString() : "";

        public PartnerCountry PartnerCountry()
        {
            switch (PlugCode)
            {
                case "A":
                    return EmulatePreTrialLaser.PartnerCountry.Swiss;
                case "1":
                case "H":
                    return EmulatePreTrialLaser.PartnerCountry.Brazil;
            }

            return EmulatePreTrialLaser.PartnerCountry.Other;
        }

        public bool Validate(string lineId, string brandCode)
        {
            if (!LengthIsValid) return false;
            BrandCodeValid = (brandCode == BrandCode) && "BS1".Contains(BrandCode[0]) && "35789".Contains(BrandCode[1]) && "01234567A".Contains(BrandCode[2]) ;
            LineIdValid = (lineId == LineId);
            int day;
            var b =int.TryParse(DayString, out day);
            DayStringValid = b && (day >0 && day< 32);

            var c = "0123456789ND".IndexOf(MonthCode, StringComparison.Ordinal);
            MonthCodeValid=  c >= 0;

            var year = DateTime.Now.Year;
            year %= 100;
            b = int.TryParse(YearString, out day);

            YearStringValid = b && (day <= year);

            var s = MachineSerialNumber.Substring(9,4);
            b = int.TryParse(s, out day);
            CounterValid = b && day > 0;

            ColorValid = "01".Contains(Color);

            CheckSumValid = CheckSum[0] == CheckSumCalc(MachineSerialNumber);

            IsValid = BrandCodeValid && LineIdValid && DayStringValid && MonthCodeValid && YearStringValid && CounterValid && ColorValid && CheckSumValid;
            return IsValid;
        }

        public static char CheckSumCalc(string barcode)
        {
            if (barcode.Length < 18) return '\0';
            
            var total = 0;
            for (int i =0;i<18;i++)
            {
                var getIndex = Diction.IndexOf(barcode[i]);
                total += getIndex;
            }

            var indexCs = total % 58;
            var getChar = Diction[indexCs];
            return getChar;
        }

        public static readonly string Diction = "0123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz";
    }
}
