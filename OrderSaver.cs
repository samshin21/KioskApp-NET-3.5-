using System;
using System.IO;
using System.Windows.Forms;

namespace ThermalPrinterNetworkExample
{
    public static class OrderSaver
    {
        public static void SaveOrder(ListView listView)
        {
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string orderFilePath = Path.Combine(desktopPath, "Order.txt");

            using (StreamWriter writer = new StreamWriter(orderFilePath, true))
            {
                writer.WriteLine("Order Summary:");
                writer.WriteLine("--------------");

                foreach (ListViewItem item in listView.Items)
                {
                    string qty = item.SubItems[0].Text;
                    string itemName = item.SubItems[1].Text;
                    string price = item.SubItems[2].Text;
                    writer.WriteLine($"{qty} x {itemName} - ${price}");
                }

                writer.WriteLine();
            }

            MessageBox.Show($"Order has been saved to {orderFilePath}", "Order Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}

