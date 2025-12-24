using Sandbox;
using System.Linq;

public sealed class InventoryUIBinder : Component
{
    [Property] public InventoryUI Panel { get; set; }

    protected override void OnStart()
    {
        // On cherche juste le premier PlayerController dans la scène
        var playerController = Scene.GetAllComponents<PlayerController>().FirstOrDefault();
        if (playerController == null)
        {
            Log.Warning("Aucun PlayerController trouvé dans la scène");
            return;
        }

        // Récupération du GameObject du joueur
        var playerGO = playerController.GameObject;
        if (playerGO == null)
        {
            Log.Warning("GameObject du player introuvable");
            return;
        }

        // Récupération de l'InventoryComponent
        var inventory = playerGO.Components.Get<InventoryComponent>();
        if (inventory == null)
        {
            Log.Warning("InventoryComponent introuvable sur le player");
            return;
        }

        // On branche l'inventaire au panel Razor
        Panel.Inventory = inventory;

        Log.Info("Inventory correctement branché !");
    }
}
