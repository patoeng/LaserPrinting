using System.Collections.Generic;
using MesData.Settings;

namespace LaserPrinting.Model
{
    public class LocalProductionOrder:AppSettings<LocalProductionOrder>
    {
        public string ProductionOrder { get; set; } = "";
        public int DummyQty { get; set; } = 3;
        public double TimeOffset { get; set; } = 8f;
        public List<string> ContainerList { get; set; } = new List<string>();
        public bool PreparationFinished { get; set; } = false;
    }
}
