// Phase B: PLC 파서 공용 헬퍼
//
// 파서 클래스들이 공유하는 정적 변환 메서드 모음.
// PLC short[] 배열 → 도메인 문자열 변환 규칙을 한 곳에서 관리한다.

using Bi.ConSight_MxComponent.Data;

namespace ConSight.DAQ.Device
{
    internal static class PlcParseHelper
    {
        // ── Repair 코드 ──────────────────────────────────────────────────────
        // 0 = AUTO, 1 = REPAIR, 2 = MASTER, else → 숫자 문자열
        internal static string Repair(short val) => val switch
        {
            0 => MxComp_DB_REPAIR_CODE.AUTO,
            1 => MxComp_DB_REPAIR_CODE.REPAIR,
            2 => MxComp_DB_REPAIR_CODE.MASTER,
            _ => val.ToString()
        };

        // ── 판정 코드 ────────────────────────────────────────────────────────
        // 1 = OK, 2 = NG, 4 = PASS, else → 숫자 문자열
        internal static string Judge(short val) => val switch
        {
            1 => MxComp_DB_JUDGE_CODE.OK,
            2 => MxComp_DB_JUDGE_CODE.NG,
            4 => MxComp_DB_JUDGE_CODE.PASS,
            _ => val.ToString()
        };

        // ── 실수 변환 — 1워드 / 소수점 2자리 (/100) ─────────────────────────
        // 예: 1234 → "12.34"
        internal static string F2(short[] d, int offset)
            => ((double)d[offset] / 100).ToString("0.00");

        // ── 실수 변환 — 2워드 Int32 / 소수점 2자리 (/100) ───────────────────
        // 예: 12345 → "123.45"
        internal static string F2Int(short[] d, int offset)
            => (PlcDataConverter.shortToInt(d, offset) / 100.0).ToString("0.00");

        // ── 실수 변환 — 2워드 Int32 / 소수점 4자리 (/10000) ─────────────────
        // 예: 12345 → "1.2345"
        internal static string F4Int(short[] d, int offset)
            => (PlcDataConverter.shortToInt(d, offset) / 10000.0).ToString("0.0000");

        // ── 시리얼 문자열 (공백 → 빈 문자열로 정규화) ────────────────────────
        internal static string Serial(short[] d, int offset, int length)
        {
            string s = PlcDataConverter.ShortToString(d, offset, length);
            return string.IsNullOrWhiteSpace(s) ? string.Empty : s;
        }
    }
}
