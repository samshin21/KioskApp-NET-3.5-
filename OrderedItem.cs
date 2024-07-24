using System.Collections.Generic;

namespace ThermalPrinterNetworkExample
{
    public class OrderedItem
    {
        public string ItemName { get; set; }
        public int Quantity { get; set; }
        public List<string> Modifiers { get; set; }
        public string Price { get; set; }

        public OrderedItem(string itemName, int quantity, List<string> modifiers, string price)
        {
            ItemName = itemName;
            Quantity = quantity;
            Modifiers = modifiers;
            Price = price;
        }
    }
}
