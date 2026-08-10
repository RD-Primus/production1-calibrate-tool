using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CMATestVer1
{
    public class TestResultExcel
    {
        public int No { get; set; }
        public string RelayLED { get; set; } = "";
        public string WL { get; set; } = "";
        public string WL_AL2 { get; set; } = "";
        public string HP { get; set; } = "";
        public string HP_AL2 { get; set; } = "";
        public string Alarm1 { get; set; } = "";
        public string hotFAN { get; set; } = "";
        public string coolFAN { get; set; } = "";
        public string Compressor { get; set; } = "";
        public string Ohm200 { get; set; } = "";
        public string Ohm2000 { get; set; } = "";
        public string Ohm8000 { get; set; } = "";
        public string test20 { get; set; } = "";
        public string test30 { get; set; } = "";
        public string test40 { get; set; } = "";
        public string test50 { get; set; } = "";
        public string BtnFunction { get; set; } = "-";
        public string BtnDown { get; set; } = "-";
        public string BtnUp { get; set; } = "-";
        public string LedCheck { get; set; } = "-"; 
        public string Status { get; set; } = "";
        public string LOT { get; set; } = string.Empty;
        public string SerialNumber { get; set; } = "";
    }
}
