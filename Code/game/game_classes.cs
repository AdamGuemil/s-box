using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;

[System.Serializable]
public class InventoryItem
{
    public string ItemId { get; set; }
    public string ItemName { get; set; }
    public int Quantity { get; set; }
    public int MaxStack { get; set; } = 64;

    public InventoryItem(string id, string name, int quantity = 1, int maxStack = 64)
    {
        ItemId = id;
        ItemName = name;
        Quantity = quantity;
        MaxStack = maxStack;
    }
}