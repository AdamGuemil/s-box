using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;

// ===== CLASSE ITEM =====


// ===== COMPOSANT INVENTAIRE (sur le joueur) =====
public sealed class InventoryComponent : Component
{
    [Property] public int MaxSlots { get; set; } = 20;
    
    // Liste des items dans l'inventaire
    private List<InventoryItem> items = new List<InventoryItem>();

    // Affichage pour l'inspecteur
    [Property, ReadOnly, Title("📦 Nombre d'items")]
    public int ItemCount => items.Count;

    protected override void OnStart()
    {
        Log.Info("Inventaire initialisé");
    }

    // ===== AJOUTER UN ITEM =====
    public bool AddItem(string itemId, string itemName, int quantity = 1, int maxStack = 64)
    {
        // Vérifie si l'item existe déjà (pour stacker)
        var existingItem = items.FirstOrDefault(i => i.ItemId == itemId);

        if (existingItem != null)
        {
            // Stack sur l'item existant
            int spaceLeft = existingItem.MaxStack - existingItem.Quantity;
            int toAdd = System.Math.Min(quantity, spaceLeft);

            existingItem.Quantity += toAdd;
            Log.Info($"Stacké {toAdd}x {itemName}. Total: {existingItem.Quantity}");

            // S'il reste des items à ajouter
            quantity -= toAdd;
            if (quantity > 0)
            {
                return AddItem(itemId, itemName, quantity, maxStack); // Récursif
            }
            return true;
        }

        // Nouvel item
        if (items.Count >= MaxSlots)
        {
            Log.Warning("Inventaire plein !");
            return false;
        }

        var newItem = new InventoryItem(itemId, itemName, quantity, maxStack);
        items.Add(newItem);
        Log.Info($"Ajouté {quantity}x {itemName} à l'inventaire");
        return true;
    }

    // ===== RETIRER UN ITEM =====
    public bool RemoveItem(string itemId, int quantity = 1)
    {
        var item = items.FirstOrDefault(i => i.ItemId == itemId);

        if (item == null)
        {
            Log.Warning($"Item {itemId} non trouvé !");
            return false;
        }

        if (item.Quantity < quantity)
        {
            Log.Warning($"Pas assez de {item.ItemName} ! Besoin: {quantity}, Disponible: {item.Quantity}");
            return false;
        }

        item.Quantity -= quantity;
        Log.Info($"Retiré {quantity}x {item.ItemName}");

        // Supprime l'item si quantité = 0
        if (item.Quantity <= 0)
        {
            items.Remove(item);
            Log.Info($"{item.ItemName} retiré de l'inventaire (quantité = 0)");
        }

        return true;
    }

    // ===== VÉRIFIER SI ON A UN ITEM =====
    public bool HasItem(string itemId, int quantity = 1)
    {
        var item = items.FirstOrDefault(i => i.ItemId == itemId);
        return item != null && item.Quantity >= quantity;
    }

    // ===== OBTENIR LA QUANTITÉ D'UN ITEM =====
    public int GetItemCount(string itemId)
    {
        var item = items.FirstOrDefault(i => i.ItemId == itemId);
        return item?.Quantity ?? 0;
    }

    // ===== AFFICHER L'INVENTAIRE =====
    public void DisplayInventory()
    {
        Log.Info("=== INVENTAIRE ===");
        if (items.Count == 0)
        {
            Log.Info("Inventaire vide");
            return;
        }

        foreach (var item in items)
        {
            Log.Info($"- {item.ItemName} x{item.Quantity}");
        }
        Log.Info($"Slots utilisés: {items.Count}/{MaxSlots}");
    }

    // ===== VIDER L'INVENTAIRE =====
    public void ClearInventory()
    {
        items.Clear();
        Log.Info("Inventaire vidé");
    }

    // ===== OBTENIR TOUS LES ITEMS =====
    public List<InventoryItem> GetAllItems()
    {
        return new List<InventoryItem>(items); // Copie pour éviter modifications externes
    }
}

// ===== EXEMPLE : ITEM RAMASSABLE AVEC PROMPT (RAYCAST + DISTANCE) =====
public sealed class ItemPickup : Component
{
    [Property] public string ItemId { get; set; } = "wood";
    [Property] public string ItemName { get; set; } = "Bois";
    [Property] public int Quantity { get; set; } = 1;
    [Property] public int MaxStack { get; set; } = 64;
    
    [Property] public float MaxPickupDistance { get; set; } = 300f; // Distance max pour ramasser
    [Property] public float MaxViewAngle { get; set; } = 45f; // Angle max pour "regarder" l'item

    private bool isLookingAt = false;
    private float distanceToPlayer = float.MaxValue;

    protected override void OnUpdate()
    {
        CheckIfPlayerLooking();

        // Affiche le prompt si le joueur regarde l'item
        if (isLookingAt)
        {
            DisplayPrompt();
        }

        // Gestion du ramassage
        HandlePickup();
    }

