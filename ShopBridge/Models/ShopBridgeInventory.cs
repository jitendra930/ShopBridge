using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;


namespace ShopBridge.Models
{
    public partial class ShopBridgeInventory
    {
        [Key]
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public int Price { get; set; }
        public int QuantityAvailable { get; set; }
    }
}
