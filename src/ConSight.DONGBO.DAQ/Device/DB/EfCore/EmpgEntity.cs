// EMPG 테이블 EF Core 엔티티 (EMPG / EMPG_HIS 공통 스키마)
// EmpgHisEntity는 동일 프로퍼티, 테이블명만 EMPG_HIS로 오버라이드 (DongBoDbContext 참조)
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ConSight.DAQ.Device.DB.EfCore
{
    [Table("EMPG")]
    public class EmpgEntity
    {
        [Key]
        [Column("RESULT_ID")]
        public string ResultId { get; set; } = string.Empty;

        [Column("UPDATE_TIME")]
        public DateTime UpdateTime { get; set; }

        [Column("REPAIR")]        public string? Repair        { get; set; }
        [Column("MODEL")]         public string? Model         { get; set; }
        [Column("TOTAL_JUDGE")]   public string? TotalJudge    { get; set; }
        [Column("MAT_SERIAL01")]  public string? MatSerial01   { get; set; }
        [Column("MAT_SERIAL02")]  public string? MatSerial02   { get; set; }
        [Column("CREATE_DAYTIME")]public DateTime? CreateDaytime{ get; set; }
        [Column("OP200_TOTAL_JUDGE")] public string? Op200TotalJudge { get; set; }

        [Column("APD01")]  public string? Apd01  { get; set; }
        [Column("APD02")]  public string? Apd02  { get; set; }
        [Column("APD03")]  public string? Apd03  { get; set; }
        [Column("APD04")]  public string? Apd04  { get; set; }
        [Column("APD05")]  public string? Apd05  { get; set; }
        [Column("APD06")]  public string? Apd06  { get; set; }
        [Column("APD07")]  public string? Apd07  { get; set; }
        [Column("APD08")]  public string? Apd08  { get; set; }
        [Column("APD09")]  public string? Apd09  { get; set; }
        [Column("APD10")]  public string? Apd10  { get; set; }
        [Column("APD11")]  public string? Apd11  { get; set; }
        [Column("APD12")]  public string? Apd12  { get; set; }
        [Column("APD13")]  public string? Apd13  { get; set; }
        [Column("APD14")]  public string? Apd14  { get; set; }
        [Column("APD15")]  public string? Apd15  { get; set; }
        [Column("APD16")]  public string? Apd16  { get; set; }
        [Column("APD17")]  public string? Apd17  { get; set; }
        [Column("APD18")]  public string? Apd18  { get; set; }
        [Column("APD19")]  public string? Apd19  { get; set; }
        [Column("APD20")]  public string? Apd20  { get; set; }
        [Column("APD21")]  public string? Apd21  { get; set; }
        [Column("APD22")]  public string? Apd22  { get; set; }
        [Column("APD23")]  public string? Apd23  { get; set; }
        [Column("APD24")]  public string? Apd24  { get; set; }
        [Column("APD25")]  public string? Apd25  { get; set; }
        [Column("APD26")]  public string? Apd26  { get; set; }
        [Column("APD27")]  public string? Apd27  { get; set; }
        [Column("APD28")]  public string? Apd28  { get; set; }
        [Column("APD29")]  public string? Apd29  { get; set; }
        [Column("APD30")]  public string? Apd30  { get; set; }
        [Column("APD31")]  public string? Apd31  { get; set; }
        [Column("APD32")]  public string? Apd32  { get; set; }
        [Column("APD33")]  public string? Apd33  { get; set; }
        [Column("APD34")]  public string? Apd34  { get; set; }
        [Column("APD35")]  public string? Apd35  { get; set; }
        [Column("APD36")]  public string? Apd36  { get; set; }
        [Column("APD37")]  public string? Apd37  { get; set; }
        [Column("APD38")]  public string? Apd38  { get; set; }
        [Column("APD39")]  public string? Apd39  { get; set; }
        [Column("APD40")]  public string? Apd40  { get; set; }
        [Column("APD41")]  public string? Apd41  { get; set; }
        [Column("APD42")]  public string? Apd42  { get; set; }
        [Column("APD43")]  public string? Apd43  { get; set; }
        [Column("APD44")]  public string? Apd44  { get; set; }

        [Column("SP01")]   public string? Sp01   { get; set; }
        [Column("SP02")]   public string? Sp02   { get; set; }
        [Column("SP03")]   public string? Sp03   { get; set; }
        [Column("SP04")]   public string? Sp04   { get; set; }
        [Column("SP05")]   public string? Sp05   { get; set; }
        [Column("SP06")]   public string? Sp06   { get; set; }
        [Column("SP07")]   public string? Sp07   { get; set; }
        [Column("SP08")]   public string? Sp08   { get; set; }
        [Column("SP09")]   public string? Sp09   { get; set; }
        [Column("SP10")]   public string? Sp10   { get; set; }
        [Column("SP11")]   public string? Sp11   { get; set; }
        [Column("SP12")]   public string? Sp12   { get; set; }
        [Column("SP13")]   public string? Sp13   { get; set; }
        [Column("SP14")]   public string? Sp14   { get; set; }
        [Column("SP15")]   public string? Sp15   { get; set; }
        [Column("SP16")]   public string? Sp16   { get; set; }
        [Column("SP17")]   public string? Sp17   { get; set; }
        [Column("SP18")]   public string? Sp18   { get; set; }
        [Column("SP19")]   public string? Sp19   { get; set; }
        [Column("SP20")]   public string? Sp20   { get; set; }
        [Column("SP21")]   public string? Sp21   { get; set; }
        [Column("SP22")]   public string? Sp22   { get; set; }
        [Column("SP23")]   public string? Sp23   { get; set; }
        [Column("SP24")]   public string? Sp24   { get; set; }
        [Column("SP25")]   public string? Sp25   { get; set; }
        [Column("SP26")]   public string? Sp26   { get; set; }
        [Column("SP27")]   public string? Sp27   { get; set; }
        [Column("SP28")]   public string? Sp28   { get; set; }
        [Column("SP29")]   public string? Sp29   { get; set; }
        [Column("SP30")]   public string? Sp30   { get; set; }
        [Column("SP31")]   public string? Sp31   { get; set; }
        [Column("SP32")]   public string? Sp32   { get; set; }
        [Column("SP33")]   public string? Sp33   { get; set; }
        [Column("SP34")]   public string? Sp34   { get; set; }
        [Column("SP35")]   public string? Sp35   { get; set; }
        [Column("SP36")]   public string? Sp36   { get; set; }
        [Column("SP37")]   public string? Sp37   { get; set; }
        [Column("SP38")]   public string? Sp38   { get; set; }
        [Column("SP39")]   public string? Sp39   { get; set; }
        [Column("SP40")]   public string? Sp40   { get; set; }
        [Column("SP41")]   public string? Sp41   { get; set; }
        [Column("SP42")]   public string? Sp42   { get; set; }
        [Column("SP43")]   public string? Sp43   { get; set; }
        [Column("SP44")]   public string? Sp44   { get; set; }
        [Column("SP45")]   public string? Sp45   { get; set; }
        [Column("SP46")]   public string? Sp46   { get; set; }
        [Column("SP47")]   public string? Sp47   { get; set; }
        [Column("SP48")]   public string? Sp48   { get; set; }
        [Column("SP49")]   public string? Sp49   { get; set; }
        [Column("SP50")]   public string? Sp50   { get; set; }
    }

    // EMPG_HIS: 동일 스키마, 테이블명만 다름
    [Table("EMPG_HIS")]
    public class EmpgHisEntity : EmpgEntity { }
}
