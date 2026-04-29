using Bi.ConSight_MxComponent.Data;

namespace ConSight.DAQ
{
    public sealed class ModelProduction
    {
        public string Model         { get; private set; }
        public double ProductionQty { get; private set; }
        public double FinishedQty   { get; private set; }
        public double DefectiveQty  { get; private set; }

        public double Yield => ProductionQty == 0 ? 0 : FinishedQty / ProductionQty;

        public ModelProduction(string model) { Model = model; }

        public ModelProduction(string model, double productionQty, double finishedQty, double defectiveQty)
        {
            Model         = model;
            ProductionQty = productionQty;
            FinishedQty   = finishedQty;
            DefectiveQty  = defectiveQty;
        }

        public void ApplyResult(string totalJudge)
        {
            ProductionQty++;
            if (totalJudge == MxComp_DB_JUDGE_CODE.OK) FinishedQty++;
            else DefectiveQty++;
        }
    }
}
