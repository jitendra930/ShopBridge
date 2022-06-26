using ShopBridge.Models;

namespace ShopBridge.Interface
{
    public interface IShopBridgeMethods
    {
        Task<bool> AddNewItemInInventoryAsync(InventoryItem inventoryItem);
        Task<bool> ModifyQuantityAvailableAsync(string name, int quantityAvailable);
        Task<bool> DeleteItemInInventoryAsync(string name);
        Task<List<ShopBridgeInventory>> ShowAllItemInInventoryAsync();
    }
}
