using System;
using System.Collections.Generic;

namespace ThermalPrinterNetworkExample
{
    public class ReceiptBuilder
    {
        private readonly string tempPrinter;
        private readonly string storename;
        private readonly string address1;
        private readonly string address2;
        private readonly string phoneNumber;
        private readonly string barcodesize;
        private readonly string pricebar;
        private readonly string width;
        private readonly List<OrderedItem> orderedItems;

        public ReceiptBuilder(string tempPrinter, string storename, string address1, string address2, string phoneNumber, string barcodesize, string pricebar, string width, List<OrderedItem> orderedItems)
        {
            this.tempPrinter = tempPrinter;
            this.storename = storename;
            this.address1 = address1;
            this.address2 = address2;
            this.phoneNumber = phoneNumber;
            this.barcodesize = barcodesize;
            this.pricebar = pricebar;
            this.width = width;
            this.orderedItems = orderedItems;
        }
        public void PrintReceipt(PrinterClient printerClient)
        {
            printerClient.WriteBytes(27, 33, 0);  // ESC ! 0 (normal height and width)
            printerClient.WriteBytes(27, 97, 49);  // ESC a justification center  
            printerClient.WriteString(storename + "\n");
            printerClient.WriteString(address1 + "\n");
            printerClient.WriteString(address2 + "\n");
            printerClient.WriteString(phoneNumber + "\n");
            printerClient.WriteBytes(27, 97, 48);  // ESC a left justification
            printerClient.WriteString("   " + DateTime.Now.ToString("hh:mm:ss") + "\n");
            printerClient.WriteString("   " + DateTime.Now.ToString("MM/dd/yyyy") + "\n");

            // Set double height for the order number
            printerClient.WriteBytes(27, 33, 16);  // ESC ! 16 (double height)
            printerClient.WriteString("order number:  99999\n");

            // Reset to normal height and width
            printerClient.WriteBytes(27, 33, 0);  // ESC ! 0 (normal height and width)

            printerClient.WriteString("------------------------------------------\n");
            printerClient.WriteString("   order #  68\n");

            decimal subtotal = 0;

            foreach (var item in orderedItems)
            {
                // Clean the price string and parse it
                string cleanedPrice = item.Price.Replace("$", "").Replace(",", "").Trim();
                if (!decimal.TryParse(cleanedPrice, out decimal price))
                {
                    // If parsing fails, set the price to 0
                    price = 0;
                }

                subtotal += price * item.Quantity;

                printerClient.WriteString($"{item.Quantity} {item.ItemName}{item.Price.PadLeft(30 - item.ItemName.Length)}\n");
                foreach (var modifier in item.Modifiers)
                {
                    printerClient.WriteString($"   + {modifier}\n");
                }
            }
            printerClient.WriteString("------------------------------------------\n");

            // Calculate tax and total
            decimal tax = subtotal * 0.10m; // Example 10% tax
            decimal total = subtotal + tax;

            // Print subtotal, tax, and total amounts
            printerClient.WriteBytes(27, 97, 50);  // ESC a right justification
            printerClient.WriteString($"Subtotal: {subtotal:C}".PadLeft(40) + "\n");
            printerClient.WriteString($"Tax: {tax:C}".PadLeft(40) + "\n");
            printerClient.WriteBytes(27, 33, 16);  // ESC ! 16 (double height)
            printerClient.WriteString($"Total: {total:C}".PadLeft(40) + "\n");
            printerClient.WriteBytes(27, 33, 0);  // ESC ! 0 (normal height and width)

            printerClient.WriteBytes(27, 97, 49);  // ESC a justification center
            printerClient.WriteString("*** Thank you! ***\n");

            // Add a few line feeds to ensure the last part of the receipt is printed before the cut
            printerClient.WriteString("\n\n\n");  // Adding more line feeds if needed

            // Full cut command as it was in the first successful example
            printerClient.WriteBytes(29, 86, 1);  // ESC V 1 (partial cut)
            printerClient.WriteBytes(29, 86, 0);  // ESC V 0 (full cut)
        }
    }
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
