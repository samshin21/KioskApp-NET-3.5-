using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Newtonsoft.Json.Linq;

namespace KioskApp
{
    public partial class Form1 : Form
    {
        private string jsonPath;
        private JObject itemData;
        private Dictionary<string, List<Control>> categoryControls;
        private TableLayoutPanel panel;

        public Form1()
        {
            InitializeComponent();
            this.WindowState = FormWindowState.Maximized;

            ImportData(); // Import data during initialization
            InitializeUI(); // Initialize UI components
            CreateCategoryPictureBoxesAndPanels(); // Create categories
        }

        // Import Data Method
        private void ImportData()
        {
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            jsonPath = Path.Combine(desktopPath, "formatted_items.txt");

            itemData = LoadJsonData(jsonPath);

            if (itemData == null)
            {
                MessageBox.Show("Failed to load JSON data.");
            }
        }

        // Load JSON Data Method
        private JObject LoadJsonData(string path)
        {
            if (!File.Exists(path))
            {
                MessageBox.Show($"JSON file not found: {path}");
                return null;
            }

            try
            {
                string json = File.ReadAllText(path);
                return JObject.Parse(json);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to read or parse JSON file: {ex.Message}");
                return null;
            }
        }

        // Initialize UI Method
        private void InitializeUI()
        {
            panel = new TableLayoutPanel
            {
                AutoSize = true,
                ColumnCount = 6, // Set column count to 6
                RowCount = 0,
                Padding = new Padding(10), // Set padding to 10
                Dock = DockStyle.Fill
            };

            this.Controls.Add(panel);
            categoryControls = new Dictionary<string, List<Control>>();
        }

        // Create Categories Method
        private void CreateCategoryPictureBoxesAndPanels()
        {
            if (itemData == null) return;

            HashSet<string> uniqueCategories = new HashSet<string>();

            foreach (JObject item in itemData["data"])
            {
                if (item.TryGetValue("menucategory", out JToken categoryToken))
                {
                    string category = categoryToken.ToString();

                    if (uniqueCategories.Add(category)) // Add only if the category is unique
                    {
                        string categoryImagePath = GetImagePath($"{category}.bmp");
                        string picturesFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "KioskProject");
                        string fullCategoryImagePath = Path.Combine(picturesFolder, categoryImagePath.ToLower());

                        if (!File.Exists(fullCategoryImagePath))
                        {
                            fullCategoryImagePath = Path.Combine(picturesFolder, "image not avail.bmp");
                        }

                        Image categoryImage;
                        try
                        {
                            categoryImage = Image.FromFile(fullCategoryImagePath);
                        }
                        catch (Exception)
                        {
                            categoryImage = Image.FromFile(Path.Combine(picturesFolder, "image not avail.bmp")); // Load fallback image
                        }

                        PictureBox categoryPictureBox = CreateFormattedPictureBox(category, categoryImage, category);
                        categoryPictureBox.Visible = true;
                        categoryPictureBox.Click += CategoryPictureBox_Click;

                        Label categoryLabel = new Label
                        {
                            Text = category,
                            AutoSize = true,
                            TextAlign = ContentAlignment.MiddleCenter,
                            Margin = new Padding(10)
                        };

                        FlowLayoutPanel categoryPanel = new FlowLayoutPanel
                        {
                            FlowDirection = FlowDirection.TopDown,
                            AutoSize = true,
                            Margin = new Padding(10)
                        };

                        categoryPanel.Controls.Add(categoryPictureBox);
                        categoryPanel.Controls.Add(categoryLabel);
                        panel.Controls.Add(categoryPanel);
                    }
                }
                else
                {
                    MessageBox.Show($"Invalid item data in JSON file: {item}");
                }
            }
        }

        // Create Formatted PictureBox Method
        private PictureBox CreateFormattedPictureBox(string name, Image image, string tag)
        {
            return new PictureBox
            {
                Name = name,
                Size = new Size(158, 118),
                SizeMode = PictureBoxSizeMode.StretchImage,
                Image = image,
                Margin = new Padding(10),
                Tag = tag
            };
        }

        // Category PictureBox Click Handler
        private void CategoryPictureBox_Click(object sender, EventArgs e)
        {
            if (sender is PictureBox pictureBox)
            {
                string category = pictureBox.Tag.ToString();
                DisplayItemsInCategory(category);
            }
        }

        // Display Items in Category Method
        private void DisplayItemsInCategory(string category)
        {
            panel.Controls.Clear(); // Clear the current controls in the panel

            foreach (JObject item in itemData["data"])
            {
                if (item.TryGetValue("menucategory", out JToken categoryToken) && categoryToken.ToString() == category)
                {
                    if (item.TryGetValue("menuitem", out JToken itemNameToken) &&
                        item.TryGetValue("itemprice", out JToken itemPriceToken))
                    {
                        string itemName = itemNameToken.ToString();
                        string itemPrice = itemPriceToken.ToString();

                        string itemImagePath = GetImagePath($"{itemName}.bmp");
                        string picturesFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "KioskProject");
                        string fullItemImagePath = Path.Combine(picturesFolder, itemImagePath.ToLower());

                        if (!File.Exists(fullItemImagePath))
                        {
                            fullItemImagePath = Path.Combine(picturesFolder, "image not avail.bmp");
                        }

                        Image itemImage;
                        try
                        {
                            itemImage = Image.FromFile(fullItemImagePath);
                        }
                        catch (Exception)
                        {
                            itemImage = Image.FromFile(Path.Combine(picturesFolder, "image not avail.bmp")); // Load fallback image
                        }

                        PictureBox itemPictureBox = CreateFormattedPictureBox(itemName, itemImage, itemName);
                        itemPictureBox.Visible = true;

                        Label itemLabel = new Label
                        {
                            Text = $"{itemName} - ${itemPrice}",
                            AutoSize = true,
                            TextAlign = ContentAlignment.MiddleCenter,
                            Margin = new Padding(10)
                        };

                        FlowLayoutPanel itemPanel = new FlowLayoutPanel
                        {
                            FlowDirection = FlowDirection.TopDown,
                            AutoSize = true,
                            Margin = new Padding(10)
                        };

                        itemPanel.Controls.Add(itemPictureBox);
                        itemPanel.Controls.Add(itemLabel);
                        panel.Controls.Add(itemPanel);
                    }
                }
            }
        }

        // Get Image Path Method
        private string GetImagePath(string path)
        {
            string[] parts = path.Split(':');
            return parts.Last().Trim().ToLower();
        }
    }
}
