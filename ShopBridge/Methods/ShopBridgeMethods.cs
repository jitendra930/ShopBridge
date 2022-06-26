using ShopBridge.Interface;
using ShopBridge.Models;

namespace ShopBridge.Methods
{
    public class ShopBridgeMethods: IShopBridgeMethods
    {
        
        public ShopBridgeMethods()
        {
            
        }

        public async Task<bool> AddNewItemInInventoryAsync(InventoryItem inventoryItem)
        {
            IotDBContext context = new IotDBContext();
            ShopBridgeInventory shopBridgeInventory = new ShopBridgeInventory
            {
                Name = inventoryItem.Name,
                Price = inventoryItem.Price,
                QuantityAvailable = inventoryItem.QuantityAvailable,
                Description = inventoryItem.Description,
            };
            await context.ShopBridgeInventories.AddAsync(shopBridgeInventory);
            context.SaveChanges();
            return true;
        }

        public async Task<bool> ModifyQuantityAvailableAsync(string name, int quantityAvailable)
        {
            IotDBContext context = new IotDBContext();
            var res = await context.ShopBridgeInventories.FindAsync(name);
            if (res != null)
            {
                res.QuantityAvailable = quantityAvailable;
                context.SaveChanges();
                return true;
            }
            return false;
        }


        public async Task<bool> DeleteItemInInventoryAsync(string name)
        {
            IotDBContext context = new IotDBContext();
            var res = await context.ShopBridgeInventories.FindAsync(name);
            if (res != null)
            {
                context.ShopBridgeInventories.Remove(res);
                context.SaveChanges();
                return true;
            }
            return false;
        }


        public async Task<List<ShopBridgeInventory>> ShowAllItemInInventoryAsync()
        {
            IotDBContext context = new IotDBContext();
            return await Task.Run(() => context.ShopBridgeInventories.ToList());
        }
    }
}
