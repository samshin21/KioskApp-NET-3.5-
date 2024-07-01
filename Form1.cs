using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Newtonsoft.Json.Linq;

namespace Pictures
{
    public partial class Form1 : Form
    {
        private TableLayoutPanel panel;
        private Stack<string> navigationHistory;
        private Button nextButton;
        private Button previousButton;
        private string jsonPath;
        private string modifierJsonPath;
        private string modifierDetailJsonPath;
        private Dictionary<string, List<FlowLayoutPanel>> categoryControls;
        private List<FlowLayoutPanel> categoryPanels;
        private JObject itemData;
        private JObject modifierData;
        private JObject modifierDetailData;
        private List<string> currentModifierCodes;
        private int currentModifierIndex;
        private string previousCategory;
        private ListView selectionListView;
        private string storeName = "demostore1_123MainStr";
        private Dictionary<string, bool> modifierSelectionState;

        public Form1()
        {
            InitializeComponent();
            InitializeNavigationButtons();
            navigationHistory = new Stack<string>();
            this.WindowState = FormWindowState.Maximized;
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            jsonPath = Path.Combine(desktopPath, "formatted_items.txt");
            modifierJsonPath = Path.Combine(desktopPath, "formatted_modifierDef.txt");
            modifierDetailJsonPath = Path.Combine(desktopPath, "formatted_modifierDetail.txt");
            InitializeSelectionListView();
            LoadData();
            CreateCategoryPictureBoxesAndPanels();
            modifierSelectionState = new Dictionary<string, bool>();
        }

        private void InitializeNavigationButtons()
        {
            nextButton = new Button
            {
                Text = "Next",
                Dock = DockStyle.Bottom
            };
            nextButton.Click += NextButton_Click;

            previousButton = new Button
            {
                Text = "Previous",
                Dock = DockStyle.Bottom,
                Enabled = false // Initially disabled
            };
            previousButton.Click += PreviousButton_Click;

            this.Controls.Add(nextButton);
            this.Controls.Add(previousButton);
        }

        private void InitializeSelectionListView()
        {
            Log("Initializing selection list view.");
            selectionListView = new ListView
            {
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                Dock = DockStyle.Fill // Fill the remaining space
            };

            selectionListView.Columns.Add("Item", 150);
            selectionListView.Columns.Add("Price", 150);

            TableLayoutPanel rightPanel = new TableLayoutPanel
            {
                ColumnCount = 1,
                RowCount = 1,
                Dock = DockStyle.Right,
                Width = 300 // Set the width to accommodate buttons and listview
            };

            rightPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F)); // Make the ListView take the remaining space
            rightPanel.Controls.Add(selectionListView, 0, 0);

