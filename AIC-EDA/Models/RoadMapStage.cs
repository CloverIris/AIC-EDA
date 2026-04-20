using System.Collections.Generic;

namespace AIC_EDA.Models
{
    public class RoadMapStage
    {
        public int Order { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<string> RequiredItems { get; set; } = new();
        public List<string> RequiredMachines { get; set; } = new();
        public double EstimatedPower { get; set; }
        public double EstimatedArea { get; set; }
        public bool IsCompleted { get; set; }
    }
}
