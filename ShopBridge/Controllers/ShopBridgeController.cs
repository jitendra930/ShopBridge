using Microsoft.AspNetCore.Mvc;
using ShopBridge.Models;
using System.Threading.Tasks;
using System;
using ShopBridge.Methods;
using Microsoft.Extensions.Logging;
using ShopBridge.Interface;

namespace ShopBridge.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class ShopBridgeController : Controller
    {
        private readonly IShopBridgeMethods _shopBridgeMethods;
        private readonly ILogger<ShopBridgeController> _logger;
        public ShopBridgeController(IShopBridgeMethods shopBridgeMethods, ILogger<ShopBridgeController> logger)
        {
            _shopBridgeMethods = shopBridgeMethods;
            _logger = logger;
        }

        /// <summary>
        /// This action method used to show list of all items in inventory
        /// return list of ShopBridgeInventory
        /// </summary>
        [HttpGet(Name = "ShowAllItemInInventory")]
        public List<ShopBridgeInventory>? ShowAllItemInInventory()
        {
            try
            {
                var res = _shopBridgeMethods.ShowAllItemInInventoryAsync().GetAwaiter().GetResult();
                return res;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Something went wrong in ShowAllItemInInventory ");
            }
            return null;
        }


        /// <summary>
        /// This action method used to add new item in inventory
        /// param = inventoryItem
        /// return bool
        /// </summary>
        [HttpPost(Name = "AddNewItemInInventory")]
        public ActionResult<bool> AddNewItemInInventory(InventoryItem inventoryItem)
        {
            try
            {
                var res = _shopBridgeMethods.AddNewItemInInventoryAsync(inventoryItem).GetAwaiter().GetResult();
                return res;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Something went wrong in AddNewItemInInventory");
            }
            return false;
        }

        /// <summary>
        /// This action method used to modify item in inventory
        /// param = name (Primary key)
        /// param = quantityAvailable (Updated quantity)
        /// return bool
        /// </summary>
        [HttpPut(Name = "ModifyQuantityAvailable")]
        public ActionResult<bool> ModifyQuantityAvailable(string name, int quantityAvailable)
        {
            try
            {
                var res = _shopBridgeMethods.ModifyQuantityAvailableAsync(name, quantityAvailable).GetAwaiter().GetResult();
                return res;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Something went wrong in ModifyQuantityAvailable");
            }
            return false;
        }

        /// <summary>
        /// This action method used to delete item in inventory
        /// param = name (Primary key)
        /// return bool
        /// </summary>
        [HttpDelete(Name = "DeleteItemInInventory")]
        public ActionResult<bool> DeleteItemInInventory(string name)
        {
            try
            {
                var res = _shopBridgeMethods.DeleteItemInInventoryAsync(name).GetAwaiter().GetResult();
                return res;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Something went wrong in DeleteItemInInventory");
            }
            return false;
        }
    }
}
