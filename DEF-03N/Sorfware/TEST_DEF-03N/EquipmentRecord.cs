namespace TEST_DEF_03N
{
    internal sealed class EquipmentRecord
    {
        public string Name { get; set; } = "";
        public string Series { get; set; } = "";
        public string EquipmentId { get; set; } = "";
        public string ExpireDate { get; set; } = "";

        public EquipmentRecord Clone() => new EquipmentRecord
        {
            Name = Name,
            Series = Series,
            EquipmentId = EquipmentId,
            ExpireDate = ExpireDate
        };

        public static List<EquipmentRecord> CreateDefaults() => new()
        {
            new EquipmentRecord
            {
                Name = "Ohm Source",
                Series = "OS-270",
                EquipmentId = "OS-274",
                ExpireDate = "3/2/27"
            },
            new EquipmentRecord
            {
                Name = "8 Digital Input Module",
                Series = "PH-01",
                EquipmentId = "67110058",
                ExpireDate = "-"
            }
        };
    }
}
