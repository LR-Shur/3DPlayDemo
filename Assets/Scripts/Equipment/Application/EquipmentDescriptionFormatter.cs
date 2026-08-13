using Train.Equipment.Data;

namespace Train.Equipment.Application
{
    /// <summary>统一生成装备详情中的描述和套装归属。</summary>
    public static class EquipmentDescriptionFormatter
    {
        public static string Format(
            EquipmentItemDefinition definition,
            IEquipmentService equipment)
        {
            if (definition == null)
            {
                return string.Empty;
            }

            var description = definition.Description ?? string.Empty;
            if (string.IsNullOrWhiteSpace(definition.SetId))
            {
                return description;
            }

            var setName = definition.SetId;
            if (equipment != null &&
                equipment.TryGetSetDefinition(definition.SetId, out var set) &&
                set != null &&
                !string.IsNullOrWhiteSpace(set.DisplayName))
            {
                setName = set.DisplayName;
            }

            var setDescription = $"所属套装：{setName}";
            return string.IsNullOrWhiteSpace(description)
                ? setDescription
                : $"{description}\n\n{setDescription}";
        }
    }
}
