// Step 10: EF Core DbContext
// - EMPG  → DbSet<EmpgEntity>
// - EMPG_HIS → DbSet<EmpgHisEntity>
// - 인덱스 Phase B와 동일 환경에서 측정 (IX_EMPG_UPDATE_TIME 존재 상태)
// - Migration 미사용: Code-First 스키마 생성 X, 기존 DB에 연결만 함
using Microsoft.EntityFrameworkCore;

namespace ConSight.DAQ.Device.DB.EfCore
{
    public class DongBoDbContext : DbContext
    {
        public DongBoDbContext(DbContextOptions<DongBoDbContext> options) : base(options) { }

        public DbSet<EmpgEntity>    Empg    { get; set; } = null!;
        public DbSet<EmpgHisEntity> EmpgHis { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // EmpgHisEntity가 EmpgEntity를 상속하므로 TPH 상속 비활성화,
            // 별도 독립 테이블로 매핑
            modelBuilder.Entity<EmpgEntity>()
                .ToTable("EMPG")
                .HasKey(e => e.ResultId);

            // HasBaseType(null): C# 상속은 유지하되 EF Core 모델에서는 독립 엔티티로 등록.
            // 없으면 EF Core가 TPH로 처리해 파생 타입에 HasKey() 호출 불가 예외 발생.
            modelBuilder.Entity<EmpgHisEntity>()
                .HasBaseType((Type?)null)
                .ToTable("EMPG_HIS")
                .HasKey(e => e.ResultId);
        }
    }
}
