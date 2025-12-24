using Sandbox;

public sealed class StatsComponents : Component
{
[Property]     public HealthComponent Health { get; private set; }
  [Property]   public ArmorComponent Armor { get; private set; }
  [Property]   public InventoryComponent Inventory { get; private set; }
  [Property]   public MoneyComponent Money { get; private set; }

    protected override void OnStart()
    {
        Health    = GameObject.Components.GetOrCreate<HealthComponent>();
        Armor     = GameObject.Components.GetOrCreate<ArmorComponent>();
        Inventory = GameObject.Components.GetOrCreate<InventoryComponent>();
        Money     = GameObject.Components.GetOrCreate<MoneyComponent>();
    }
}