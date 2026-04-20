namespace AIC_EDA.Models
{
    public enum AssetType
    {
        Machine,
        Material,
    }

    public class PlayerAsset
    {
        public string ItemId { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public AssetType Type { get; set; }
        public double Quantity { get; set; }
        public string? MachineTypeDisplay { get; set; }
    }
}