            this.Controls.Add(rightPanel);
        }

        private void LoadData()
        {
            itemData = LoadJsonData(jsonPath, "Item");
            modifierData = LoadJsonData(modifierJsonPath, "Modifier");
            modifierDetailData = LoadJsonData(modifierDetailJsonPath, "Modifier Detail");
        }

        private JObject LoadJsonData(string path, string dataType)
        {
            Log($"Loading {dataType} data.");
            if (!File.Exists(path))
            {
                MessageBox.Show($"{dataType} JSON file not found: {path}");
                Log($"{dataType} JSON file not found: {path}");
                return null;
            }

            string json;
            try
            {
                json = File.ReadAllText(path);
                Log($"{dataType} JSON file read successfully.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to read {dataType} JSON file: {ex.Message}");
                Log($"Failed to read {dataType} JSON file: {ex.Message}");
                return null;
            }

            try
            {
                JObject data = JObject.Parse(json);
                Log($"{dataType} JSON file parsed successfully.");
                return data;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to parse {dataType} JSON file: {ex.Message}");
                Log($"Failed to parse {dataType} JSON file: {ex.Message}");
                return null;
            }
        }

        private PictureBox CreateFormattedPictureBox(string name, Image image, string tag)
        {
            Log($"Creating formatted PictureBox for {name}");
            return new PictureBox
            {
                Name = name,
                Size = new Size(158, 118),
                SizeMode = PictureBoxSizeMode.StretchImage,
                Image = image,
                Margin = new Padding(10),
                Padding = new Padding(0),
                BorderStyle = BorderStyle.FixedSingle,
                Tag = tag
            };
        }

        private void CreateCategoryPictureBoxesAndPanels()
        {
            Log("Creating category picture boxes and panels.");

            if (itemData == null)
            {
                Log("Item data is not loaded.");
                return;
            }

            panel = new TableLayoutPanel
            {
                AutoSize = true,
                ColumnCount = 6,
                RowCount = 0,
                Padding = new Padding(10),
                Dock = DockStyle.Fill
            };

            this.Controls.Add(panel);

            categoryControls = new Dictionary<string, List<FlowLayoutPanel>>();
            categoryPanels = new List<FlowLayoutPanel>();

            foreach (JObject item in itemData["data"])
            {
                if (item.TryGetValue("menuitem", out JToken itemNameToken) &&
                    item.TryGetValue("menucategory", out JToken categoryToken))
                {
                    string itemName = itemNameToken.ToString();
                    string category = categoryToken.ToString();

                    string itemImagePath = GetImagePath($"{itemName}.bmp");
                    string categoryImagePath = GetImagePath($"{category}.bmp");
                    string picturesFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "KioskProject");
                    string fullItemImagePath = Path.Combine(picturesFolder, itemImagePath.ToLower());
                    string fullCategoryImagePath = Path.Combine(picturesFolder, categoryImagePath.ToLower());

                    if (!File.Exists(fullItemImagePath))
                    {
                        fullItemImagePath = Path.Combine(picturesFolder, "image not avail.bmp");
                    }

                    if (!File.Exists(fullCategoryImagePath))
                    {
                        fullCategoryImagePath = Path.Combine(picturesFolder, "image not avail.bmp");
                    }

                    Image itemImage = LoadImage(fullItemImagePath, itemName);
                    Image categoryImage = LoadImage(fullCategoryImagePath, category);

                    string tag = $"{category}|{itemName}";

                    PictureBox pictureBox = CreateFormattedPictureBox(itemName, itemImage, itemName);
                    pictureBox.Visible = true;
                    pictureBox.Click += new EventHandler(PictureBox_Click);

                    Label label = new Label
                    {
                        Name = itemName,
                        Text = $"{itemName}",
                        AutoSize = true,
                        Margin = new Padding(10),
                        TextAlign = ContentAlignment.MiddleCenter,
                        Visible = true
                    };

                    FlowLayoutPanel flowPanel = new FlowLayoutPanel
                    {
                        FlowDirection = FlowDirection.TopDown,
                        AutoSize = true,
                        Margin = new Padding(10),
                        Visible = true
                    };

                    flowPanel.Controls.Add(pictureBox);
                    flowPanel.Controls.Add(label);

                    if (!categoryControls.ContainsKey(category))
                    {
                        categoryControls[category] = new List<FlowLayoutPanel>();
                        PictureBox categoryPictureBox = CreateFormattedPictureBox(category, categoryImage, category);
                        categoryPictureBox.Visible = true;
                        categoryPictureBox.Click += (sender, e) => RefreshCategory(category);

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
                            Margin = new Padding(10),
                            Visible = true
                        };

                        categoryPanel.Controls.Add(categoryPictureBox);
                        categoryPanel.Controls.Add(categoryLabel);
                        panel.Controls.Add(categoryPanel);

                        categoryPanels.Add(categoryPanel);
                    }

                    categoryControls[category].Add(flowPanel);
                }
                else
                {
                    MessageBox.Show($"Invalid item data in JSON file: {item}");
                    Log($"Invalid item data in JSON file: {item}");
                }
            }

            Log("Category picture boxes and panels created.");
        }

        private void PictureBox_Click(object sender, EventArgs e)
        {
            PictureBox pictureBox = sender as PictureBox;
            if (pictureBox != null)
            {
                string itemTag = pictureBox.Tag.ToString();
                Log($"Item {itemTag} clicked.");
                navigationHistory.Push(previousCategory);
                RefreshItem(itemTag);
                UpdateNavigationButtons();
            }
        }

        private void RefreshCategory(string category)
        {
            Log($"Refreshing category: {category}");

            panel.Controls.Clear();
            panel.ColumnStyles.Clear();
            panel.RowStyles.Clear();

            panel.ColumnCount = 6; // Set this to the desired number of columns
            int column = 0;
            int row = 0;

            if (categoryControls.ContainsKey(category))
            {
                foreach (var flowPanel in categoryControls[category])
                {
                    if (column >= panel.ColumnCount)
                    {
                        column = 0;
                        row++;
                    }
                    panel.Controls.Add(flowPanel, column, row);
                    flowPanel.Visible = true; // Ensure visibility of the FlowLayoutPanel
                    column++;
                }
            }

            previousCategory = category;
            panel.Update(); // Explicitly update the layout
            Log($"Category refreshed: {category}");
        }

        private void RefreshItem(string itemTag)
        {
            Log($"Refreshing item: {itemTag}");

            panel.Controls.Clear();
            panel.ColumnStyles.Clear();
            panel.RowStyles.Clear();

            panel.ColumnCount = 6; // Set this to the desired number of columns

            DisplayItemModifiers(itemTag);

            var item = itemData["data"]
                .FirstOrDefault(m => m["menuitem"] != null && m["menuitem"].ToString() == itemTag);

            if (item != null)
            {
                string itemName = item["menuitem"].ToString();
                string itemPrice = item["itemprice"].ToString();

                ListViewItem listViewItem = new ListViewItem(itemName);
                listViewItem.SubItems.Add(itemPrice);

                selectionListView.Items.Add(listViewItem);
                Log($"Added item to selection: {itemName} - {itemPrice}");
            }

            panel.Update(); // Explicitly update the layout
        }

        private void NextButton_Click(object sender, EventArgs e)
        {
            // Implement the logic to navigate to the next screen
            // For example, if navigating to the next category or item
            string currentScreen = GetCurrentScreen(); // Implement this to get the current screen identifier
            navigationHistory.Push(currentScreen);

            // Navigate to the next screen
            // Example: RefreshCategory(nextCategory);

            UpdateNavigationButtons();
        }

        private void PreviousButton_Click(object sender, EventArgs e)
        {
            if (navigationHistory.Count > 0)
            {
                string previousScreen = navigationHistory.Pop();
                // Navigate to the previous screen
                // Example: RefreshCategory(previousScreen);

                RefreshCategory(previousScreen);

                UpdateNavigationButtons();
            }
        }

        private void UpdateNavigationButtons()
        {
            previousButton.Enabled = navigationHistory.Count > 0;
        }

        private string GetCurrentScreen()
        {
            // Implement this method to return the current screen identifier
            // For example, the current category or item being viewed
            return previousCategory; // Or any other appropriate identifier
        }

        private void DisplayItemModifiers(string itemTag)
        {
            Log($"Displaying modifiers for item: {itemTag}");
            var item = itemData["data"]
                .FirstOrDefault(m => m["menuitem"] != null && m["menuitem"].ToString() == itemTag);

            if (item == null)
            {
                Log($"No item found for tag: {itemTag}");
                return;
            }

            string[] modifierSections = { "aa", "bb", "cc", "dd", "ee", "ff", "gg", "hh", "ii", "jj", "kk", "ll" };
            currentModifierCodes = new List<string>();

            foreach (var section in modifierSections)
            {
                if (item[section] != null && item[section].Type == JTokenType.String && !string.IsNullOrEmpty(item[section].ToString()))
                {
                    currentModifierCodes.Add(item[section].ToString());
                }
            }

            currentModifierIndex = 0;
            previousCategory = item["menucategory"].ToString();
            DisplayNextModifier();
        }

        private void DisplayNextModifier()
        {
            if (currentModifierIndex >= currentModifierCodes.Count)
            {
                Log("No more modifiers to display.");
                DisplayFinalSaleScreen();
                return;
            }

            string modCode = currentModifierCodes[currentModifierIndex];
            currentModifierIndex++;

            var modifierDef = modifierData["data"]
                .FirstOrDefault(m => m["modcode"] != null && m["modcode"].ToString() == modCode);

            if (modifierDef != null)
            {
                DisplayModifierDetails(modCode);
            }
            else
            {
                DisplayNextModifier();
            }
        }

        private void DisplayModifierDetails(string modCode)
        {
            Log($"Displaying details for modifier: {modCode}");

            panel.Controls.Clear();

            var modifierDef = modifierData["data"]
                .FirstOrDefault(m => m["modcode"] != null && m["modcode"].ToString() == modCode);

            if (modifierDef == null)
            {
                Log($"No modifier definition found for code: {modCode}");
                return;
            }

            string modChoiceType = modifierDef["modchoice"]?.ToString() ?? "one";

            var modifierDetails = modifierDetailData["data"]
                .Where(d => d["modcode"] != null && d["modcode"].ToString() == modCode)
                .ToList();

            foreach (var detail in modifierDetails)
            {
                string detailDesc = detail["description"]?.ToString() ?? "Unknown Detail";
                string cost = detail["cost"]?.ToString() ?? "";

                string imagePath = GetImagePath(detail["location"]?.ToString() ?? "image not avail.bmp");
                string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                string picturesFolder = Path.Combine(desktopPath, "KioskProject");
                string fullImagePath = Path.Combine(picturesFolder, imagePath.ToLower());

                if (!File.Exists(fullImagePath))
                {
                    fullImagePath = Path.Combine(picturesFolder, "image not avail.bmp");
                }

                Image detailImage = LoadImage(fullImagePath, detailDesc);

                PictureBox detailPictureBox = CreateFormattedPictureBox(detailDesc, detailImage, detailDesc);

                if (modChoiceType == "one")
                {
                    detailPictureBox.Click += (sender, e) => ModifierDetailPictureBox_Click_One(sender, e, modCode);
                }
                else if (modChoiceType == "upsale")
                {
                    detailPictureBox.Click += (sender, e) => ModifierDetailPictureBox_Click_Upsale(sender, e, modCode);
                }

                Label detailLabel = new Label
                {
                    Text = string.IsNullOrEmpty(cost) ? detailDesc : $"{detailDesc} (+${cost})",
                    AutoSize = true,
                    TextAlign = ContentAlignment.MiddleCenter,
                    Margin = new Padding(10)
                };

                FlowLayoutPanel detailPanel = new FlowLayoutPanel
                {
                    FlowDirection = FlowDirection.TopDown,
                    AutoSize = true,
                    Margin = new Padding(10)
                };

                detailPanel.Controls.Add(detailPictureBox);
                detailPanel.Controls.Add(detailLabel);
                panel.Controls.Add(detailPanel);

                if (!modifierSelectionState.ContainsKey(detailDesc))
                {
                    modifierSelectionState[detailDesc] = false;
                }

                UpdatePictureBoxSelectionState(detailPictureBox, modifierSelectionState[detailDesc], detailDesc, modCode);
            }
        }

        private void DisplayFinalSaleScreen()
        {
            Log("Displaying final sale screen.");

            panel.Controls.Clear();

            Label finalMessage = new Label
            {
                Text = "Thank you for your purchase!",
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleCenter,
                Margin = new Padding(10),
                Font = new Font("Arial", 24, FontStyle.Bold)
            };

            FlowLayoutPanel finalPanel = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown,
                AutoSize = true,
                Margin = new Padding(10)
            };

            finalPanel.Controls.Add(finalMessage);
            panel.Controls.Add(finalPanel);
        }

        private void ModifierDetailPictureBox_Click_One(object sender, EventArgs e, string modCode)
        {
            PictureBox pictureBox = sender as PictureBox;
            if (pictureBox != null)
            {
                string detailDesc = pictureBox.Tag.ToString();
                Log($"Modifier detail clicked (one): {detailDesc}");

                if (!modifierSelectionState.ContainsKey(detailDesc) || !modifierSelectionState[detailDesc])
                {
                    ToggleModifierSelection(pictureBox, detailDesc, modCode);
                }

                DisplayNextModifier();
            }
        }

        private void ModifierDetailPictureBox_Click_Upsale(object sender, EventArgs e, string modCode)
        {
            PictureBox pictureBox = sender as PictureBox;
            if (pictureBox != null)
            {
                string detailDesc = pictureBox.Tag.ToString();
                Log($"Modifier detail clicked (upsale): {detailDesc}");
                ToggleModifierSelection(pictureBox, detailDesc, modCode);
            }
        }

        private void ToggleModifierSelection(PictureBox pictureBox, string detailDesc, string modCode)
        {
            if (modifierSelectionState.ContainsKey(detailDesc))
            {
                modifierSelectionState[detailDesc] = !modifierSelectionState[detailDesc];
                string action = modifierSelectionState[detailDesc] ? "Selecting" : "Deselecting";
                Log($"{action} modifier detail: {detailDesc}");
                UpdatePictureBoxSelectionState(pictureBox, modifierSelectionState[detailDesc], detailDesc, modCode);
                UpdateSelectionListView(detailDesc, modifierSelectionState[detailDesc], modCode);
            }
        }

        private void UpdatePictureBoxSelectionState(PictureBox pictureBox, bool isSelected, string detailDesc, string modCode)
        {
            var modifierDef = modifierData["data"]
                .FirstOrDefault(m => m["modcode"] != null && m["modcode"].ToString() == modCode);

            if (modifierDef != null)
            {
                string modChoiceType = modifierDef["modchoice"]?.ToString() ?? "one";
                if (modChoiceType == "upsale")
                {
                    if (isSelected)
                    {
                        Bitmap overlayImage = new Bitmap(pictureBox.Image);
                        using (Graphics g = Graphics.FromImage(overlayImage))
                        {
                            using (Brush brush = new SolidBrush(Color.FromArgb(128, Color.Yellow)))
                            {
                                g.FillRectangle(brush, new Rectangle(0, 0, overlayImage.Width, overlayImage.Height));
                            }
                        }
                        pictureBox.Image = overlayImage;
                        Log($"Applied overlay to modifier detail image: {detailDesc}");
                    }
                    else
                    {
                        string imagePath = GetImagePath($"{detailDesc}.bmp");
                        string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                        string picturesFolder = Path.Combine(desktopPath, "KioskProject");
                        string fullImagePath = Path.Combine(picturesFolder, imagePath.ToLower());

                        if (File.Exists(fullImagePath))
                        {
                            pictureBox.Image = Image.FromFile(fullImagePath);
                        }
                        else
                        {
                            pictureBox.Image = Image.FromFile(Path.Combine(picturesFolder, "image not avail.bmp")); // Load fallback image
                        }
                        Log($"Restored original image for modifier detail: {detailDesc}");
                    }
                }
                else
                {
                    pictureBox.BackColor = isSelected ? Color.Green : Color.Transparent;
                    Log($"{(isSelected ? "Selected" : "Deselected")} modifier detail: {detailDesc}");
                }
            }
        }

        private void UpdateSelectionListView(string detailDesc, bool isSelected, string modCode)
        {
            var detail = modifierDetailData["data"]
                .FirstOrDefault(d => d["modcode"] != null && d["modcode"].ToString() == modCode && d["description"]?.ToString() == detailDesc);
            string cost = detail?["cost"]?.ToString() ?? "";

            if (isSelected)
            {
                ListViewItem listViewItem = new ListViewItem(detailDesc);
                listViewItem.SubItems.Add(cost);
                selectionListView.Items.Add(listViewItem);
                Log($"Added modifier detail to selection: {detailDesc}");
            }
            else
            {
                ListViewItem itemToRemove = selectionListView.Items.Cast<ListViewItem>().FirstOrDefault(item => item.Text == detailDesc);
                if (itemToRemove != null)
                {
                    selectionListView.Items.Remove(itemToRemove);
                    Log($"Removed modifier detail from selection: {detailDesc}");
                }
            }
        }

        private string GetImagePath(string path)
        {
            string[] parts = path.Split(':');
            return parts.Last().Trim().ToLower();
        }

        private void Log(string functionName)
        {
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string logFilePath = Path.Combine(desktopPath, $"{storeName}_{DateTime.Today:yyyy-MM-dd}.txt");
            string logMessage = $"[{DateTime.Now:HH:mm:ss}] {functionName}";
            try
            {
                File.AppendAllText(logFilePath, logMessage + Environment.NewLine);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to write log: " + ex.Message);
            }
        }

        private Image LoadImage(string path, string description)
        {
            try
            {
                Image image = Image.FromFile(path);
                Log($"Loaded image for {description}");
                return image;
            }
            catch (Exception)
            {
                string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                string kioskProjectFolder = Path.Combine(desktopPath, "KioskProject");
                string fallbackPath = Path.Combine(kioskProjectFolder, "image not avail.bmp");
                Image fallbackImage = Image.FromFile(fallbackPath);
                Log($"Failed to load image for {description}. Loaded fallback image.");
                return fallbackImage;
            }
        }
    }
}
