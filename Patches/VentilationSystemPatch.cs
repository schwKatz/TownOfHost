
namespace TownOfHostY;

class VentilationSystemPatch
{
    public static void ClearVent()
    {
        if (!ShipStatus.Instance.Systems.TryGetValue(SystemTypes.Ventilation, out var systemType)) return;
        var instance = systemType.Cast<VentilationSystem>();
        instance.PlayersInsideVents.Clear();
        instance.IsDirty = true;
    }
}
