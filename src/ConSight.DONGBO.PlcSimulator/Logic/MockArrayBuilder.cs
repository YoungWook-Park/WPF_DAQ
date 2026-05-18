namespace ConSight.DONGBO.PlcSimulator.Logic
{
    // ProcessPipelineTestView.xaml.cs 349~537 줄의 빌더 로직 복제
    // DAQ 측 파서 오프셋(Op200Parser 등)과 항상 동기화 유지
    internal static class MockArrayBuilder
    {
        internal static short[] BuildOp200ProcArray()
        {
            var data = new short[100];
            data[0]  = 1;      // BackUp_Start
            data[2]  = 0;      // Repair: AUTO

            EncodeAscii(data, 10, 10, "MODEL-A");
            EncodeAscii(data, 20, 20, "SN-00001");
            EncodeAscii(data, 40, 20, "GR-00001");

            data[60] = 1;      // TotalJudge: OK

            data[61] = 1234;                  // GR_R1_Load   → 12.34
            SetInt32(data, 62, 123456);       // GR_R1_Stroke → 1234.56
            data[64] = 987;                   // GR_R2_Load   → 9.87
            SetInt32(data, 65, 98765);        // GR_R2_Stroke → 987.65
            data[67] = 500;                   // GR_P_Load    → 5.00
            SetInt32(data, 68, 50000);        // GR_P_Stroke  → 500.00
            data[70] = 1;                     // GR_Judge     : OK
            data[71] = 3;                     // GR_IndexNo

            data[72] = 2345;                  // BR_R1_Load   → 23.45
            SetInt32(data, 73, 234567);       // BR_R1_Stroke → 2345.67
            data[75] = 1111;                  // BR_R2_Load   → 11.11
            SetInt32(data, 76, 111100);       // BR_R2_Stroke → 1111.00
            data[78] = 600;                   // BR_P_Load    → 6.00
            SetInt32(data, 79, 60000);        // BR_P_Stroke  → 600.00
            data[81] = 1;                     // BR_Judge     : OK
            data[82] = 2;                     // BR_IndexNo

            SetInt32(data, 83, 12345);        // SR_Groove_000 → 1.2345
            SetInt32(data, 85, 12356);        // SR_Groove_180 → 1.2356
            SetInt32(data, 87, 11000);        // SR_GradeData  → 110.00
            data[89] = 2;                     // SR_Grade
            data[90] = 1;                     // SR_Groove_Judge: OK
            SetInt32(data, 91, 15000);        // SR_Heigh_Thick → 150.00
            data[93] = 1;                     // SR_Heigh_Judge : OK
            data[94] = 1;                     // SR_Judge       : OK

            SetInt32(data, 95, 10050);        // EndPlate_Data → 100.50
            data[97] = 1;                     // EndPlate_Judge: OK

            return data;
        }

        internal static short[] BuildOp200SettingArray()
        {
            var data = new short[100];

            data[0] = 500;  data[1] = 2000;
            SetInt32(data, 2, 50000);  SetInt32(data, 4, 200000);
            data[6] = 400;  data[7] = 1800;
            SetInt32(data, 8, 40000);  SetInt32(data, 10, 180000);
            data[12] = 100; data[13] = 1000;
            SetInt32(data, 14, 10000); SetInt32(data, 16, 100000);

            data[20] = 1000; data[21] = 4000;
            SetInt32(data, 22, 100000); SetInt32(data, 24, 400000);
            data[26] = 800;  data[27] = 3000;
            SetInt32(data, 28, 80000);  SetInt32(data, 30, 300000);
            data[32] = 200;  data[33] = 1500;
            SetInt32(data, 34, 20000);  SetInt32(data, 36, 150000);

            SetInt32(data, 48, 10000); SetInt32(data, 50, 20000);
            SetInt32(data, 52, 12000); SetInt32(data, 54, 18000);

            SetInt32(data, 60, 9500);  SetInt32(data, 62, 11000);

            SetInt32(data, 70, 500);   SetInt32(data, 72, 1500);
            SetInt32(data, 74, 5000);  SetInt32(data, 76, 15000);
            SetInt32(data, 80, 8000);  SetInt32(data, 82, 12000);

            return data;
        }

        internal static short[] BuildOp210ProcArray()
        {
            var data = new short[70];
            data[0] = 1;
            data[2] = 0;
            EncodeAscii(data, 10, 10, "MODEL-A");
            EncodeAscii(data, 20, 20, "SN-00001");

            SetInt32(data, 60, 9800);
            data[62] = 1;
            SetInt32(data, 63, 10200);
            data[65] = 1;
            return data;
        }

        internal static short[] BuildOp220ProcArray()
        {
            var data = new short[70];
            data[0] = 1;
            data[2] = 0;
            EncodeAscii(data, 10, 10, "MODEL-A");
            EncodeAscii(data, 20, 20, "SN-00001");

            data[60] = 1;
            data[61] = (short)9950;
            data[63] = 1;
            return data;
        }

        internal static short[] BuildOp230ProcArray()
        {
            var data = new short[80];
            data[0] = 1;
            data[2] = 0;
            EncodeAscii(data, 10, 10, "MODEL-A");
            EncodeAscii(data, 20, 20, "SN-00001");
            EncodeAscii(data, 40, 20, "GR-00001");

            data[60] = 1;
            data[61] = 1;
            data[62] = 3456;
            SetInt32(data, 63, 34567);
            data[65] = 3200;
            SetInt32(data, 66, 32000);
            data[68] = 1500;
            SetInt32(data, 69, 15000);
            data[71] = 1;
            SetInt32(data, 72, 99990);
            data[74] = 1;
            return data;
        }

        internal static short[] BuildOp230SettingArray()
        {
            var data = new short[24];
            data[0] = 2000; data[1] = 5000;
            SetInt32(data, 2, 20000);  SetInt32(data, 4, 50000);
            data[6] = 1500; data[7] = 4500;
            SetInt32(data, 8, 15000);  SetInt32(data, 10, 45000);
            data[12] = 1000; data[13] = 3000;
            SetInt32(data, 14, 10000); SetInt32(data, 16, 30000);
            SetInt32(data, 18, 80000); SetInt32(data, 20, 120000);
            return data;
        }

        private static void EncodeAscii(short[] array, int offset, int maxWords, string text)
        {
            int wordCount = Math.Min(maxWords, (text.Length + 1) / 2 + 1);
            for (int i = 0; i < wordCount && offset + i < array.Length; i++)
            {
                byte lowByte  = (i * 2)     < text.Length ? (byte)text[i * 2]     : (byte)0;
                byte highByte = (i * 2 + 1) < text.Length ? (byte)text[i * 2 + 1] : (byte)0;
                array[offset + i] = (short)(lowByte | (highByte << 8));
            }
        }

        private static void SetInt32(short[] array, int offset, int value)
        {
            array[offset]     = (short)(value & 0xFFFF);
            array[offset + 1] = (short)((value >> 16) & 0xFFFF);
        }
    }
}