    void CheckIfPlayerLooking()
    {
        var camera = Scene.Camera;
        if (camera == null)
        {
            isLookingAt = false;
            return;
        }

        // Calcule la distance entre la caméra et l'item
        distanceToPlayer = Vector3.DistanceBetween(camera.Transform.Position, Transform.Position);

        // Trop loin ?
        if (distanceToPlayer > MaxPickupDistance)
        {
            isLookingAt = false;
            return;
        }

        // Direction de la caméra vers l'item
        var directionToItem = (Transform.Position - camera.Transform.Position).Normal;
        var cameraForward = camera.Transform.Rotation.Forward;

        // Calcule l'angle entre la direction de la caméra et l'item
        var dotProduct = Vector3.Dot(cameraForward, directionToItem);
        var angle = Math.Acos(Math.Clamp(dotProduct, -1f, 1f)) * (180f / Math.PI);

        // Le joueur regarde l'item ?
        isLookingAt = angle <= MaxViewAngle;
    }

    void HandlePickup()
    {
        // Si le joueur appuie sur F
        if (!Input.Keyboard.Pressed("f"))
            return;

        // Trouve l'item le plus proche parmi ceux regardés
        var closestItem = FindClosestLookedAtItem();

        // Si cet item est le plus proche, ramasse-le
        if (closestItem == this)
        {
            TryPickup();
        }
    }

    ItemPickup FindClosestLookedAtItem()
    {
        // Trouve tous les ItemPickup de la scène
        var allItems = Scene.GetAllComponents<ItemPickup>();

        ItemPickup closest = null;
        float closestDistance = float.MaxValue;

        foreach (var item in allItems)
        {
            // Seulement les items regardés
            if (!item.isLookingAt) continue;

            if (item.distanceToPlayer < closestDistance)
            {
                closestDistance = item.distanceToPlayer;
                closest = item;
            }
        }

        return closest;
    }

    void TryPickup()
    {
        if (!Networking.IsHost) return; // Serveur seulement

        // Cherche l'inventaire dans la scène (sur le joueur)
        var inventory = Scene.GetAllComponents<InventoryComponent>().FirstOrDefault();
        
        if (inventory == null)
        {
            Log.Warning("Aucun InventoryComponent trouvé !");
            return;
        }

        // Ajoute l'item
        if (inventory.AddItem(ItemId, ItemName, Quantity, MaxStack))
        {
            // Item ramassé avec succès
            Log.Info($"✅ Ramassé {Quantity}x {ItemName}");
            GameObject.Destroy();
        }
    }

    void DisplayPrompt()
    {
        // Affiche un texte 3D au-dessus de l'item
        var screenPos = Scene.Camera.PointToScreenPixels(Transform.Position + Vector3.Up * 20);
        
        // Fond noir semi-transparent (simplifié pour s&box)
        var rectWidth = 250f;
        var rectHeight = 40f;
        var rect = new Rect(screenPos.x - rectWidth / 2, screenPos.y - rectHeight / 2, rectWidth, rectHeight);
        
        // Dessine le fond
        Gizmo.Draw.Color = new Color(0, 0, 0, 0.8f);
        Gizmo.Draw.LineBBox(new BBox(new Vector3(rect.Left, rect.Top, 0), new Vector3(rect.Right, rect.Bottom, 0)));
        
        // Texte blanc par-dessus
        Gizmo.Draw.Color = Color.White;
        Gizmo.Draw.ScreenText($"[F] Ramasser {ItemName} x{Quantity}", screenPos, "Poppins", 18);
    }
}

// ===== EXEMPLE : CRAFT SIMPLE =====
public sealed class CraftingStation : Component
{
    [Property] public string RecipeItemId { get; set; } = "plank";
    [Property] public string RecipeItemName { get; set; } = "Planche";
    [Property] public string RequiredItemId { get; set; } = "wood";
    [Property] public int RequiredQuantity { get; set; } = 4;
    [Property] public int OutputQuantity { get; set; } = 1;

    public bool TryCraft(InventoryComponent inventory)
    {
        if (inventory == null) return false;

        // Vérifie si le joueur a les ressources
        if (!inventory.HasItem(RequiredItemId, RequiredQuantity))
        {
            Log.Warning($"Pas assez de ressources ! Besoin: {RequiredQuantity}x {RequiredItemId}");
            return false;
        }

        // Retire les ressources
        if (!inventory.RemoveItem(RequiredItemId, RequiredQuantity))
        {
            return false;
        }

        // Donne l'item crafté
        inventory.AddItem(RecipeItemId, RecipeItemName, OutputQuantity);
        
        Log.Info($"Craft réussi : {OutputQuantity}x {RecipeItemName}");
        return true;
    }
}