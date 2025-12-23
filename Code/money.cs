using Sandbox;

// ===== COMPOSANT JOUEUR (sur le GameObject joueur) =====
public sealed class PlayerMoney : Component
{
    // [Sync] = synchronisé automatiquement sur le réseau
    [Sync] private int _money { get; set; } = 0;
    
    // Affichage dans l'inspecteur (se met à jour correctement)
    [Property, ReadOnly, Title("Argent")]
    public string MoneyDisplay => $"{_money}€";
    
    [Property] public int StartingMoney { get; set; } = 100;

    public int GetMoney() => _money;
    
    protected override void OnStart()
    {
        // Charge l'argent sauvegardé si le joueur revient
        if (IsProxy) return; // Skip sur les clients
        
        LoadMoney();
    }
    
    // ===== AJOUTER DE L'ARGENT (CÔTÉ SERVEUR UNIQUEMENT) =====
    // ⚠️ CETTE MÉTHODE NE DEVRAIT ÊTRE APPELÉE QUE PAR DES SYSTÈMES DE JEU VALIDÉS
    // Ne pas exposer directement aux inputs joueur !
    public void AddMoneyServer(int amount, string reason = "")
    {
        if (!Networking.IsHost) return; // Sécurité : serveur uniquement
        if (amount <= 0) return;
        
        _money += amount;
        Log.Info($"Argent ajouté: +{amount} (raison: {reason}). Total: {_money}");
        SaveMoney();
    }
    
    // ===== RETIRER DE L'ARGENT (CÔTÉ SERVEUR UNIQUEMENT) =====
    public bool RemoveMoneyServer(int amount, string reason = "")
    {
        if (!Networking.IsHost) return false;
        if (amount <= 0) return false;
        if (_money < amount) 
        {
            Log.Warning($"Pas assez d'argent ! Besoin: {amount}, Disponible: {_money}");
            return false;
        }
        
        _money -= amount;
        Log.Info($"Argent retiré: -{amount} (raison: {reason}). Total: {_money}");
        SaveMoney();
        return true;
    }
    
    // ===== SYSTÈME DE SAUVEGARDE =====
    void SaveMoney()
    {
        if (!Networking.IsHost) return;
        
        // Utilise le SteamId pour identifier le joueur de manière unique
        var steamId = GetPlayerSteamId();
        if (string.IsNullOrEmpty(steamId)) return;
        
        // Sauvegarde dans le cookie (persistant entre sessions)
        Cookie.Set($"player_money_{steamId}", _money);
        Log.Info($"Argent sauvegardé: {_money}");
    }
    
    void LoadMoney()
    {
        if (!Networking.IsHost) return;
        
        var steamId = GetPlayerSteamId();
        if (string.IsNullOrEmpty(steamId))
        {
            Log.Warning($"SteamId vide en mode test éditeur. Money = StartingMoney ({StartingMoney})");
            // Modifie directement le backing field (l'éditeur ne peut pas toucher ça)
            _money = StartingMoney;
            return;
        }
        
        // Charge depuis le cookie ou utilise la valeur par défaut
        var loadedMoney = Cookie.Get($"player_money_{steamId}", StartingMoney);
        _money = loadedMoney;
        Log.Info($"Argent chargé: {_money}");
    }
    
    string GetPlayerSteamId()
    {
        // Récupère l'ID Steam du joueur (unique et sécurisé)
        var connection = GameObject.Network.Owner;
        if (connection == null) return string.Empty;
        
        return connection.SteamId.ToString();
    }
    
    // ===== RÉINITIALISER L'ARGENT (pour les tests) =====
    public void ResetMoney()
    {
        if (!Networking.IsHost) return;
        
        _money = StartingMoney;
        SaveMoney();
    }
}

// ===== EXEMPLE D'UTILISATION : ZONE DE COLLECTE D'ARGENT =====
public sealed class MoneyPickup : Component, Component.ITriggerListener
{
    [Property] public int MoneyAmount { get; set; } = 50;    
    private bool isActive = true;
    
public void OnTriggerEnter(Collider other)
    {
        if (!Networking.IsHost) return; // Serveur seulement - empêche le client de tricher
        if (!isActive) return;
        
        // Cherche le composant PlayerMoney sur l'objet
        var playerMoney = other.GameObject.Components.Get<PlayerMoney>();
        
        // Si pas trouvé, cherche dans le parent
        if (playerMoney == null && other.GameObject.Parent != null)
        {
            playerMoney = other.GameObject.Parent.Components.Get<PlayerMoney>();
        }
        
        // Si toujours pas trouvé, cherche dans le grand-parent
        if (playerMoney == null && other.GameObject.Parent?.Parent != null)
        {
            playerMoney = other.GameObject.Parent.Parent.Components.Get<PlayerMoney>();
        }
        
        if (playerMoney == null) return;
        
        // ✅ SÉCURISÉ : Le serveur valide et ajoute l'argent
        // Le client ne peut PAS appeler cette fonction directement
        playerMoney.AddMoneyServer(MoneyAmount, "MoneyPickup collecté");
        
        // Désactive temporairement
        isActive = false;
        GameObject.Destroy();
    }
}

// ===== EXEMPLE : ACHAT DANS UNE BOUTIQUE =====
public sealed class ShopItem : Component
{
    [Property] public int Price { get; set; } = 100;
    [Property] public string ItemName { get; set; } = "Item";
    
    public void TryBuy(GameObject buyer)
    {
        if (!Networking.IsHost) return; // Serveur seulement - empêche triche
        
        var playerMoney = buyer.Components.Get<PlayerMoney>();
        if (playerMoney == null) return;
        
        // ✅ SÉCURISÉ : Le serveur vérifie et retire l'argent
        if (playerMoney.RemoveMoneyServer(Price, $"Achat: {ItemName}"))
        {
            Log.Info($"Achat réussi: {ItemName}");
            GiveItemToPlayer(buyer);
        }
        else
        {
            Log.Warning($"Pas assez d'argent pour acheter {ItemName}");
        }
    }
    
    void GiveItemToPlayer(GameObject player)
    {
        // Donne l'item au joueur ici
        Log.Info($"Item {ItemName} donné au joueur");
    }
}