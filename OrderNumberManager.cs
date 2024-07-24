using System;
using System.IO;

namespace ThermalPrinterNetworkExample
{
    public static class OrderNumberManager
    {
        private static readonly string OrderNumberFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "OrderNumber.txt");

        public static int GetNextOrderNumber()
        {
            int currentOrderNumber = 1;

            if (File.Exists(OrderNumberFilePath))
            {
                string content = File.ReadAllText(OrderNumberFilePath);
                if (int.TryParse(content, out int lastOrderNumber))
                {
                    currentOrderNumber = (lastOrderNumber % 9999) + 1;
                }
            }

            File.WriteAllText(OrderNumberFilePath, currentOrderNumber.ToString());
            return currentOrderNumber;
        }
    }
}
