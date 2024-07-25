using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Newtonsoft.Json.Linq;

namespace ThermalPrinterNetworkExample
{
    public partial class Form1 : Form
    {
        private TableLayoutPanel panel;
        private Stack<NavigationEntry> navigationHistory;
        private Button nextButton, previousButton, startOverButton, finishOrderButton, httpCallButton;
        private Dictionary<string, List<FlowLayoutPanel>> categoryControls;
        private List<FlowLayoutPanel> categoryPanels;
        private JObject itemData, modifierData, modifierDetailData;
        private List<string> currentModifierCodes;
        private int currentModifierIndex;
        private string previousCategory, currentScreenType;
        private ListView selectionListView;
        private Dictionary<string, bool> modifierSelectionState;
        private Dictionary<string, Image> imageCache;
        private Label itemCountLabel, totalPriceLabel;
        private readonly string printerIpAddress = "192.168.1.199";
        private readonly int printerPort = 9100;

        public Form1()
        {
            InitializeComponent();
            InitializeForm();
            LoadData();
            CreateCategoryPictureBoxes();
        }

        private void InitializeForm()
        {
            this.WindowState = FormWindowState.Maximized;
            this.BackColor = ColorTranslator.FromHtml("#fbe8af");
            InitializeNavigationButtons();
            InitializeSelectionListView();
            navigationHistory = new Stack<NavigationEntry>();
            modifierSelectionState = new Dictionary<string, bool>();
            imageCache = new Dictionary<string, Image>();
        }

        private void InitializeNavigationButtons()
        {
            int buttonWidth = 150, buttonHeight = 100, buttonSpacing = 10;
            Font buttonFont = new Font("Calibri", 12, FontStyle.Bold);
            startOverButton = CreateNavigationButton("Start Over", buttonWidth, buttonHeight, buttonFont, StartOverButton_Click);
            previousButton = CreateNavigationButton("Previous", buttonWidth, buttonHeight, buttonFont, PreviousButton_Click, false);
            nextButton = CreateNavigationButton("Next", buttonWidth, buttonHeight, buttonFont, NextButton_Click, false);
            finishOrderButton = CreateNavigationButton("Finish Order", buttonWidth, buttonHeight, buttonFont, FinishOrderButton_Click, false); // Updated visibility
            httpCallButton = CreateNavigationButton("Make HTTP Call", buttonWidth, buttonHeight, buttonFont, HttpCallButton_Click, true); // New button

            PositionButtons(buttonWidth, buttonHeight, buttonSpacing);
            this.Resize += (sender, e) => PositionButtons(buttonWidth, buttonHeight, buttonSpacing);
        }

        private Button CreateNavigationButton(string text, int width, int height, Font font, EventHandler onClick, bool visible = true)
        {
            var button = new Button
            {
                Text = text,
                Size = new Size(width, height),
                BackColor = Color.Black,
                ForeColor = Color.White,
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
            finishOrderButton.Location = new Point(startX + 3 * (buttonWidth + buttonSpacing), startY);
            httpCallButton.Location = new Point(startX + 4 * (buttonWidth + buttonSpacing), startY); // New button position
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

            itemCountLabel = new Label { Text = "Total Items: 0", AutoSize = true, Location = new Point(1350, 680) };
            totalPriceLabel = new Label { Text = "Total Price: $0.00", AutoSize = true, Location = new Point(1350, 710) };
            this.Controls.Add(itemCountLabel);
            this.Controls.Add(totalPriceLabel);
        }

        private void LoadData()
        {
            itemData = LoadJsonData("formatted_items.txt");
            modifierData = LoadJsonData("formatted_modifierDef.txt");
            modifierDetailData = LoadJsonData("formatted_modifierDetail.txt");
        }

        private JObject LoadJsonData(string fileName)
        {
            string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), fileName);
            return File.Exists(path) ? JObject.Parse(File.ReadAllText(path)) : null;
        }

