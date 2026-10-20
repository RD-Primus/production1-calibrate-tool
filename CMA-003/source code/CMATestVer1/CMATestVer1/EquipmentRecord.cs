namespace CMATestVer1
{
    internal sealed class EquipmentRecord
    {
        public string Name { get; set; } = "";
        public string Series { get; set; } = "";
        public string EquipmentId { get; set; } = "";
        public string Expiration { get; set; } = "-";

        public EquipmentRecord Clone()
        {
            return new EquipmentRecord
            {
                Name = Name,
                Series = Series,
                EquipmentId = EquipmentId,
                Expiration = Expiration
            };
        }

        public static List<EquipmentRecord> CreateDefaults()
        {
            return new List<EquipmentRecord>
            {
                new EquipmentRecord { Name = "Ohm Source", Series = "OS-270", EquipmentId = "OS-274", Expiration = "1/4/26" },
                new EquipmentRecord { Name = "Relay Output Module", Series = "PH-07", EquipmentId = "67110058", Expiration = "-" },
                new EquipmentRecord { Name = "8 Digital Input Module", Series = "PH-01", EquipmentId = "67110058", Expiration = "-" },
                new EquipmentRecord { Name = "Magnetic Contactor", Series = "S-T", EquipmentId = "S-T10", Expiration = "-" }
            };
        }
    }
}
