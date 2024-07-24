using System;
using System.Collections.Specialized;
using System.Net;
using System.Text;
using System.Windows.Forms;

namespace ThermalPrinterNetworkExample
{
    public static class HttpService
    {
        public static void MakeHttpCall(string orderNumber)
        {
            string url = @"http://apibeast.com/Datatables/controllers/samtest.php";
            var parameters = new NameValueCollection
            {
                { "orderNumber", orderNumber }
            };

            using (WebClient client = new WebClient())
            {
                try
                {
                    client.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";
                    byte[] responseBytes = client.UploadValues(url, "POST", parameters);
                    string response = Encoding.UTF8.GetString(responseBytes);
                    MessageBox.Show($"Response from server: {response}", "HTTP Call Result", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (WebException ex)
                {
                    MessageBox.Show($"An error occurred: {ex.Message}", "HTTP Call Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
    }
}