        private void CreateCategoryPictureBoxes()
        {
            if (itemData == null) return;

            panel = new TableLayoutPanel { AutoSize = true, ColumnCount = 7, Padding = new Padding(10), Dock = DockStyle.Fill };
            this.Controls.Add(panel);
            categoryControls = new Dictionary<string, List<FlowLayoutPanel>>();
            categoryPanels = new List<FlowLayoutPanel>();

            var categories = new HashSet<string>(itemData["data"].Select(item => item["menucategory"]?.ToString()).Where(c => !string.IsNullOrEmpty(c)));
            foreach (var category in categories)
            {
                string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "KioskProject");
                Image categoryImage = LoadImage(GetImagePath($"{category}.bmp", folder));

                PictureBox categoryPictureBox = CreatePictureBox(category, categoryImage, CategoryPictureBox_Click);
                Label categoryLabel = CreateLabel(category);

                FlowLayoutPanel categoryPanel = CreateFlowLayoutPanel();
                categoryPanel.Controls.Add(categoryPictureBox);
                categoryPanel.Controls.Add(categoryLabel);
                panel.Controls.Add(categoryPanel);

                categoryPanels.Add(categoryPanel);
                categoryControls[category] = new List<FlowLayoutPanel>();
            }
        }

        private PictureBox CreatePictureBox(string name, Image image, EventHandler onClick)
        {
            var pictureBox = new PictureBox
            {
                Name = name,
                Size = new Size(158, 118),
                SizeMode = PictureBoxSizeMode.StretchImage,
                Image = image,
                Margin = new Padding(0, 0, 0, 5),
                Tag = name
            };
            pictureBox.Click += onClick;
            return pictureBox;
        }

        private Label CreateLabel(string text) => new Label { Text = text, AutoSize = true, TextAlign = ContentAlignment.MiddleCenter, Margin = new Padding(0, 5, 0, 0) };

        private Button CreateItemButton(string text, string tag, EventHandler onClick)
        {
            var button = new Button
            {
                Text = text,
                Size = new Size(158, 118),
                BackColor = Color.Black,
                ForeColor = Color.White,
                Tag = tag,
                Padding = new Padding(5)
            };
            button.Click += onClick;
            return button;
        }

        private FlowLayoutPanel CreateFlowLayoutPanel() => new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, AutoSize = true, Margin = new Padding(10) };

        private string GetImagePath(string fileName, string folder) => File.Exists(Path.Combine(folder, fileName.ToLower())) ? Path.Combine(folder, fileName.ToLower()) : Path.Combine(folder, "image not avail.bmp");

        private Image LoadImage(string path)
        {
            if (!imageCache.TryGetValue(path, out var image))
            {
                image = Image.FromFile(path);
                imageCache[path] = image;
            }
            return image;
        }

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
                string itemTag = (string)button.Tag;
                navigationHistory.Push(new NavigationEntry("Category", previousCategory));
                RefreshItem(itemTag);
                DisplayItemModifiers(itemTag);
            }
        }

        private void NextButton_Click(object sender, EventArgs e)
        {
            if (currentScreenType == "Modifier") DisplayNextModifier();
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
                    case "FinalSale": // Handle navigation from the final sale screen
                        if (navigationHistory.Count > 0)
                        {
                            previousScreen = navigationHistory.Pop();
                            if (previousScreen.ScreenType == "Modifier")
                            {
                                currentModifierIndex = currentModifierCodes.Count; // Reset to last modifier index
                                var modCode = currentModifierCodes[currentModifierIndex - 1];
                                ResetModifierSelectionState(modCode); // Reset selection state for "one" type modifiers
                                DisplayModifierDetails(modCode);
                                currentScreenType = "Modifier";
                            }
                            else if (previousScreen.ScreenType == "Item")
                            {
                                RefreshItem(previousScreen.ScreenData);
                                currentScreenType = "Item";
                            }
                            else if (previousScreen.ScreenType == "Category")
                            {
                                RefreshCategory(previousScreen.ScreenData);
                                currentScreenType = "Category";
                            }
                        }
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
            UpdateItemCount();
            UpdateTotalPrice();
        }

        private void ClearOrderButton_Click(object sender, EventArgs e)
        {
            selectionListView.Items.Clear();
            ResetPreviousSelections();
            UpdateItemCount();
            UpdateTotalPrice();
            DisplayMainCategory();
            currentScreenType = "MainCategory";
            UpdateNavigationButtons();
        }

        private void FinishOrderButton_Click(object sender, EventArgs e)
        {
            List<OrderedItem> orderedItems = selectionListView.Items.Cast<ListViewItem>().Select(item => new OrderedItem(item.SubItems[1].Text, int.Parse(item.SubItems[0].Text), item.Tag as List<string> ?? new List<string>(), item.SubItems[2].Text.Replace("$", "").Replace(",", "").Trim())).ToList();

            var receiptBuilder = ExtractReceiptFieldsFromJson(orderedItems);
            if (receiptBuilder == null)
            {
                MessageBox.Show("Failed to extract receipt fields.");
                return;
            }

            // Get the order number from the server
            string orderNumber = HttpService.GetOrderNumber();
            if (string.IsNullOrEmpty(orderNumber))
            {
                MessageBox.Show("Failed to retrieve order number.");
                return;
            }

            using (var printerClient = new PrinterClient(printerIpAddress, printerPort))
            {
                printerClient.Connect();
                receiptBuilder.PrintReceipt(printerClient, orderNumber);
            }

            // Make HTTP call with the order number
            HttpService.MakeHttpCall();

            selectionListView.Items.Clear();
            ResetPreviousSelections();
            UpdateItemCount();
            UpdateTotalPrice();
            DisplayMainCategory();
            currentScreenType = "MainCategory";
            UpdateNavigationButtons();
        }

        private void AddItemButton_Click(object sender, EventArgs e)
        {
            ResetPreviousSelections();
            DisplayMainCategory();
            currentScreenType = "MainCategory";
            UpdateNavigationButtons();
        }

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
                        missingModifiers.Add(modCode);
                }
            }

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

            string modCode = currentModifierCodes[currentModifierIndex++];
            var modifierDef = modifierData["data"].FirstOrDefault(m => m["modcode"]?.ToString() == modCode);
            if (modifierDef != null)
            {
                navigationHistory.Push(new NavigationEntry("Modifier", modCode));
                DisplayModifierDetails(modCode);
            }
            else
                DisplayNextModifier();

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
                Image detailImage = LoadImage(imagePath);

                PictureBox detailPictureBox = CreatePictureBox(detailDesc, detailImage, modChoiceType == "one" ? (EventHandler)((s, e) => ModifierDetailPictureBox_Click_One(s, e, modCode)) : (EventHandler)((s, e) => ModifierDetailPictureBox_Click_Upsale(s, e, modCode)));
                Label detailLabel = CreateLabel(string.IsNullOrEmpty(cost) ? detailDesc : $"{detailDesc} (+${cost})");

                FlowLayoutPanel detailPanel = CreateFlowLayoutPanel();
                detailPanel.Controls.Add(detailPictureBox);
                detailPanel.Controls.Add(detailLabel);

                panel.Controls.Add(detailPanel);
                if (!modifierSelectionState.ContainsKey(detailDesc))
                    modifierSelectionState[detailDesc] = false;

                UpdatePictureBoxSelectionState(detailPictureBox, modifierSelectionState[detailDesc], modCode);
            }

            currentScreenType = "Modifier";
            UpdateNavigationButtons();
        }

        private void DisplayFinalSaleScreen()
        {
            panel.Controls.Clear();
            var finalPanel = CreateFlowLayoutPanel();
            finalPanel.Controls.Add(new Label { Text = "Thank you for your purchase!", Font = new Font("Arial", 24, FontStyle.Bold), AutoSize = true });
            finalPanel.Controls.Add(CreateNavigationButton("Clear Order", 150, 50, new Font("Calibri", 12, FontStyle.Bold), ClearOrderButton_Click));
            finalPanel.Controls.Add(CreateNavigationButton("Add Item", 150, 50, new Font("Calibri", 12, FontStyle.Bold), AddItemButton_Click));
            finalPanel.Controls.Add(finishOrderButton);

            finishOrderButton.Visible = true; // Show finishOrderButton only on the final sale screen

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
                UpdatePictureBoxSelectionState(pictureBox, modifierSelectionState[detailDesc], modCode);
                UpdateSelectionListView(detailDesc, modifierSelectionState[detailDesc], modCode);
                UpdateTotalPrice();
            }
        }

        private void UpdatePictureBoxSelectionState(PictureBox pictureBox, bool isSelected, string modCode)
        {
            var modifierDef = modifierData["data"].FirstOrDefault(m => m["modcode"]?.ToString() == modCode);
            if (modifierDef == null) return;

            string modChoiceType = modifierDef["modchoice"]?.ToString() ?? "one";
            pictureBox.Image = isSelected && modChoiceType == "upsale" ? CreateOverlayImage(pictureBox.Image) : LoadImage(GetImagePath($"{pictureBox.Tag}.bmp", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "KioskProject")));
            pictureBox.BackColor = isSelected && modChoiceType != "upsale" ? Color.Green : Color.Transparent;
        }

        private Image CreateOverlayImage(Image originalImage)
        {
            Bitmap overlayImage = new Bitmap(originalImage);
            using (Graphics g = Graphics.FromImage(overlayImage))
                g.FillRectangle(new SolidBrush(Color.FromArgb(128, Color.Yellow)), new Rectangle(0, 0, overlayImage.Width, overlayImage.Height));
            return overlayImage;
        }

        private void UpdateSelectionListView(string detailDesc, bool isSelected, string modCode)
        {
            var detail = modifierDetailData["data"].FirstOrDefault(d => d["modcode"]?.ToString() == modCode && d["description"]?.ToString() == detailDesc);
            string cost = detail?["cost"]?.ToString() ?? "";

            if (isSelected)
            {
                var listViewItem = new ListViewItem("1") { Tag = modCode };
                listViewItem.SubItems.Add("+" + detailDesc);
                listViewItem.SubItems.Add(cost);
                selectionListView.Items.Add(listViewItem);
            }
            else
            {
                var itemToRemove = selectionListView.Items.Cast<ListViewItem>().FirstOrDefault(item => item.SubItems[1].Text == "+" + detailDesc);
                if (itemToRemove != null) selectionListView.Items.Remove(itemToRemove);
            }
            UpdateTotalPrice();
        }

        private void UpdateItemCount() => itemCountLabel.Text = $"Total Items: {selectionListView.Items.Cast<ListViewItem>().Count(item => !item.SubItems[1].Text.StartsWith("+"))}";

        private void UpdateTotalPrice() => totalPriceLabel.Text = $"Total Price: ${selectionListView.Items.Cast<ListViewItem>().Where(item => !string.IsNullOrEmpty(item.SubItems[2].Text)).Sum(item => decimal.Parse(item.SubItems[2].Text.Replace("$", "").Trim())):F2}";

        private void ResetModifierSelectionState(string modCode)
        {
            var modifierDef = modifierData["data"].FirstOrDefault(m => m["modcode"]?.ToString() == modCode);
            if (modifierDef != null && modifierDef["modchoice"]?.ToString() == "one")
            {
                var modifierDetails = modifierDetailData["data"].Where(d => d["modcode"]?.ToString() == modCode).ToList();
                foreach (var detail in modifierDetails)
                {
                    string detailDesc = detail["description"]?.ToString() ?? "Unknown Detail";
                    modifierSelectionState[detailDesc] = false;
                    var itemToRemove = selectionListView.Items.Cast<ListViewItem>().FirstOrDefault(item => item.SubItems[1].Text == "+" + detailDesc);
                    if (itemToRemove != null) selectionListView.Items.Remove(itemToRemove);
                }
            }
        }

        private void ResetPreviousSelections()
        {
            modifierSelectionState.Clear();
            currentModifierCodes.Clear();
            currentModifierIndex = 0;
        }

        private void DisplayMainCategory()
        {
            panel.Controls.Clear();
            panel.ColumnStyles.Clear();
            panel.RowStyles.Clear();
            panel.ColumnCount = 7;

            foreach (var categoryPanel in categoryPanels)
                panel.Controls.Add(categoryPanel);

            currentScreenType = "MainCategory";
            panel.Update();
            UpdateNavigationButtons();
        }

        private void UpdateNavigationButtons()
        {
            previousButton.Visible = (currentScreenType == "Modifier" && currentModifierIndex > 1) || currentScreenType == "FinalSale";
            nextButton.Visible = currentScreenType == "Modifier" && currentModifierIndex <= currentModifierCodes.Count && (modifierData["data"].FirstOrDefault(m => m["modcode"]?.ToString() == currentModifierCodes[currentModifierIndex - 1]) != null && (modifierData["data"].First(m => m["modcode"].ToString() == currentModifierCodes[currentModifierIndex - 1])["modchoice"].ToString() == "one" || modifierData["data"].First(m => m["modcode"].ToString() == currentModifierCodes[currentModifierIndex - 1])["modchoice"].ToString() == "upsale"));
            finishOrderButton.Visible = currentScreenType == "FinalSale"; // Show finishOrderButton only on the final sale screen
        }

        private ReceiptBuilder ExtractReceiptFieldsFromJson(List<OrderedItem> orderedItems)
        {
            var firstItem = itemData["data"].FirstOrDefault();
            if (firstItem == null) return null;

            string[] storenameParts = firstItem["storename"]?.ToString().Split('_') ?? new string[0];
            string storeName = storenameParts.Length > 0 ? storenameParts[0] : "Store Name";
            string address1 = storenameParts.Length > 1 ? storenameParts[1] : "1234 Main St, Anytown, USA";

            return new ReceiptBuilder("TEMP_PRINTER", storeName, address1, "AnyCity, PA 10001", "215-555-4444", "100", "123456789012", "1", orderedItems);
        }

        public class NavigationEntry
        {
            public string ScreenType { get; }
            public string ScreenData { get; }

            public NavigationEntry(string screenType, string screenData)
            {
                ScreenType = screenType;
                ScreenData = screenData;
            }
        }

        private void HttpCallButton_Click(object sender, EventArgs e)
        {
            HttpService.MakeHttpCall();
        }
    }
}
