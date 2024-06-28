using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        private TableLayoutPanel modifierPanel;
        private string jsonPath;
        private string modifierJsonPath;
        private string modifierDetailJsonPath;
        private Dictionary<string, List<Control>> categoryControls;
        private List<FlowLayoutPanel> categoryPanels;
        private JObject itemData;
        private JObject modifierData;
        private JObject modifierDetailData;
        private List<string> currentModifierCodes;
        private int currentModifierIndex;
        private string previousCategory;
        private Button previousButton;
        private Button nextButton;
        private ListView selectionListView;
        private string storeName = "demostore1_123MainStr";
        private Dictionary<string, bool> modifierSelectionState;

        // Enum to capture the current state
        private enum State { Category, Item, Modifier }
        private State currentState;

        public Form1()
        {
            InitializeComponent();
            this.WindowState = FormWindowState.Maximized;
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            jsonPath = Path.Combine(desktopPath, "formatted_items.txt");
            modifierJsonPath = Path.Combine(desktopPath, "formatted_modifierDef.txt");
            modifierDetailJsonPath = Path.Combine(desktopPath, "formatted_modifierDetail.txt");
            InitializeSelectionListViewAndNavigationButtons();
            LoadItemData();
            LoadModifierData();
            LoadModifierDetailData();
            CreateCategoryPictureBoxesAndPanels(jsonPath);
            modifierSelectionState = new Dictionary<string, bool>();
        }

        // Initialization Methods
        private void InitializeSelectionListViewAndNavigationButtons()
        {
            Log("Initializing selection list view and navigation buttons.");
            selectionListView = new ListView
            {
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                Dock = DockStyle.Fill // Fill the remaining space
            };

            selectionListView.Columns.Add("Item", 150);
            selectionListView.Columns.Add("Price", 150);

            previousButton = new Button
            {
                Text = "Previous",
                AutoSize = true,
                Margin = new Padding(10),
                Visible = false // Initially invisible
            };
            previousButton.Click += PreviousButton_Click;

            nextButton = new Button
            {
                Text = "Next",
                AutoSize = true,
                Margin = new Padding(10),
                Visible = false // Initially invisible
            };
            nextButton.Click += NextButton_Click;

            FlowLayoutPanel buttonPanel = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true,
                Margin = new Padding(10),
            };

            buttonPanel.Controls.Add(previousButton);
            buttonPanel.Controls.Add(nextButton);

            TableLayoutPanel rightPanel = new TableLayoutPanel
            {
                ColumnCount = 1,
                RowCount = 2,
                Dock = DockStyle.Right,
                Width = 300 // Set the width to accommodate buttons and listview
            };

            rightPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            rightPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F)); // Make the ListView take the remaining space
            rightPanel.Controls.Add(buttonPanel, 0, 0);
            rightPanel.Controls.Add(selectionListView, 0, 1);

            this.Controls.Add(rightPanel);
        }

        // Data Loading Methods
        private void LoadItemData()
        {
            Log("Loading item data.");
            if (!File.Exists(jsonPath))
            {
                MessageBox.Show("Item JSON file not found: " + jsonPath);
                Log("Item JSON file not found: " + jsonPath);
                return;
            }

            string json;
            try
            {
                json = File.ReadAllText(jsonPath);
                Log("Item JSON file read successfully.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to read Item JSON file: " + ex.Message);
                Log("Failed to read Item JSON file: " + ex.Message);
                return;
            }

            try
            {
                itemData = JObject.Parse(json);
                Log("Item JSON file parsed successfully.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to parse Item JSON file: " + ex.Message);
                Log("Failed to parse Item JSON file: " + ex.Message);
            }
        }

        private void LoadModifierData()
        {
            Log("Loading modifier data.");
            if (!File.Exists(modifierJsonPath))
            {
                MessageBox.Show("Modifier JSON file not found: " + modifierJsonPath);
                Log("Modifier JSON file not found: " + modifierJsonPath);
                return;
            }

            string json;
            try
            {
                json = File.ReadAllText(modifierJsonPath);
                Log("Modifier JSON file read successfully.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to read Modifier JSON file: " + ex.Message);
                Log("Failed to read Modifier JSON file: " + ex.Message);
                return;
            }

            try
            {
                modifierData = JObject.Parse(json);
                Log("Modifier JSON file parsed successfully.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to parse Modifier JSON file: " + ex.Message);
                Log("Failed to parse Modifier JSON file: " + ex.Message);
            }
        }

        private void LoadModifierDetailData()
        {
            Log("Loading modifier detail data.");
            if (!File.Exists(modifierDetailJsonPath))
            {
                MessageBox.Show("Modifier Detail JSON file not found: " + modifierDetailJsonPath);
                Log("Modifier Detail JSON file not found: " + modifierDetailJsonPath);
                return;
            }

            string json;
            try
            {
                json = File.ReadAllText(modifierDetailJsonPath);
                Log("Modifier Detail JSON file read successfully.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to read Modifier Detail JSON file: " + ex.Message);
                Log("Failed to read Modifier Detail JSON file: " + ex.Message);
                return;
            }

            try
            {
                modifierDetailData = JObject.Parse(json);
                Log("Modifier Detail JSON file parsed successfully.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to parse Modifier Detail JSON file: " + ex.Message);
                Log("Failed to parse Modifier Detail JSON file: " + ex.Message);
            }
        }

        // UI Creation Methods
        private PictureBox CreateFormattedPictureBox(string name, Image image, string tag)
        {
            Log("Creating formatted PictureBox for " + name);
            return new PictureBox
            {
                Name = name,
                Size = new Size(158, 118),
                SizeMode = PictureBoxSizeMode.StretchImage,
                Image = image,
                Margin = new Padding(10),
                Padding = new Padding(0),
                BorderStyle = BorderStyle.FixedSingle, // For debugging layout issues
                Tag = tag
            };
        }

        private void CreateCategoryPictureBoxesAndPanels(string jsonPath)
        {
            Log("Creating category picture boxes and panels.");
            if (!File.Exists(jsonPath))
            {
                MessageBox.Show("JSON file not found: " + jsonPath);
                Log("JSON file not found: " + jsonPath);
                return;
            }

            string json;
            try
            {
                json = File.ReadAllText(jsonPath);
                Log("JSON file read successfully.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to read JSON file: " + ex.Message);
                Log("Failed to read JSON file: " + ex.Message);
                return;
            }

            JObject data;
            try
            {
                data = JObject.Parse(json);
                Log("JSON file parsed successfully.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to parse JSON file: " + ex.Message);
                Log("Failed to parse JSON file: " + ex.Message);
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

            modifierPanel = new TableLayoutPanel
            {
                AutoSize = true,
                ColumnCount = 6,
                RowCount = 0,
                Padding = new Padding(10),
                Dock = DockStyle.Bottom,
                Visible = false
            };

            this.Controls.Add(panel);
            this.Controls.Add(modifierPanel);

            categoryControls = new Dictionary<string, List<Control>>();
            categoryPanels = new List<FlowLayoutPanel>();

            foreach (JObject item in data["data"])
            {
                if (item.TryGetValue("menuitem", out JToken itemNameToken) &&
                    item.TryGetValue("menucategory", out JToken categoryToken) &&
                    item.TryGetValue("position", out JToken positionToken) &&
                    item.TryGetValue("itemprice", out JToken itemPriceToken) &&
                    item.TryGetValue("printto", out JToken printToToken) &&
                    item.TryGetValue("foodstamp", out JToken foodStampToken) &&
                    item.TryGetValue("taxable", out JToken taxableToken))
                {
                    string itemName = itemNameToken.ToString();
                    string category = categoryToken.ToString();
                    string position = positionToken.ToString();
                    string itemPrice = itemPriceToken.ToString();
                    string printTo = printToToken.ToString();
                    string foodStamp = foodStampToken.ToString();
                    string taxable = taxableToken.ToString();

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

                    Image itemImage;
                    Image categoryImage;

                    try
                    {
                        itemImage = Image.FromFile(fullItemImagePath);
                        Log("Loaded item image for " + itemName);
                    }
                    catch (Exception)
                    {
                        itemImage = Image.FromFile(Path.Combine(picturesFolder, "image not avail.bmp")); // Load fallback image
                        Log("Failed to load item image for " + itemName + ". Loaded fallback image.");
                    }

                    try
                    {
                        categoryImage = Image.FromFile(fullCategoryImagePath);
                        Log("Loaded category image for " + category);
                    }
                    catch (Exception)
                    {
                        categoryImage = Image.FromFile(Path.Combine(picturesFolder, "image not avail.bmp")); // Load fallback image
                        Log("Failed to load category image for " + category + ". Loaded fallback image.");
                    }

                    string tag = $"{category}|{position}|{itemName}|{itemPrice}|{printTo}|{foodStamp}|{taxable}";

                    PictureBox pictureBox = CreateFormattedPictureBox(itemName, itemImage, itemName);
                    pictureBox.Visible = false;
                    pictureBox.Click += new EventHandler(PictureBox_Click);

                    Label label = new Label
                    {
                        Name = itemName,
                        Text = $"{itemName}",
                        AutoSize = true,
                        Margin = new Padding(10),
                        TextAlign = ContentAlignment.MiddleCenter,
                        Visible = false
                    };

                    if (!categoryControls.ContainsKey(category))
                    {
                        categoryControls[category] = new List<Control>();
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
                            Margin = new Padding(10)
                        };

                        categoryPanel.Controls.Add(categoryPictureBox);
                        categoryPanel.Controls.Add(categoryLabel);
                        panel.Controls.Add(categoryPanel);

                        categoryPanels.Add(categoryPanel);
                    }

                    categoryControls[category].Add(pictureBox);
                    categoryControls[category].Add(label);
                }
                else
                {
                    MessageBox.Show($"Invalid item data in JSON file: {item}");
                    Log($"Invalid item data in JSON file: {item}");
                }
            }

            currentState = State.Category;
            UpdateNavigationButtons(); // Called here to initially set button visibility
            Log("Category picture boxes and panels created.");
        }

        // Event Handlers
        private void PictureBox_Click(object sender, EventArgs e)
        {
            PictureBox pictureBox = sender as PictureBox;
            if (pictureBox != null)
            {
                string itemTag = pictureBox.Tag.ToString();
                Log($"Item {itemTag} clicked.");
                RefreshItem(itemTag);
            }
        }

        private void PreviousButton_Click(object sender, EventArgs e)
        {
            Log("Previous button clicked.");
            switch (currentState)
            {
                case State.Item:
                case State.Modifier:
                    CreateCategoryPictureBoxesAndPanels(jsonPath);
                    break;
            }

            if (currentState == State.Modifier && currentModifierIndex >= 0)
            {
                string modCode = currentModifierCodes[currentModifierIndex];
                var modifierDef = modifierData["data"]
                    .FirstOrDefault(m => m["modcode"] != null && m["modcode"].ToString() == modCode);

                if (modifierDef != null)
                {
                    string modChoiceType = modifierDef["modchoice"]?.ToString() ?? "one";
                    if (modChoiceType == "one")
                    {
                        var modifierDetails = modifierDetailData["data"]
                            .Where(d => d["modcode"] != null && d["modcode"].ToString() == modCode)
                            .ToList();

                        foreach (var detail in modifierDetails)
                        {
                            string detailDesc = detail["description"]?.ToString() ?? "Unknown Detail";
                            if (modifierSelectionState.ContainsKey(detailDesc) && modifierSelectionState[detailDesc])
                            {
                                modifierSelectionState[detailDesc] = false;
                                ListViewItem itemToRemove = selectionListView.Items.Cast<ListViewItem>().FirstOrDefault(item => item.Text == detailDesc);
                                if (itemToRemove != null)
                                {
                                    selectionListView.Items.Remove(itemToRemove);
                                    Log($"Deselected modifier detail: {detailDesc}");
                                }
                            }
                        }
                    }
                }
            }
        }

        private void NextButton_Click(object sender, EventArgs e)
        {
            Log("Next button clicked.");
            DisplayNextModifier();
        }

        // State Transition Methods
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
                foreach (var control in categoryControls[category])
                {
                    if (column >= panel.ColumnCount)
                    {
                        column = 0;
                        row++;
                    }
                    panel.Controls.Add(control, column, row);
                    control.Visible = true;
                    column++;
                }
            }

            previousCategory = category;
            currentState = State.Item;
            UpdateNavigationButtons();
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
            currentState = State.Modifier;
            DisplayNextModifier();
        }

        private void DisplayNextModifier()
        {
            if (currentModifierIndex >= currentModifierCodes.Count)
            {
                Log("No more modifiers to display.");
                nextButton.Visible = false;
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
                string picturesFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "KioskProject");
                string fullImagePath = Path.Combine(picturesFolder, imagePath.ToLower());

                if (!File.Exists(fullImagePath))
                {
                    fullImagePath = Path.Combine(picturesFolder, "image not avail.bmp");
                }

                Image detailImage;
                try
                {
                    detailImage = Image.FromFile(fullImagePath);
                    Log($"Loaded image for modifier detail: {detailDesc}");
                }
                catch (Exception)
                {
                    detailImage = Image.FromFile(Path.Combine(picturesFolder, "image not avail.bmp")); // Load fallback image
                    Log($"Failed to load image for modifier detail: {detailDesc}. Loaded fallback image.");
                }

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

            UpdateNavigationButtons();
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

        // Selection Methods
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
                        string picturesFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "KioskProject");
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

        private void UpdateNavigationButtons()
        {
            // Make the previous button appear after a category is selected
            previousButton.Visible = currentState != State.Category;

            // Make the next button appear when you are on modifiers
            bool shouldShowNextButton = currentState == State.Modifier;

            // Make the next button disappear when the modChoice for a modDef is type "one"
            if (currentState == State.Modifier && currentModifierIndex > 0)
            {
                string modCode = currentModifierCodes[currentModifierIndex - 1];
                var modifierDef = modifierData["data"]
                    .FirstOrDefault(m => m["modcode"] != null && m["modcode"].ToString() == modCode);

                if (modifierDef != null && modifierDef["modchoice"]?.ToString() == "one")
                {
                    shouldShowNextButton = false;
                }
            }

            nextButton.Visible = shouldShowNextButton;
            Log("Updated navigation buttons.");
        }

        // Utility Methods
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
    }
}
