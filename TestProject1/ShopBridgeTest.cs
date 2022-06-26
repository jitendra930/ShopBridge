using Microsoft.VisualStudio.TestTools.UnitTesting;
using ShopBridge;
using ShopBridge.Models;
using Assert = Microsoft.VisualStudio.TestTools.UnitTesting.Assert;

namespace ShopBridgeTest
{
    [TestClass]
    public class ShopBridgeTest
    {
        [Fact(DisplayName = "Show all item from inventory")]
        public void ShowAllItemInInventoryAsyncTest()
        {
            ShopBridge.Methods.ShopBridgeMethods shopBridgeMethods = new ShopBridge.Methods.ShopBridgeMethods();
            var res = shopBridgeMethods.ShowAllItemInInventoryAsync().GetAwaiter().GetResult();
            Assert.IsNotNull(res);
        }

        [TestMethod]
        [Theory(DisplayName = "Modify quantity available")]
        [InlineData("Speaker", 15)]
        public void ModifyQuantityAvailableAsyncTest(string name, int quantityAvailable)
        {
            ShopBridge.Methods.ShopBridgeMethods shopBridgeMethods = new ShopBridge.Methods.ShopBridgeMethods();
            var res = shopBridgeMethods.ModifyQuantityAvailableAsync(name, quantityAvailable).GetAwaiter().GetResult();
            Assert.IsTrue(res);
        }


        [TestMethod]
        [Fact(DisplayName = "Add new item to inventory")]
        public void AddNewItemInInventoryAsyncTest()
        {
            InventoryItem inventory = new InventoryItem { Name = "CPU", Description = "Hp CPU", Price = 5000, QuantityAvailable = 10 };
            ShopBridge.Methods.ShopBridgeMethods shopBridgeMethods = new ShopBridge.Methods.ShopBridgeMethods();
            var res = shopBridgeMethods.AddNewItemInInventoryAsync(inventory).GetAwaiter().GetResult();
            Assert.IsTrue(res);
        }

        [TestMethod]
        [Theory(DisplayName = "Delete item from inventory")]
        [InlineData("Mouse")]
        public void DeleteItemInInventoryAsync(string name)
        {
            ShopBridge.Methods.ShopBridgeMethods shopBridgeMethods = new ShopBridge.Methods.ShopBridgeMethods();
            var res = shopBridgeMethods.DeleteItemInInventoryAsync(name).GetAwaiter().GetResult();
            Assert.IsTrue(res);
        }

    }
}