using System.Windows.Media;

namespace ConSight.DAQ.Views
{
    public class ResourceLotHisItem
    {
        public int    No                { get; set; }
        public string Date_Time         { get; set; } = string.Empty;
        public string Repair            { get; set; } = string.Empty;
        public string Model             { get; set; } = string.Empty;
        public string Material01_Serial { get; set; } = string.Empty;
        public string Material02_Serial { get; set; } = string.Empty;
        public string TotalJudge        { get; set; } = string.Empty;

        public string APD01 { get; set; } = string.Empty;
        public string APD02 { get; set; } = string.Empty;
        public string APD03 { get; set; } = string.Empty;
        public string APD04 { get; set; } = string.Empty;
        public string APD05 { get; set; } = string.Empty;
        public string APD06 { get; set; } = string.Empty;
        public string APD07 { get; set; } = string.Empty;
        public string APD08 { get; set; } = string.Empty;
        public string APD09 { get; set; } = string.Empty;
        public string APD10 { get; set; } = string.Empty;
        public string APD11 { get; set; } = string.Empty;
        public string APD12 { get; set; } = string.Empty;
        public string APD13 { get; set; } = string.Empty;
        public string APD14 { get; set; } = string.Empty;
        public string APD15 { get; set; } = string.Empty;
        public string APD16 { get; set; } = string.Empty;
        public string APD17 { get; set; } = string.Empty;
        public string APD18 { get; set; } = string.Empty;
        public string APD19 { get; set; } = string.Empty;
        public string APD20 { get; set; } = string.Empty;
        public string APD21 { get; set; } = string.Empty;
        public string APD22 { get; set; } = string.Empty;
        public string APD23 { get; set; } = string.Empty;
        public string APD24 { get; set; } = string.Empty;
        public string APD25 { get; set; } = string.Empty;
        public string APD26 { get; set; } = string.Empty;
        public string APD27 { get; set; } = string.Empty;
        public string APD28 { get; set; } = string.Empty;
        public string APD29 { get; set; } = string.Empty;
        public string APD30 { get; set; } = string.Empty;
        public string APD31 { get; set; } = string.Empty;
        public string APD32 { get; set; } = string.Empty;
        public string APD33 { get; set; } = string.Empty;
        public string APD34 { get; set; } = string.Empty;
        public string APD35 { get; set; } = string.Empty;
        public string APD36 { get; set; } = string.Empty;
        public string APD37 { get; set; } = string.Empty;
        public string APD38 { get; set; } = string.Empty;
        public string APD39 { get; set; } = string.Empty;
        public string APD40 { get; set; } = string.Empty;
        public string APD41 { get; set; } = string.Empty;
        public string APD42 { get; set; } = string.Empty;
        public string APD43 { get; set; } = string.Empty;
        public string APD44 { get; set; } = string.Empty;

        public string SP01 { get; set; } = string.Empty;
        public string SP02 { get; set; } = string.Empty;
        public string SP03 { get; set; } = string.Empty;
        public string SP04 { get; set; } = string.Empty;
        public string SP05 { get; set; } = string.Empty;
        public string SP06 { get; set; } = string.Empty;
        public string SP07 { get; set; } = string.Empty;
        public string SP08 { get; set; } = string.Empty;
        public string SP09 { get; set; } = string.Empty;
        public string SP10 { get; set; } = string.Empty;
        public string SP11 { get; set; } = string.Empty;
        public string SP12 { get; set; } = string.Empty;
        public string SP13 { get; set; } = string.Empty;
        public string SP14 { get; set; } = string.Empty;
        public string SP15 { get; set; } = string.Empty;
        public string SP16 { get; set; } = string.Empty;
        public string SP17 { get; set; } = string.Empty;
        public string SP18 { get; set; } = string.Empty;
        public string SP19 { get; set; } = string.Empty;
        public string SP20 { get; set; } = string.Empty;
        public string SP21 { get; set; } = string.Empty;
        public string SP22 { get; set; } = string.Empty;
        public string SP23 { get; set; } = string.Empty;
        public string SP24 { get; set; } = string.Empty;
        public string SP25 { get; set; } = string.Empty;
        public string SP26 { get; set; } = string.Empty;
        public string SP27 { get; set; } = string.Empty;
        public string SP28 { get; set; } = string.Empty;
        public string SP29 { get; set; } = string.Empty;
        public string SP30 { get; set; } = string.Empty;
        public string SP31 { get; set; } = string.Empty;
        public string SP32 { get; set; } = string.Empty;
        public string SP33 { get; set; } = string.Empty;
        public string SP34 { get; set; } = string.Empty;
        public string SP35 { get; set; } = string.Empty;
        public string SP36 { get; set; } = string.Empty;
        public string SP37 { get; set; } = string.Empty;
        public string SP38 { get; set; } = string.Empty;
        public string SP39 { get; set; } = string.Empty;
        public string SP40 { get; set; } = string.Empty;
        public string SP41 { get; set; } = string.Empty;
        public string SP42 { get; set; } = string.Empty;
        public string SP43 { get; set; } = string.Empty;
        public string SP44 { get; set; } = string.Empty;
        public string SP45 { get; set; } = string.Empty;
        public string SP46 { get; set; } = string.Empty;
        public string SP47 { get; set; } = string.Empty;
        public string SP48 { get; set; } = string.Empty;
        public string SP49 { get; set; } = string.Empty;
        public string SP50 { get; set; } = string.Empty;

        // Cell background: Orange when measured value is outside [min, max] spec
        public Brush APD01_CELL_BG => CellBg(APD01, SP01, SP02);
        public Brush APD02_CELL_BG => CellBg(APD02, SP03, SP04);
        public Brush APD03_CELL_BG => CellBg(APD03, SP05, SP06);
        public Brush APD04_CELL_BG => CellBg(APD04, SP07, SP08);
        public Brush APD05_CELL_BG => CellBg(APD05, SP09, SP10);
        public Brush APD06_CELL_BG => CellBg(APD06, SP11, SP12);
        public Brush APD09_CELL_BG => CellBg(APD09, SP13, SP14);
        public Brush APD10_CELL_BG => CellBg(APD10, SP15, SP16);
        public Brush APD11_CELL_BG => CellBg(APD11, SP17, SP18);
        public Brush APD12_CELL_BG => CellBg(APD12, SP19, SP20);
        public Brush APD13_CELL_BG => CellBg(APD13, SP21, SP22);
        public Brush APD14_CELL_BG => CellBg(APD14, SP23, SP24);

        private static Brush CellBg(string value, string minStr, string maxStr)
        {
            if (double.TryParse(value, out double val) &&
                double.TryParse(minStr, out double min) &&
                double.TryParse(maxStr, out double max))
            {
                if (val < min || val > max)
                    return Brushes.Orange;
            }
            return Brushes.White;
        }
    }
}
