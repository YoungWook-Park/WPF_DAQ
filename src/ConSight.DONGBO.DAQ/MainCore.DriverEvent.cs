using ConSight.DAQ.Device.DB;

namespace ConSight.DONGBO.DAQ;

// PLC 드라이버 이벤트 및 EventBus 구독 처리
// MxComponent 기반 원본과 달리 TcpPlcDriver 는 자동 재접속 방식이므로
// 여기서는 공정 완료 이벤트 수신 → 로그 기록을 담당한다.
public sealed partial class MainCore
{
    private void SubscribeDriverEvents()
    {
        EventBus.Subscribe(cFunc_EventBus_ProcessCompleted);

        Log.WriteInformation(
            $"Location={nameof(MainCore)}, Function={nameof(SubscribeDriverEvents)}, Action=이벤트 구독 등록");
    }

    // ── EventBus 콜백 ─────────────────────────────────────────────────────

    private void cFunc_EventBus_ProcessCompleted(EmpgRow row)
    {
        Log.WriteInformation(
            $"Location={nameof(MainCore)}, Function={nameof(cFunc_EventBus_ProcessCompleted)}, " +
            $"Action=공정완료  Model={row.Model}  Serial={row.MatSerial01}  판정={row.TotalJudge}");
    }
}
