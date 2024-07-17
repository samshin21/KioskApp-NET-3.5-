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
        // Form fields
        private TableLayoutPanel panel;
        private Stack<NavigationEntry> navigationHistory;
        private Button nextButton, previousButton, startOverButton;
        private string jsonPath, modifierJsonPath, modifierDetailJsonPath;
        private Dictionary<string, List<FlowLayoutPanel>> categoryControls;
        private List<FlowLayoutPanel> categoryPanels;
        private JObject itemData, modifierData, modifierDetailData;
        private List<string> currentModifierCodes;
        private int currentModifierIndex;
        private string previousCategory, currentScreenType;
        private ListView selectionListView;
        private Dictionary<string, bool> modifierSelectionState;
        private Dictionary<string, Image> imageCache;
        private Label itemCountLabel;
        private Label totalPriceLabel;

        // Constructor
        public Form1()
        {
            InitializeComponent();
            InitializeForm();
            LoadData();
            CreateCategoryPictureBoxes();
        }

        // Initialization methods
        private void InitializeForm()
        {
            this.WindowState = FormWindowState.Maximized;
            this.BackColor = ColorTranslator.FromHtml("#fbe8af");
            InitializeNavigationButtons();
            InitializeSelectionListView();
            navigationHistory = new Stack<NavigationEntry>();
            modifierSelectionState = new Dictionary<string, bool>();
            imageCache = new Dictionary<string, Image>();
            SetJsonPaths();
        }

        private void InitializeNavigationButtons()
        {
            int buttonWidth = 150, buttonHeight = 100, buttonSpacing = 10;
            Color buttonBackColor = Color.Black, buttonForeColor = Color.White;
            Font buttonFont = new Font("Calibri", 12, FontStyle.Bold);

            startOverButton = CreateNavigationButton("Start Over", buttonWidth, buttonHeight, buttonBackColor, buttonForeColor, buttonFont, StartOverButton_Click);
            previousButton = CreateNavigationButton("Previous", buttonWidth, buttonHeight, buttonBackColor, buttonForeColor, buttonFont, PreviousButton_Click, false);
            nextButton = CreateNavigationButton("Next", buttonWidth, buttonHeight, buttonBackColor, buttonForeColor, buttonFont, NextButton_Click, false);

            PositionButtons(buttonWidth, buttonHeight, buttonSpacing);
            this.Resize += (sender, e) => PositionButtons(buttonWidth, buttonHeight, buttonSpacing);
        }

        private Button CreateNavigationButton(string text, int width, int height, Color backColor, Color foreColor, Font font, EventHandler onClick, bool visible = true)
        {
            var button = new Button
            {
                Text = text,
                Size = new Size(width, height),
                BackColor = backColor,
                ForeColor = foreColor,
                Font = font,
                Visible = visible
            };
            button.Click += onClick;
            this.Controls.Add(button);
            return button;
        }

        private void PositionButtons(int buttonWidth, int buttonHeight, int buttonSpacing)
        {
            int startX = 20, startY = 750;
            startOverButton.Location = new Point(startX, startY);
            previousButton.Location = new Point(startX + buttonWidth + buttonSpacing, startY);
            nextButton.Location = new Point(startX + 2 * (buttonWidth + buttonSpacing), startY);
        }

        private void InitializeSelectionListView()
        {
            selectionListView = new ListView
            {
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                Size = new Size(300, 650),
                Location = new Point(1350, 20)
            };
            selectionListView.Columns.Add("Qty", 50);
            selectionListView.Columns.Add("Item", 150);
            selectionListView.Columns.Add("Price", 100);
            this.Controls.Add(selectionListView);

            // Initialize the item count label
            itemCountLabel = new Label
            {
                Text = "Total Items: 0",
                AutoSize = true,
                Location = new Point(1350, 680) // Position it below the ListView
            };
            this.Controls.Add(itemCountLabel);

            // Initialize the total price label
            totalPriceLabel = new Label
            {
                Text = "Total Price: $0.00",
                AutoSize = true,
                Location = new Point(1350, 710) // Position it below the item count label
            };
            this.Controls.Add(totalPriceLabel);
        }

        private void SetJsonPaths()
        {
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            jsonPath = Path.Combine(desktopPath, "formatted_items.txt");
            modifierJsonPath = Path.Combine(desktopPath, "formatted_modifierDef.txt");
            modifierDetailJsonPath = Path.Combine(desktopPath, "formatted_modifierDetail.txt");
        }

        // Data loading methods
        private void LoadData()
        {
            itemData = LoadJsonData(jsonPath, "Item");
            modifierData = LoadJsonData(modifierJsonPath, "Modifier");
            modifierDetailData = LoadJsonData(modifierDetailJsonPath, "Modifier Detail");
        }

        private JObject LoadJsonData(string path, string dataType)
        {
            if (!File.Exists(path))
            {
                MessageBox.Show($"{dataType} JSON file not found: {path}");
                return null;
            }

            try
            {
                string json = File.ReadAllText(path);
                return JObject.Parse(json);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load {dataType} JSON file: {ex.Message}");
                return null;
            }
        }

        // Category and item display methods
        private void CreateCategoryPictureBoxes()
        {
            if (itemData == null) return;

            panel = new TableLayoutPanel
            {
                AutoSize = true,
                ColumnCount = 7,
                Padding = new Padding(10),
                Dock = DockStyle.Fill
            };
            this.Controls.Add(panel);

            categoryControls = new Dictionary<string, List<FlowLayoutPanel>>();
            categoryPanels = new List<FlowLayoutPanel>();

            var categories = new HashSet<string>(itemData["data"].Select(item => item["menucategory"]?.ToString()).Where(c => !string.IsNullOrEmpty(c)));

            foreach (var category in categories)
            {
                string picturesFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "KioskProject");
                Image categoryImage = LoadImage(GetImagePath($"{category}.bmp", picturesFolder), category);

                PictureBox categoryPictureBox = CreatePictureBox(category, categoryImage, category, CategoryPictureBox_Click);
                Label categoryLabel = CreateLabel(category);

                FlowLayoutPanel categoryPanel = CreateFlowLayoutPanel();
                categoryPanel.Controls.Add(categoryPictureBox);
                categoryPanel.Controls.Add(categoryLabel);
                panel.Controls.Add(categoryPanel);

                categoryPanels.Add(categoryPanel);
                categoryControls[category] = new List<FlowLayoutPanel>();

                // Log the loading of category images
                Console.WriteLine($"[CreateCategoryPictureBoxes] {DateTime.Now}: Loaded image for category: {category}, Path: {GetImagePath($"{category}.bmp", picturesFolder)}");
            }
        }

        private PictureBox CreatePictureBox(string name, Image image, string tag, EventHandler onClick)
        {
            var pictureBox = new PictureBox
            {
                Name = name,
                Size = new Size(158, 118),
                SizeMode = PictureBoxSizeMode.StretchImage,
                Image = image,
                Margin = new Padding(0, 0, 0, 5),
                Padding = new Padding(0),
                Tag = tag // Store the image name in the Tag property
            };
            pictureBox.Click += onClick;
            return pictureBox;
        }

        private Label CreateLabel(string text)
        {
            return new Label
            {
                Text = text,
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleCenter,
                Margin = new Padding(0, 5, 0, 0)
            };
        }

        private Button CreateItemButton(string text, string tag, EventHandler onClick)
        {
            var button = new Button
            {
                Text = text,
                Size = new Size(158, 118),
                BackColor = Color.Black,
                ForeColor = Color.White,
                Tag = tag, // Store the image name in the Tag property
                Padding = new Padding(5)
            };
            button.Click += onClick;
            return button;
        }

        private FlowLayoutPanel CreateFlowLayoutPanel()
        {
            return new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown,
                AutoSize = true,
                Margin = new Padding(10)
            };
        }

        private string GetImagePath(string fileName, string folder)
        {
            string fullPath = Path.Combine(folder, fileName.ToLower());
            return File.Exists(fullPath) ? fullPath : Path.Combine(folder, "image not avail.bmp");
        }

        private Image LoadImage(string path, string pictureBoxName)
        {
            if (imageCache.ContainsKey(path))
            {
                Console.WriteLine($"[LoadImage] {DateTime.Now}: Using cached image: {path} | {pictureBoxName}");
                return imageCache[path];
            }

            try
            {
                Image image = Image.FromFile(path);
                imageCache[path] = image;
                Console.WriteLine($"[LoadImage] {DateTime.Now}: Successfully loaded image: {path} | {pictureBoxName}"); // Log successful image load
                return image;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[LoadImage] {DateTime.Now}: Failed to load image: {path}, Error: {ex.Message} | {pictureBoxName}"); // Log image load failure
                string fallbackPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "KioskProject");
                fallbackPath = Path.Combine(fallbackPath, "image not avail.bmp");
                if (!imageCache.ContainsKey(fallbackPath))
                {
                    imageCache[fallbackPath] = Image.FromFile(fallbackPath);
                    Console.WriteLine($"[LoadImage] {DateTime.Now}: Using fallback image: {fallbackPath} | {pictureBoxName}"); // Log fallback image usage
                }
                return imageCache[fallbackPath];
            }
        }

        // Event handlers
        private void CategoryPictureBox_Click(object sender, EventArgs e)
        {
            if (sender is PictureBox pictureBox)
            {
                string category = pictureBox.Tag.ToString();
                navigationHistory.Push(new NavigationEntry("Category", previousCategory));
                RefreshCategory(category);
                currentScreenType = "Item";
                UpdateNavigationButtons();
            }
        }

        private void ItemButton_Click(object sender, EventArgs e)
        {
            if (sender is Button button)
            {
                string itemTag = (string)button.Tag; // Retrieve the image name from the Tag property
                navigationHistory.Push(new NavigationEntry("Category", previousCategory));
                RefreshItem(itemTag);
                DisplayItemModifiers(itemTag);
            }
        }

        private void NextButton_Click(object sender, EventArgs e)
        {
            if (currentScreenType == "Modifier")
            {
                DisplayNextModifier();
            }
        }

        private void PreviousButton_Click(object sender, EventArgs e)
        {
            if (navigationHistory.Count > 0)
            {
                var previousScreen = navigationHistory.Pop();
                switch (currentScreenType)
                {
                    case "Item":
                        RefreshCategory(previousScreen.ScreenData);
                        currentScreenType = "Category";
                        break;
                    case "Modifier":
                        if (currentModifierIndex > 1)
                        {
                            currentModifierIndex--;
                            var modCode = currentModifierCodes[currentModifierIndex - 1];
                            ResetModifierSelectionState(modCode);
                            DisplayModifierDetails(modCode);
                        }
                        else if (navigationHistory.Count > 0)
                        {
                            previousScreen = navigationHistory.Pop();
                            if (previousScreen.ScreenType == "Item")
                            {
                                RefreshItem(previousScreen.ScreenData);
                                currentScreenType = "Item";
                            }
                            else
                            {
                                RefreshCategory(previousScreen.ScreenData);
                                currentScreenType = "Category";
                            }
                        }
                        break;
                    case "Category":
                        DisplayMainCategory();
                        currentScreenType = "MainCategory";
                        break;
                }
            }
            UpdateNavigationButtons();
        }

        private void StartOverButton_Click(object sender, EventArgs e)
        {
            navigationHistory.Clear();
            selectionListView.Items.Clear();
            modifierSelectionState.Clear();
            DisplayMainCategory();
            UpdateItemCount(); // Update item count when starting over
            UpdateTotalPrice(); // Update total price when starting over
        }

        private void ClearOrderButton_Click(object sender, EventArgs e)
        {
            // Clear the ListView
            selectionListView.Items.Clear();

            // Reset previous selections
            ResetPreviousSelections();

            // Update the item count and total price
            UpdateItemCount();
            UpdateTotalPrice();

            // Navigate back to the category screen
            DisplayMainCategory();
            currentScreenType = "MainCategory";
            UpdateNavigationButtons();
        }

        private void FinishOrderButton_Click(object sender, EventArgs e)
        {
            // Write the order to a .txt file
            WriteOrderToFile();

            // Clear the ListView
            selectionListView.Items.Clear();

            // Reset previous selections
            ResetPreviousSelections();

            // Update the item count and total price
            UpdateItemCount();
            UpdateTotalPrice();

            // Navigate back to the category screen
            DisplayMainCategory();
            currentScreenType = "MainCategory";
            UpdateNavigationButtons();
        }

        private void AddItemButton_Click(object sender, EventArgs e)
        {
            // Reset previous selections
            ResetPreviousSelections();

            // Navigate back to the category screen without clearing the ListView
            DisplayMainCategory();
            currentScreenType = "MainCategory";
            UpdateNavigationButtons();
        }

        private void WriteOrderToFile()
        {
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string orderFilePath = Path.Combine(desktopPath, "Order.txt");

            using (StreamWriter writer = new StreamWriter(orderFilePath, true))
            {
                writer.WriteLine("Order Summary:");
                writer.WriteLine("--------------");

                foreach (ListViewItem item in selectionListView.Items)
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

        // Refresh methods
        private void RefreshCategory(string category)
        {
            if (string.IsNullOrEmpty(category) || !categoryControls.ContainsKey(category)) return;

            panel.Controls.Clear();
            panel.ColumnStyles.Clear();
            panel.RowStyles.Clear();
            panel.ColumnCount = 7;

            var items = itemData["data"].Where(item => item["menucategory"]?.ToString() == category);

            foreach (var item in items)
            {
                string itemName = item["menuitem"]?.ToString();
                if (string.IsNullOrEmpty(itemName)) continue;

                Button itemButton = CreateItemButton(itemName, itemName, ItemButton_Click);

                FlowLayoutPanel itemPanel = CreateFlowLayoutPanel();
                itemPanel.Controls.Add(itemButton);

                categoryControls[category].Add(itemPanel);
                panel.Controls.Add(itemPanel);

                // Log the loading of item buttons
                Console.WriteLine($"[RefreshCategory] {DateTime.Now}: Loaded button for item: {itemName}");
            }

            previousCategory = category;
            currentScreenType = "Category";
            panel.Update();
            UpdateNavigationButtons();
        }

        private void RefreshItem(string itemTag)
        {
            if (currentScreenType == "Modifier") return;

            panel.Controls.Clear();
            panel.ColumnStyles.Clear();
            panel.RowStyles.Clear();
            panel.ColumnCount = 7;

            var item = itemData["data"].FirstOrDefault(m => m["menuitem"]?.ToString() == itemTag);

            if (item != null)
            {
                string itemName = item["menuitem"].ToString();
                string itemPrice = item["itemprice"].ToString();

                Button itemButton = CreateItemButton(itemName, itemName, null);

                FlowLayoutPanel itemPanel = CreateFlowLayoutPanel();
                itemPanel.Controls.Add(itemButton);

                panel.Controls.Add(itemPanel, 0, 0);

                AddItemToSelectionListView(itemName, itemPrice);

                // Log the loading of item buttons
                Console.WriteLine($"[RefreshItem] {DateTime.Now}: Loaded button for item: {itemName}");
            }
        }

        private void AddItemToSelectionListView(string itemName, string itemPrice)
        {
            var listViewItem = new ListViewItem("1");
            listViewItem.SubItems.Add(itemName);
            listViewItem.SubItems.Add(itemPrice);
            selectionListView.Items.Add(listViewItem);

            UpdateItemCount();
            UpdateTotalPrice();
        }

        // Modifier display methods
        private void DisplayItemModifiers(string itemTag)
        {
            var item = itemData["data"].FirstOrDefault(m => m["menuitem"]?.ToString() == itemTag);
            if (item == null) return;

            string[] modifierSections = { "aa", "bb", "cc", "dd", "ee", "ff", "gg", "hh", "ii", "jj", "kk", "ll" };
            currentModifierCodes = new List<string>();
            List<string> missingModifiers = new List<string>();

            foreach (var section in modifierSections)
            {
                if (item[section] != null && item[section].Type == JTokenType.String && !string.IsNullOrEmpty(item[section].ToString()))
                {
                    string modCode = item[section].ToString();
                    currentModifierCodes.Add(modCode);
                    if (!modifierData["data"].Any(m => m["modcode"]?.ToString() == modCode))
                    {
                        missingModifiers.Add(modCode);
                    }
                }
            }

            // Log the modifiers to the console
            Console.WriteLine($"[DisplayItemModifiers] {DateTime.Now}: Modifiers for item '{itemTag}': {string.Join(", ", currentModifierCodes.ToArray())}");

            if (missingModifiers.Count > 0)
            {
                string missingModifiersMessage = $"Missing Modifiers for item '{itemTag}':\n" + string.Join("\n", missingModifiers.ToArray());
                MessageBox.Show(missingModifiersMessage, "Missing Modifiers", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            currentModifierIndex = 0;
            previousCategory = item["menucategory"].ToString();
            navigationHistory.Push(new NavigationEntry("Item", itemTag));

            DisplayNextModifier();
        }

        private void DisplayNextModifier()
        {
            if (currentModifierIndex >= currentModifierCodes.Count)
            {
                DisplayFinalSaleScreen();
                return;
            }

            string modCode = currentModifierCodes[currentModifierIndex];
            currentModifierIndex++;

            var modifierDef = modifierData["data"].FirstOrDefault(m => m["modcode"]?.ToString() == modCode);

            if (modifierDef != null)
            {
                navigationHistory.Push(new NavigationEntry("Modifier", modCode));
                DisplayModifierDetails(modCode);
            }
            else
            {
                DisplayNextModifier();
            }

            currentScreenType = "Modifier";
            UpdateNavigationButtons();
        }

        private void DisplayModifierDetails(string modCode)
        {
            panel.Controls.Clear();

            var modifierDef = modifierData["data"].FirstOrDefault(m => m["modcode"]?.ToString() == modCode);
            if (modifierDef == null) return;

            string modChoiceType = modifierDef["modchoice"]?.ToString() ?? "one";
            var modifierDetails = modifierDetailData["data"].Where(d => d["modcode"]?.ToString() == modCode).ToList();

            foreach (var detail in modifierDetails)
            {
                string detailDesc = detail["description"]?.ToString() ?? "Unknown Detail";
                string cost = detail["cost"]?.ToString() ?? "";
                string imagePath = GetImagePath(detail["location"]?.ToString() ?? "image not avail.bmp", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "KioskProject"));
                Image detailImage = LoadImage(imagePath, detailDesc);

                PictureBox detailPictureBox = CreatePictureBox(detailDesc, detailImage, detailDesc, modChoiceType == "one" ? (EventHandler)((s, e) => ModifierDetailPictureBox_Click_One(s, e, modCode)) : (EventHandler)((s, e) => ModifierDetailPictureBox_Click_Upsale(s, e, modCode)));
                Label detailLabel = CreateLabel(string.IsNullOrEmpty(cost) ? detailDesc : $"{detailDesc} (+${cost})");

                FlowLayoutPanel detailPanel = CreateFlowLayoutPanel();
                detailPanel.Controls.Add(detailPictureBox);
                detailPanel.Controls.Add(detailLabel);

                panel.Controls.Add(detailPanel);

                if (!modifierSelectionState.ContainsKey(detailDesc))
                {
                    modifierSelectionState[detailDesc] = false;
                }

                UpdatePictureBoxSelectionState(detailPictureBox, modifierSelectionState[detailDesc], detailDesc, modCode);
            }

            currentScreenType = "Modifier";
            UpdateNavigationButtons();
        }

        private void DisplayFinalSaleScreen()
        {
            panel.Controls.Clear();

            Label finalMessage = new Label
            {
                Text = "Thank you for your purchase!",
                Font = new Font("Arial", 24, FontStyle.Bold),
                AutoSize = true
            };

            Button clearOrderButton = new Button
            {
                Text = "Clear Order",
                Size = new Size(150, 50),
                BackColor = Color.Black,
                ForeColor = Color.White,
                Font = new Font("Calibri", 12, FontStyle.Bold)
            };
            clearOrderButton.Click += ClearOrderButton_Click;

            // Add Item button
            Button addItemButton = new Button
            {
                Text = "Add Item",
                Size = new Size(150, 50),
                BackColor = Color.Black,
                ForeColor = Color.White,
                Font = new Font("Calibri", 12, FontStyle.Bold)
            };
            addItemButton.Click += AddItemButton_Click;

            // Finish Order button
            Button finishOrderButton = new Button
            {
                Text = "Finish Order",
                Size = new Size(150, 50),
                BackColor = Color.Black,
                ForeColor = Color.White,
                Font = new Font("Calibri", 12, FontStyle.Bold)
            };
            finishOrderButton.Click += FinishOrderButton_Click;

            FlowLayoutPanel finalPanel = CreateFlowLayoutPanel();
            finalPanel.Controls.Add(finalMessage);
            finalPanel.Controls.Add(clearOrderButton);
            finalPanel.Controls.Add(addItemButton);
            finalPanel.Controls.Add(finishOrderButton); // Add the button to the panel
            panel.Controls.Add(finalPanel);

            currentScreenType = "FinalSale";
            UpdateNavigationButtons();
        }

        private void ModifierDetailPictureBox_Click_One(object sender, EventArgs e, string modCode)
        {
            if (sender is PictureBox pictureBox)
            {
                string detailDesc = pictureBox.Tag.ToString();
                ToggleModifierSelection(pictureBox, detailDesc, modCode);
                DisplayNextModifier();
            }
        }

        private void ModifierDetailPictureBox_Click_Upsale(object sender, EventArgs e, string modCode)
        {
            if (sender is PictureBox pictureBox)
            {
                string detailDesc = pictureBox.Tag.ToString();
                ToggleModifierSelection(pictureBox, detailDesc, modCode);
            }
        }

        private void ToggleModifierSelection(PictureBox pictureBox, string detailDesc, string modCode)
        {
            if (modifierSelectionState.ContainsKey(detailDesc))
            {
                modifierSelectionState[detailDesc] = !modifierSelectionState[detailDesc];
                UpdatePictureBoxSelectionState(pictureBox, modifierSelectionState[detailDesc], detailDesc, modCode);
                UpdateSelectionListView(detailDesc, modifierSelectionState[detailDesc], modCode);
                UpdateTotalPrice();
            }
        }

        private void UpdatePictureBoxSelectionState(PictureBox pictureBox, bool isSelected, string detailDesc, string modCode)
        {
            var modifierDef = modifierData["data"].FirstOrDefault(m => m["modcode"]?.ToString() == modCode);
            if (modifierDef == null) return;

            string modChoiceType = modifierDef["modchoice"]?.ToString() ?? "one";
            if (modChoiceType == "upsale")
            {
                pictureBox.Image = isSelected ? CreateOverlayImage(pictureBox.Image) : ReloadImage((string)pictureBox.Tag);
            }
            else
            {
                pictureBox.BackColor = isSelected ? Color.Green : Color.Transparent;
            }
        }

        private Image CreateOverlayImage(Image originalImage)
        {
            Bitmap overlayImage = new Bitmap(originalImage);
            using (Graphics g = Graphics.FromImage(overlayImage))
            {
                using (Brush brush = new SolidBrush(Color.FromArgb(128, Color.Yellow)))
                {
                    g.FillRectangle(brush, new Rectangle(0, 0, overlayImage.Width, overlayImage.Height));
                }
            }
            return overlayImage;
        }

        private Image ReloadImage(string imageName)
        {
            string imagePath = GetImagePath($"{imageName}.bmp", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "KioskProject"));
            return LoadImage(imagePath, imageName);
        }

        private void UpdateSelectionListView(string detailDesc, bool isSelected, string modCode)
        {
            var detail = modifierDetailData["data"].FirstOrDefault(d => d["modcode"]?.ToString() == modCode && d["description"]?.ToString() == detailDesc);
            string cost = detail?["cost"]?.ToString() ?? "";

            if (isSelected)
            {
                var listViewItem = new ListViewItem("1");
                listViewItem.SubItems.Add("+" + detailDesc); // Add a "+" before the modifier name
                listViewItem.SubItems.Add(cost);
                selectionListView.Items.Add(listViewItem);
            }
            else
            {
                var itemToRemove = selectionListView.Items.Cast<ListViewItem>().FirstOrDefault(item => item.SubItems[1].Text == "+" + detailDesc);
                if (itemToRemove != null)
                {
                    selectionListView.Items.Remove(itemToRemove);
                }
            }

            UpdateTotalPrice();
        }

        private void UpdateItemCount()
        {
            int itemCount = selectionListView.Items.Cast<ListViewItem>().Count(item => !item.SubItems[1].Text.StartsWith("+"));
            itemCountLabel.Text = $"Total Items: {itemCount}";
        }

        private void UpdateTotalPrice()
        {
            decimal totalPrice = selectionListView.Items.Cast<ListViewItem>()
                .Where(item => !string.IsNullOrEmpty(item.SubItems[2].Text))
                .Sum(item => decimal.Parse(item.SubItems[2].Text.Replace("$", "").Trim()));

            totalPriceLabel.Text = $"Total Price: ${totalPrice:F2}";
        }

        private void ResetModifierSelectionState(string modCode)
        {
            var modifierDef = modifierData["data"].FirstOrDefault(m => m["modcode"]?.ToString() == modCode);
            if (modifierDef != null && modifierDef["modchoice"]?.ToString() == "one")
            {
                var modifierDetails = modifierDetailData["data"].Where(d => d["modcode"]?.ToString() == modCode).ToList();
                foreach (var detail in modifierDetails)
                {
                    string detailDesc = detail["description"]?.ToString() ?? "Unknown Detail";
                    if (modifierSelectionState.ContainsKey(detailDesc))
                    {
                        modifierSelectionState[detailDesc] = false;
                    }

                    var itemToRemove = selectionListView.Items.Cast<ListViewItem>().FirstOrDefault(item => item.SubItems[1].Text == detailDesc);
                    if (itemToRemove != null)
                    {
                        selectionListView.Items.Remove(itemToRemove);
                    }
                }
            }
        }

        private void ResetPreviousSelections()
        {
            // Clear the modifier selection state
            modifierSelectionState.Clear();

            // Reset any other necessary states here
            currentModifierCodes.Clear();
            currentModifierIndex = 0;
        }

        private void DisplayMainCategory()
        {
            panel.Controls.Clear();
            panel.ColumnStyles.Clear();
            panel.RowStyles.Clear();
            panel.ColumnCount = 7;

            int column = 0, row = 0;
            foreach (var categoryPanel in categoryPanels)
            {
                if (column >= panel.ColumnCount)
                {
                    column = 0;
                    row++;
                }
                panel.Controls.Add(categoryPanel, column, row);
                column++;
            }

            currentScreenType = "MainCategory";
            panel.Update();
            UpdateNavigationButtons();
        }

        private void UpdateNavigationButtons()
        {
            bool previousButtonVisible = (currentScreenType == "Modifier" && currentModifierIndex > 1) || currentScreenType == "FinalSale";
            bool nextButtonVisible = false;

            if (currentScreenType == "Modifier" && currentModifierIndex <= currentModifierCodes.Count)
            {
                var modChoiceType = modifierData["data"].FirstOrDefault(m => m["modcode"]?.ToString() == currentModifierCodes[currentModifierIndex - 1])?["modchoice"]?.ToString();
                if (modChoiceType == "one" || modChoiceType == "upsale")
                {
                    nextButtonVisible = true;
                }
            }

            previousButton.Visible = previousButtonVisible;
            nextButton.Visible = nextButtonVisible;
        }
    }

    // NavigationEntry class
    public class NavigationEntry
    {
        public string ScreenType { get; set; }
        public string ScreenData { get; set; }

        public NavigationEntry(string screenType, string screenData)
        {
            ScreenType = screenType;
            ScreenData = screenData;
        }
    }
}
