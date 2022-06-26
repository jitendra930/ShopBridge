using System.ComponentModel.DataAnnotations;

namespace ShopBridge.Models

{
    public class InventoryItem
    {

        [Key]
        [Required(ErrorMessage = "{0} is a mandatory field")]
        public string Name { get; set; }

        public String? Description { get; set; }

        [Required(ErrorMessage = "{0} is a mandatory field")]
        public int Price { get; set; }

        [Required(ErrorMessage = "{0} is a mandatory field")]
        public int QuantityAvailable { get; set; }

        
    }
}
