namespace ConSight.DONGBO.PlcSimulator.Logic
{
    // ProcessPipelineTestView.xaml.cs 349~537 줄의 빌더 로직 복제
    // DAQ 측 파서 오프셋(Op200Parser 등)과 항상 동기화 유지
    internal static class MockArrayBuilder
    {
        internal static short[] BuildOp200ProcArray()
        {
            var a = new short[100];
            a[0]  = 1;      // BackUp_Start
            a[2]  = 0;      // Repair: AUTO

            EncodeAscii(a, 10, 10, "MODEL-A");
            EncodeAscii(a, 20, 20, "SN-00001");
            EncodeAscii(a, 40, 20, "GR-00001");

            a[60] = 1;      // TotalJudge: OK

            a[61] = 1234;                  // GR_R1_Load   → 12.34
            SetInt32(a, 62, 123456);       // GR_R1_Stroke → 1234.56
            a[64] = 987;                   // GR_R2_Load   → 9.87
            SetInt32(a, 65, 98765);        // GR_R2_Stroke → 987.65
            a[67] = 500;                   // GR_P_Load    → 5.00
            SetInt32(a, 68, 50000);        // GR_P_Stroke  → 500.00
            a[70] = 1;                     // GR_Judge     : OK
            a[71] = 3;                     // GR_IndexNo

            a[72] = 2345;                  // BR_R1_Load   → 23.45
            SetInt32(a, 73, 234567);       // BR_R1_Stroke → 2345.67
            a[75] = 1111;                  // BR_R2_Load   → 11.11
            SetInt32(a, 76, 111100);       // BR_R2_Stroke → 1111.00
            a[78] = 600;                   // BR_P_Load    → 6.00
            SetInt32(a, 79, 60000);        // BR_P_Stroke  → 600.00
            a[81] = 1;                     // BR_Judge     : OK
            a[82] = 2;                     // BR_IndexNo

            SetInt32(a, 83, 12345);        // SR_Groove_000 → 1.2345
            SetInt32(a, 85, 12356);        // SR_Groove_180 → 1.2356
            SetInt32(a, 87, 11000);        // SR_GradeData  → 110.00
            a[89] = 2;                     // SR_Grade
            a[90] = 1;                     // SR_Groove_Judge: OK
            SetInt32(a, 91, 15000);        // SR_Heigh_Thick → 150.00
            a[93] = 1;                     // SR_Heigh_Judge : OK
            a[94] = 1;                     // SR_Judge       : OK

            SetInt32(a, 95, 10050);        // EndPlate_Data → 100.50
            a[97] = 1;                     // EndPlate_Judge: OK

            return a;
        }

        internal static short[] BuildOp200SettingArray()
        {
            var a = new short[100];

            a[0] = 500;  a[1] = 2000;
            SetInt32(a, 2, 50000);  SetInt32(a, 4, 200000);
            a[6] = 400;  a[7] = 1800;
            SetInt32(a, 8, 40000);  SetInt32(a, 10, 180000);
            a[12] = 100; a[13] = 1000;
            SetInt32(a, 14, 10000); SetInt32(a, 16, 100000);

            a[20] = 1000; a[21] = 4000;
            SetInt32(a, 22, 100000); SetInt32(a, 24, 400000);
            a[26] = 800;  a[27] = 3000;
            SetInt32(a, 28, 80000);  SetInt32(a, 30, 300000);
            a[32] = 200;  a[33] = 1500;
            SetInt32(a, 34, 20000);  SetInt32(a, 36, 150000);

            SetInt32(a, 48, 10000); SetInt32(a, 50, 20000);
            SetInt32(a, 52, 12000); SetInt32(a, 54, 18000);

            SetInt32(a, 60, 9500);  SetInt32(a, 62, 11000);

            SetInt32(a, 70, 500);   SetInt32(a, 72, 1500);
            SetInt32(a, 74, 5000);  SetInt32(a, 76, 15000);
            SetInt32(a, 80, 8000);  SetInt32(a, 82, 12000);

            return a;
        }

        internal static short[] BuildOp210ProcArray()
        {
            var a = new short[70];
            a[0] = 1;
            a[2] = 0;
            EncodeAscii(a, 10, 10, "MODEL-A");
            EncodeAscii(a, 20, 20, "SN-00001");

            SetInt32(a, 60, 9800);
            a[62] = 1;
            SetInt32(a, 63, 10200);
            a[65] = 1;
            return a;
        }

        internal static short[] BuildOp220ProcArray()
        {
            var a = new short[70];
            a[0] = 1;
            a[2] = 0;
            EncodeAscii(a, 10, 10, "MODEL-A");
            EncodeAscii(a, 20, 20, "SN-00001");

            a[60] = 1;
            a[61] = (short)9950;
            a[63] = 1;
            return a;
        }

        internal static short[] BuildOp230ProcArray()
        {
            var a = new short[80];
            a[0] = 1;
            a[2] = 0;
            EncodeAscii(a, 10, 10, "MODEL-A");
            EncodeAscii(a, 20, 20, "SN-00001");
            EncodeAscii(a, 40, 20, "GR-00001");

            a[60] = 1;
            a[61] = 1;
            a[62] = 3456;
            SetInt32(a, 63, 34567);
            a[65] = 3200;
            SetInt32(a, 66, 32000);
            a[68] = 1500;
            SetInt32(a, 69, 15000);
            a[71] = 1;
            SetInt32(a, 72, 99990);
            a[74] = 1;
            return a;
        }

        internal static short[] BuildOp230SettingArray()
        {
            var a = new short[24];
            a[0] = 2000; a[1] = 5000;
            SetInt32(a, 2, 20000);  SetInt32(a, 4, 50000);
            a[6] = 1500; a[7] = 4500;
            SetInt32(a, 8, 15000);  SetInt32(a, 10, 45000);
            a[12] = 1000; a[13] = 3000;
            SetInt32(a, 14, 10000); SetInt32(a, 16, 30000);
            SetInt32(a, 18, 80000); SetInt32(a, 20, 120000);
            return a;
        }

        private static void EncodeAscii(short[] arr, int offset, int maxWords, string s)
        {
            int words = Math.Min(maxWords, (s.Length + 1) / 2 + 1);
            for (int i = 0; i < words && offset + i < arr.Length; i++)
            {
                byte lo = (i * 2)     < s.Length ? (byte)s[i * 2]     : (byte)0;
                byte hi = (i * 2 + 1) < s.Length ? (byte)s[i * 2 + 1] : (byte)0;
                arr[offset + i] = (short)(lo | (hi << 8));
            }
        }

        private static void SetInt32(short[] arr, int offset, int value)
        {
            arr[offset]     = (short)(value & 0xFFFF);
            arr[offset + 1] = (short)((value >> 16) & 0xFFFF);
        }
    }
}
