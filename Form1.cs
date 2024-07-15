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
        private Stack<NavigationEntry> navigationHistory;
        private Button nextButton;
        private Button previousButton;
        private Button startOverButton;
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
        private Dictionary<string, bool> modifierSelectionState;
        private string currentScreenType;

        public Form1()
        {
            InitializeComponent();
            InitializeForm();
            LoadData();
            CreateCategoryPictureBoxesAndPanels();
        }

        private void InitializeForm()
        {
            this.WindowState = FormWindowState.Maximized;
            this.BackColor = ColorTranslator.FromHtml("#fbe8af");
            InitializeNavigationButtons();
            InitializeSelectionListView();
            navigationHistory = new Stack<NavigationEntry>();
            modifierSelectionState = new Dictionary<string, bool>();
            SetJsonPaths();
        }

        private void SetJsonPaths()
        {
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            jsonPath = Path.Combine(desktopPath, "formatted_items.txt");
            modifierJsonPath = Path.Combine(desktopPath, "formatted_modifierDef.txt");
            modifierDetailJsonPath = Path.Combine(desktopPath, "formatted_modifierDetail.txt");
        }

        private void InitializeNavigationButtons()
        {
            int buttonWidth = 150;
            int buttonHeight = 100;
            int buttonSpacing = 10;
            Color buttonBackColor = Color.Black;
            Color buttonForeColor = Color.White;
            Font buttonFont = new Font("Calibri", 12, FontStyle.Bold);

            startOverButton = CreateButton("Start Over", buttonWidth, buttonHeight, buttonBackColor, buttonForeColor, buttonFont, StartOverButton_Click);
            previousButton = CreateButton("Previous", buttonWidth, buttonHeight, buttonBackColor, buttonForeColor, buttonFont, PreviousButton_Click, false);
            nextButton = CreateButton("Next", buttonWidth, buttonHeight, buttonBackColor, buttonForeColor, buttonFont, NextButton_Click, true);

            PositionButtons(buttonWidth, buttonHeight, buttonSpacing);
            this.Resize += (sender, e) => PositionButtons(buttonWidth, buttonHeight, buttonSpacing);
        }

        private Button CreateButton(string text, int width, int height, Color backColor, Color foreColor, Font font, EventHandler onClick, bool visible = true)
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
            int startX = 20;
            int startY = 750;
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
        }

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

        private void CreateCategoryPictureBoxesAndPanels()
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

            foreach (JObject item in itemData["data"])
            {
                string itemName = item["menuitem"]?.ToString();
                string category = item["menucategory"]?.ToString();

                if (string.IsNullOrEmpty(itemName) || string.IsNullOrEmpty(category))
                {
                    MessageBox.Show($"Invalid item data in JSON file: {item}");
                    continue;
                }

                string picturesFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "KioskProject");
                Image itemImage = LoadImage(GetImagePath($"{itemName}.bmp", picturesFolder));
                Image categoryImage = LoadImage(GetImagePath($"{category}.bmp", picturesFolder));

                PictureBox itemPictureBox = CreatePictureBox(itemName, itemImage, itemName, PictureBox_Click);
                Label itemLabel = CreateLabel(itemName);

                FlowLayoutPanel itemPanel = CreateFlowLayoutPanel();
                itemPanel.Controls.Add(itemPictureBox);
                itemPanel.Controls.Add(itemLabel);

                if (!categoryControls.ContainsKey(category))
                {
                    categoryControls[category] = new List<FlowLayoutPanel>();

                    PictureBox categoryPictureBox = CreatePictureBox(category, categoryImage, category, CategoryPictureBox_Click);
                    Label categoryLabel = CreateLabel(category);

                    FlowLayoutPanel categoryPanel = CreateFlowLayoutPanel();
                    categoryPanel.Controls.Add(categoryPictureBox);
                    categoryPanel.Controls.Add(categoryLabel);
                    panel.Controls.Add(categoryPanel);

                    categoryPanels.Add(categoryPanel);
                }

                categoryControls[category].Add(itemPanel);
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
                Tag = tag
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

        private Image LoadImage(string path)
        {
            try
            {
                return Image.FromFile(path);
            }
            catch (Exception)
            {
                string fallbackPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "KioskProject");
                fallbackPath = Path.Combine(fallbackPath, "image not avail.bmp");
                return Image.FromFile(fallbackPath);
            }
        }

        private void PictureBox_Click(object sender, EventArgs e)
        {
            if (sender is PictureBox pictureBox)
            {
                string itemTag = pictureBox.Tag.ToString();
                navigationHistory.Push(new NavigationEntry("Category", previousCategory));
                RefreshItem(itemTag);
                DisplayItemModifiers(itemTag);
            }
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

        private void RefreshCategory(string category)
        {
            if (string.IsNullOrEmpty(category) || !categoryControls.ContainsKey(category)) return;

            panel.Controls.Clear();
            panel.ColumnStyles.Clear();
            panel.RowStyles.Clear();
            panel.ColumnCount = 7;

            int column = 0, row = 0;
            foreach (var flowPanel in categoryControls[category])
            {
                if (column >= panel.ColumnCount)
                {
                    column = 0;
                    row++;
                }
                panel.Controls.Add(flowPanel, column, row);
                column++;
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

                string picturesFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "KioskProject");
                Image itemImage = LoadImage(GetImagePath($"{itemName}.bmp", picturesFolder));

                PictureBox itemPictureBox = CreatePictureBox(itemName, itemImage, itemName, null);
                Label itemLabel = CreateLabel(itemName);

                FlowLayoutPanel itemPanel = CreateFlowLayoutPanel();
                itemPanel.Controls.Add(itemPictureBox);
                itemPanel.Controls.Add(itemLabel);

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
        }

        private void DisplayItemModifiers(string itemTag)
        {
            var item = itemData["data"].FirstOrDefault(m => m["menuitem"]?.ToString() == itemTag);
            if (item == null) return;

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
                Image detailImage = LoadImage(imagePath);

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
        }

        private void DisplayFinalSaleScreen()
        {
            panel.Controls.Clear();

            Label finalMessage = CreateLabel("Thank you for your purchase!");
            finalMessage.Font = new Font("Arial", 24, FontStyle.Bold);

            FlowLayoutPanel finalPanel = CreateFlowLayoutPanel();
            finalPanel.Controls.Add(finalMessage);
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
            }
        }

        private void UpdatePictureBoxSelectionState(PictureBox pictureBox, bool isSelected, string detailDesc, string modCode)
        {
            var modifierDef = modifierData["data"].FirstOrDefault(m => m["modcode"]?.ToString() == modCode);
            if (modifierDef == null) return;

            string modChoiceType = modifierDef["modchoice"]?.ToString() ?? "one";
            if (modChoiceType == "upsale")
            {
                pictureBox.Image = isSelected ? CreateOverlayImage(pictureBox.Image) : ReloadImage(detailDesc);
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

        private Image ReloadImage(string detailDesc)
        {
            string imagePath = GetImagePath($"{detailDesc}.bmp", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "KioskProject"));
            return LoadImage(imagePath);
        }

        private void UpdateSelectionListView(string detailDesc, bool isSelected, string modCode)
        {
            var detail = modifierDetailData["data"].FirstOrDefault(d => d["modcode"]?.ToString() == modCode && d["description"]?.ToString() == detailDesc);
            string cost = detail?["cost"]?.ToString() ?? "";

            if (isSelected)
            {
                var listViewItem = new ListViewItem("1");
                listViewItem.SubItems.Add(detailDesc);
                listViewItem.SubItems.Add(cost);
                selectionListView.Items.Add(listViewItem);
            }
            else
            {
                var itemToRemove = selectionListView.Items.Cast<ListViewItem>().FirstOrDefault(item => item.SubItems[1].Text == detailDesc);
                if (itemToRemove != null)
                {
                    selectionListView.Items.Remove(itemToRemove);
                }
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
            if (currentScreenType == "FinalSale" && currentModifierIndex > 0 && currentModifierIndex <= currentModifierCodes.Count)
            {
                currentModifierIndex--;
                var modCode = currentModifierCodes[currentModifierIndex];
                DisplayModifierDetails(modCode);
                return;
            }

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
            bool nextButtonVisible = currentScreenType == "Modifier" && currentModifierIndex < currentModifierCodes.Count && modifierData["data"].FirstOrDefault(m => m["modcode"]?.ToString() == currentModifierCodes[currentModifierIndex - 1])?["modchoice"]?.ToString() == "upsale";

            previousButton.Visible = previousButtonVisible;
            nextButton.Visible = nextButtonVisible;
        }
    }

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
