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
        private string storeName = "demostore1_123MainStr";
        private Dictionary<string, bool> modifierSelectionState;
        private string currentScreenType;

        public Form1()
        {
            InitializeComponent();
            InitializeNavigationButtons();
            navigationHistory = new Stack<NavigationEntry>();
            this.WindowState = FormWindowState.Maximized;
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            jsonPath = Path.Combine(desktopPath, "formatted_items.txt");
            modifierJsonPath = Path.Combine(desktopPath, "formatted_modifierDef.txt");
            modifierDetailJsonPath = Path.Combine(desktopPath, "formatted_modifierDetail.txt");
            InitializeSelectionListView();
            LoadData();
            CreateCategoryPictureBoxesAndPanels();
            modifierSelectionState = new Dictionary<string, bool>();

            // Set the background color to #fbe8af
            this.BackColor = ColorTranslator.FromHtml("#fbe8af");

            // Log the items and modifier definitions
            LogItemsAndModifierDefs();
        }

        private void InitializeNavigationButtons()
        {
            // Button dimensions
            int buttonWidth = 150;
            int buttonHeight = 100;
            int buttonSpacing = 10;

            // Button design
            Color buttonBackColor = Color.Black; // Set background color to black
            Color buttonForeColor = Color.White; // Set font color to white
            Font buttonFont = new Font("Calibri", 12, FontStyle.Bold);

            // Initialize the "Start Over" button
            startOverButton = new Button
            {
                Text = "Start Over",
                Size = new Size(buttonWidth, buttonHeight),
                BackColor = buttonBackColor,
                ForeColor = buttonForeColor,
                Font = buttonFont
            };
            startOverButton.Click += StartOverButton_Click;
            this.Controls.Add(startOverButton);

            // Initialize the "Previous" button
            previousButton = new Button
            {
                Text = "Previous",
                Size = new Size(buttonWidth, buttonHeight),
                BackColor = buttonBackColor,
                ForeColor = buttonForeColor,
                Font = buttonFont,
                Visible = false
            };
            previousButton.Click += PreviousButton_Click;
            this.Controls.Add(previousButton);

            // Initialize the "Next" button
            nextButton = new Button
            {
                Text = "Next",
                Size = new Size(buttonWidth, buttonHeight),
                BackColor = buttonBackColor,
                ForeColor = buttonForeColor,
                Font = buttonFont,
                Visible = true // Ensure the nextButton is always visible
            };
            nextButton.Click += NextButton_Click;
            this.Controls.Add(nextButton);

            // Position buttons on the form
            PositionButtons(buttonWidth, buttonHeight, buttonSpacing);

            // Reposition buttons on form resize
            this.Resize += (sender, e) => PositionButtons(buttonWidth, buttonHeight, buttonSpacing);
        }

        // Method to position the buttons on the form
        private void PositionButtons(int buttonWidth, int buttonHeight, int buttonSpacing)
        {
            // Calculate total width of all buttons including spacing
            int totalButtonWidth = 3 * buttonWidth + 2 * buttonSpacing;
            // Calculate the starting X position for centering the buttons
            int startX = 20;
            // Set the Y position for the buttons at the bottom of the form
            int startY = 750;

            // Set locations for each button
            startOverButton.Location = new Point(startX, startY);
            previousButton.Location = new Point(startX + buttonWidth + buttonSpacing, startY);
            nextButton.Location = new Point(startX + 2 * (buttonWidth + buttonSpacing), startY);
        }

        private void InitializeSelectionListView()
        {
            // Initialize the ListView
            selectionListView = new ListView
            {
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                Size = new Size(300, 650), // Set the size of the ListView
                Location = new Point(1350, 20) // Position it 10 pixels from the right edge and top
            };
            selectionListView.Columns.Add("Qty", 50);
            selectionListView.Columns.Add("Item", 150);
            selectionListView.Columns.Add("Price", 100);

            // Add the ListView to the form's controls
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

        private PictureBox CreateFormattedPictureBox(string name, Image image, string tag)
        {
            return new PictureBox
            {
                Name = name,
                Size = new Size(158, 118),
                SizeMode = PictureBoxSizeMode.StretchImage,
                Image = image,
                Margin = new Padding(0, 0, 0, 5),
                Padding = new Padding(0),
                Tag = tag
            };
        }

        private void CreateCategoryPictureBoxesAndPanels()
        {
            // Check if item data is loaded
            if (itemData == null) return;

            // Initialize TableLayoutPanel
            panel = new TableLayoutPanel
            {
                AutoSize = true,
                ColumnCount = 7,
                Padding = new Padding(10),
                Dock = DockStyle.Fill
            };
            this.Controls.Add(panel);

            // Initialize dictionaries and lists to hold category controls and panels
            categoryControls = new Dictionary<string, List<FlowLayoutPanel>>();
            categoryPanels = new List<FlowLayoutPanel>();

            // Loop through each item in the item data
            foreach (JObject item in itemData["data"])
            {
                // Check for required fields in the item
                if (!item.TryGetValue("menuitem", out JToken itemNameToken) ||
                    !item.TryGetValue("menucategory", out JToken categoryToken))
                {
                    MessageBox.Show($"Invalid item data in JSON file: {item}");
                    continue; // Skip to the next item if data is invalid
                }

                // Extract item name and category from JSON data
                string itemName = itemNameToken.ToString();
                string category = categoryToken.ToString();

                // Define the folder path for images
                string picturesFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "KioskProject");

                // Load item and category images
                Image itemImage = LoadImage(GetImagePath($"{itemName}.bmp", picturesFolder), itemName);
                Image categoryImage = LoadImage(GetImagePath($"{category}.bmp", picturesFolder), category);

                // Create picture box for the item
                PictureBox pictureBox = CreateFormattedPictureBox(itemName, itemImage, itemName);
                pictureBox.Visible = true;
                pictureBox.Click += PictureBox_Click;

                // Create label for the item
                Label label = new Label
                {
                    Name = itemName,
                    Text = itemName,
                    AutoSize = true,
                    TextAlign = ContentAlignment.MiddleCenter,
                    Visible = true,
                    Margin = new Padding(0, 5, 0, 0)
                };

                // Create flow layout panel for the item
                FlowLayoutPanel flowPanel = new FlowLayoutPanel
                {
                    FlowDirection = FlowDirection.TopDown,
                    AutoSize = true,
                    Margin = new Padding(10),
                    Visible = true
                };
                flowPanel.Controls.Add(pictureBox);
                flowPanel.Controls.Add(label);

                // Check if the category already exists in the dictionary
                if (!categoryControls.ContainsKey(category))
                {
                    categoryControls[category] = new List<FlowLayoutPanel>();

                    // Create picture box for the category
                    PictureBox categoryPictureBox = CreateFormattedPictureBox(category, categoryImage, category);
                    categoryPictureBox.Visible = true;
                    categoryPictureBox.Click += (sender, e) =>
                    {
                        navigationHistory.Push(new NavigationEntry("Category", previousCategory));
                        RefreshCategory(category);
                        UpdateNavigationButtons();
                    };

                    // Create label for the category
                    Label categoryLabel = new Label
                    {
                        Text = category,
                        AutoSize = true,
                        TextAlign = ContentAlignment.MiddleCenter,
                        Margin = new Padding(0, 5, 0, 0)
                    };

                    // Create flow layout panel for the category
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

                // Add the item flow panel to the corresponding category
                categoryControls[category].Add(flowPanel);
            }
        }

        // Helper method to get the image path
        private string GetImagePath(string fileName, string folder)
        {
            string fullPath = Path.Combine(folder, fileName.ToLower());
            return File.Exists(fullPath) ? fullPath : Path.Combine(folder, "image not avail.bmp");
        }

        private void PictureBox_Click(object sender, EventArgs e)
        {
            PictureBox pictureBox = sender as PictureBox;
            if (pictureBox != null)
            {
                string itemTag = pictureBox.Tag.ToString();
                Console.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss.fff")}] Item clicked: {itemTag}");
                navigationHistory.Push(new NavigationEntry("Category", previousCategory));
                LogAssociatedModifiers(itemTag);
                RefreshItem(itemTag);
            }
        }

        private void RefreshCategory(string category)
        {
            // Check if the category is valid
            if (category == null || !categoryControls.ContainsKey(category)) return;

            // Clear the current panel content and styles
            panel.Controls.Clear();
            panel.ColumnStyles.Clear();
            panel.RowStyles.Clear();

            // Reset panel configuration
            panel.ColumnCount = 7;
            int column = 0, row = 0;

            // Populate the panel with controls for the specified category
            foreach (var flowPanel in categoryControls[category])
            {
                // Move to the next row if the current row is full
                if (column >= panel.ColumnCount)
                {
                    column = 0;
                    row++;
                }
                // Add the flow panel to the TableLayoutPanel
                panel.Controls.Add(flowPanel, column, row);
                flowPanel.Visible = true;
                column++;
            }

            // Update state variables
            previousCategory = category;
            currentScreenType = "Category";
            Console.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss.fff")}] Current screen type set to 'Category'");
            panel.Update(); // Refresh the panel display
            UpdateNavigationButtons(); // Update buttons after refreshing the category
        }

        private void RefreshItem(string itemTag)
        {
            // Add log to ensure we can track when this method is called
            Console.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss.fff")}] Attempting to refresh item");

            // Safeguard to prevent RefreshItem if the screen type is Modifier
            if (currentScreenType == "Modifier")
            {
                Console.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss.fff")}] Aborting RefreshItem because the current screen type is 'Modifier'");
                return;
            }

            // Clear the current panel content and styles
            panel.Controls.Clear();
            panel.ColumnStyles.Clear();
            panel.RowStyles.Clear();
            panel.ColumnCount = 7;

            // Find the item in the JSON data by tag
            var item = itemData["data"]
                .FirstOrDefault(m => m["menuitem"] != null && m["menuitem"].ToString() == itemTag);

            if (item != null)
            {
                // Extract item details
                string itemName = item["menuitem"].ToString();
                string itemPrice = item["itemprice"].ToString();

                // Load item image
                string picturesFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "KioskProject");
                string fullItemImagePath = GetImagePath($"{itemName}.bmp", picturesFolder);
                Image itemImage = LoadImage(fullItemImagePath, itemName);

                // Create picture box for the item
                PictureBox itemPictureBox = CreateFormattedPictureBox(itemName, itemImage, itemName);
                itemPictureBox.Visible = true;

                // Create label for the item
                Label itemLabel = new Label
                {
                    Name = itemName,
                    Text = itemName,
                    AutoSize = true,
                    TextAlign = ContentAlignment.MiddleCenter,
                    Visible = true,
                    Margin = new Padding(0, 5, 0, 0)
                };

                // Create flow layout panel for the item
                FlowLayoutPanel itemPanel = new FlowLayoutPanel
                {
                    FlowDirection = FlowDirection.TopDown,
                    AutoSize = true,
                    Margin = new Padding(10),
                    Visible = true
                };
                itemPanel.Controls.Add(itemPictureBox);
                itemPanel.Controls.Add(itemLabel);

                // Add the item panel to the main panel
                panel.Controls.Add(itemPanel, 0, 0);

                // Add the item to the ListView
                AddItemToSelectionListView(itemName, itemPrice);
            }

            // Display item modifiers
            DisplayItemModifiers(itemTag);

            // Update state variables
            currentScreenType = "Item";
            Console.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss.fff")}] Current screen type set to 'Item'");
            panel.Update(); // Refresh the panel display
            UpdateNavigationButtons(); // Update buttons after refreshing the item
        }

        private void AddItemToSelectionListView(string itemName, string itemPrice)
        {
            ListViewItem listViewItem = new ListViewItem("1");
            listViewItem.SubItems.Add(itemName);
            listViewItem.SubItems.Add(itemPrice);
            selectionListView.Items.Add(listViewItem);
        }

        private void NextButton_Click(object sender, EventArgs e)
        {
            Console.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss.fff")}] Next button clicked");
            if (currentScreenType == "Modifier")
            {
                DisplayNextModifier();
            }
            else
            {
                Console.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss.fff")}] Next button clicked, but current screen is not 'Modifier'");
            }
        }

        private void PreviousButton_Click(object sender, EventArgs e)
        {
            // Navigate back to the last modifier if we are on the Final Sale screen
            if (currentScreenType == "FinalSale")
            {
                if (currentModifierIndex > 0 && currentModifierIndex <= currentModifierCodes.Count)
                {
                    currentModifierIndex--;
                    var modCode = currentModifierCodes[currentModifierIndex];
                    DisplayModifierDetails(modCode);
                    return;
                }
            }

            // Navigate to the previous screen from the navigation history
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
            UpdateNavigationButtons(); // Update buttons after previous action
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
            // Find the modifier definition by modCode
            var modifierDef = modifierData["data"]
                .FirstOrDefault(m => m["modcode"] != null && m["modcode"].ToString() == modCode);

            // Check if modifier allows only one choice
            if (modifierDef != null && modifierDef["modchoice"]?.ToString() == "one")
            {
                // Get all details related to the modifier code
                var modifierDetails = modifierDetailData["data"]
                    .Where(d => d["modcode"] != null && d["modcode"].ToString() == modCode)
                    .ToList();

                // Iterate through each detail to reset selection state and remove from ListView
                foreach (var detail in modifierDetails)
                {
                    string detailDesc = detail["description"]?.ToString() ?? "Unknown Detail";

                    // Reset selection state if it exists in the dictionary
                    if (modifierSelectionState.ContainsKey(detailDesc))
                    {
                        modifierSelectionState[detailDesc] = false;
                    }

                    // Find and remove the item from the ListView
                    ListViewItem itemToRemove = selectionListView.Items
                        .Cast<ListViewItem>()
                        .FirstOrDefault(item => item.SubItems[1].Text == detailDesc);
                    if (itemToRemove != null)
                    {
                        selectionListView.Items.Remove(itemToRemove);
                    }
                }
            }
        }

        private void DisplayMainCategory()
        {
            // Clear the current panel content and styles
            panel.Controls.Clear();
            panel.ColumnStyles.Clear();
            panel.RowStyles.Clear();

            // Reset panel configuration
            panel.ColumnCount = 7;
            int column = 0, row = 0;

            // Populate the panel with category panels
            foreach (var categoryPanel in categoryPanels)
            {
                if (column >= panel.ColumnCount)
                {
                    column = 0;
                    row++;
                }
                panel.Controls.Add(categoryPanel, column, row);
                categoryPanel.Visible = true;
                column++;
            }

            // Update state variables
            currentScreenType = "MainCategory";
            Console.WriteLine($"[{DateTime.Now.ToString("hh:mm:ss.fff")}] current screen type set to 'maincategory'");
            panel.Update(); // Refresh the panel display
            UpdateNavigationButtons(); // Update buttons after displaying the main category
        }

        private void DisplayItemModifiers(string itemTag)
        {
            // Find the item in the JSON data by tag
            var item = itemData["data"]
                .FirstOrDefault(m => m["menuitem"] != null && m["menuitem"].ToString() == itemTag);

            // Exit if the item is not found
            if (item == null) return;

            // Define sections for modifiers
            string[] modifierSections = { "aa", "bb", "cc", "dd", "ee", "ff", "gg", "hh", "ii", "jj", "kk", "ll" };
            currentModifierCodes = new List<string>();

            // Extract modifier codes from the item
            foreach (var section in modifierSections)
            {
                if (item[section] != null && item[section].Type == JTokenType.String && !string.IsNullOrEmpty(item[section].ToString()))
                {
                    currentModifierCodes.Add(item[section].ToString());
                }
            }

            // Initialize modifier index and update navigation history
            currentModifierIndex = 0;
            previousCategory = item["menucategory"].ToString();
            navigationHistory.Push(new NavigationEntry("Item", itemTag));

            // Display the first modifier
            DisplayNextModifier();
        }

        private void DisplayNextModifier()
        {
            Console.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss.fff")}] DisplayNextModifier called");
            // Check if all modifiers have been displayed
            if (currentModifierIndex >= currentModifierCodes.Count)
            {
                DisplayFinalSaleScreen();
                return;
            }

            // Retrieve the current modifier code and increment the index
            string modCode = currentModifierCodes[currentModifierIndex];
            currentModifierIndex++;

            // Find the modifier definition by modCode
            var modifierDef = modifierData["data"]
                .FirstOrDefault(m => m["modcode"] != null && m["modcode"].ToString() == modCode);

            if (modifierDef != null)
            {
                // Push the current modifier to the navigation history and display its details
                navigationHistory.Push(new NavigationEntry("Modifier", modCode));
                DisplayModifierDetails(modCode);
            }
            else
            {
                // If the modifier definition is not found, display the next modifier
                DisplayNextModifier();
            }

            UpdateNavigationButtons(); // Update buttons after displaying the next modifier
        }

        private void DisplayModifierDetails(string modCode)
        {
            // Clear the current panel content
            panel.Controls.Clear();

            // Find the modifier definition by modCode
            var modifierDef = modifierData["data"]
                .FirstOrDefault(m => m["modcode"] != null && m["modcode"].ToString() == modCode);

            // Exit if the modifier definition is not found
            if (modifierDef == null) return;

            // Log the current modifier index and total number of modifiers
            Console.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss.fff")}] Displaying details for modifier {currentModifierIndex}/{currentModifierCodes.Count}: {modCode}");

            // Determine modifier choice type and set button visibility
            string modChoiceType = modifierDef["modchoice"]?.ToString() ?? "one";

            // Retrieve modifier details related to the modCode
            var modifierDetails = modifierDetailData["data"]
                .Where(d => d["modcode"] != null && d["modcode"].ToString() == modCode)
                .ToList();

            foreach (var detail in modifierDetails)
            {
                string detailDesc = detail["description"]?.ToString() ?? "Unknown Detail";
                string cost = detail["cost"]?.ToString() ?? "N/A";
                Console.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss.fff")}] - {detailDesc}: {cost}");
            }

            // Create and add UI elements for each modifier detail
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

                // Set up click event handlers based on modChoiceType
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
                    Margin = new Padding(0, 5, 0, 0)
                };

                FlowLayoutPanel detailPanel = new FlowLayoutPanel
                {
                    FlowDirection = FlowDirection.TopDown,
                    AutoSize = true,
                    Margin = new Padding(10),
                    Visible = true
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

            // Update state variables
            currentScreenType = "Modifier";
            Console.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss.fff")}] Current screen type set to 'Modifier'");
        }

        private void DisplayFinalSaleScreen()
        {
            // Clear the current panel content
            panel.Controls.Clear();

            // Create the thank-you message label
            Label finalMessage = new Label
            {
                Text = "Thank you for your purchase!",
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleCenter,
                Margin = new Padding(0, 5, 0, 0),
                Font = new Font("Arial", 24, FontStyle.Bold)
            };

            // Create and configure the flow layout panel
            FlowLayoutPanel finalPanel = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown,
                AutoSize = true,
                Margin = new Padding(10),
                Visible = true
            };

            // Add the label to the flow layout panel
            finalPanel.Controls.Add(finalMessage);

            // Add the flow layout panel to the main panel
            panel.Controls.Add(finalPanel);

            // Update state variables
            currentScreenType = "FinalSale";
            Console.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss.fff")}] Current screen type set to 'FinalSale'");
            UpdateNavigationButtons(); // Update buttons after displaying the final sale screen
        }

        private void ModifierDetailPictureBox_Click_One(object sender, EventArgs e, string modCode)
        {
            PictureBox pictureBox = sender as PictureBox;
            if (pictureBox != null)
            {
                string detailDesc = pictureBox.Tag.ToString();
                ToggleModifierSelection(pictureBox, detailDesc, modCode);
                DisplayNextModifier();
            }
        }

        private void ModifierDetailPictureBox_Click_Upsale(object sender, EventArgs e, string modCode)
        {
            PictureBox pictureBox = sender as PictureBox;
            if (pictureBox != null)
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
            // Find the modifier definition by modCode
            var modifierDef = modifierData["data"]
                .FirstOrDefault(m => m["modcode"] != null && m["modcode"].ToString() == modCode);

            if (modifierDef != null)
            {
                // Determine modifier choice type
                string modChoiceType = modifierDef["modchoice"]?.ToString() ?? "one";
                if (modChoiceType == "upsale")
                {
                    if (isSelected)
                    {
                        // Create an overlay on the PictureBox image to indicate selection
                        Bitmap overlayImage = new Bitmap(pictureBox.Image);
                        using (Graphics g = Graphics.FromImage(overlayImage))
                        {
                            using (Brush brush = new SolidBrush(Color.FromArgb(128, Color.Yellow)))
                            {
                                g.FillRectangle(brush, new Rectangle(0, 0, overlayImage.Width, overlayImage.Height));
                            }
                        }
                        pictureBox.Image = overlayImage;
                    }
                    else
                    {
                        // Reload the original image from the specified path
                        string imagePath = GetImagePath($"{detailDesc}.bmp");
                        string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                        string picturesFolder = Path.Combine(desktopPath, "KioskProject");
                        string fullImagePath = Path.Combine(picturesFolder, imagePath.ToLower());

                        pictureBox.Image = File.Exists(fullImagePath) ?
                            Image.FromFile(fullImagePath) :
                            Image.FromFile(Path.Combine(picturesFolder, "image not avail.bmp"));
                    }
                }
                else
                {
                    // Set the PictureBox background color based on selection state
                    pictureBox.BackColor = isSelected ? Color.Green : Color.Transparent;
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
                ListViewItem listViewItem = new ListViewItem("1");
                listViewItem.SubItems.Add(detailDesc);
                listViewItem.SubItems.Add(cost);
                selectionListView.Items.Add(listViewItem);
            }
            else
            {
                ListViewItem itemToRemove = selectionListView.Items.Cast<ListViewItem>().FirstOrDefault(item => item.SubItems[1].Text == detailDesc);
                if (itemToRemove != null)
                {
                    selectionListView.Items.Remove(itemToRemove);
                }
            }
        }

        private string GetImagePath(string path)
        {
            string[] parts = path.Split(':');
            return parts.Last().Trim().ToLower();
        }

        private Image LoadImage(string path, string description)
        {
            try
            {
                Image image = Image.FromFile(path);
                return image;
            }
            catch (Exception)
            {
                string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                string kioskProjectFolder = Path.Combine(desktopPath, "KioskProject");
                string fallbackPath = Path.Combine(kioskProjectFolder, "image not avail.bmp");
                return Image.FromFile(fallbackPath);
            }
        }

        private void LogItemsAndModifierDefs()
        {
            // Log all items
            //Console.WriteLine("Items:");
            foreach (JObject item in itemData["data"])
            {
                string itemName = item["menuitem"]?.ToString() ?? "Unknown Item";
                string itemPrice = item["itemprice"]?.ToString() ?? "Unknown Price";
                string itemCategory = item["menucategory"]?.ToString() ?? "Unknown Category";
                //Console.WriteLine($"Name: {itemName}, Price: {itemPrice}, Category: {itemCategory}");
            }

            // Log all modifier definitions
            //Console.WriteLine("\nModifier Definitions:");
            foreach (JObject modifierDef in modifierData["data"])
            {
                string modCode = modifierDef["modcode"]?.ToString() ?? "Unknown ModCode";
                string modChoice = modifierDef["modchoice"]?.ToString() ?? "Unknown ModChoice";
                //Console.WriteLine($"ModCode: {modCode}, ModChoice: {modChoice}");
            }
        }

        private void LogAssociatedModifiers(string itemTag)
        {
            // Find the item in the JSON data by tag
            var item = itemData["data"]
                .FirstOrDefault(m => m["menuitem"] != null && m["menuitem"].ToString() == itemTag);

            // Exit if the item is not found
            if (item == null) return;

            // Define sections for modifiers
            string[] modifierSections = { "aa", "bb", "cc", "dd", "ee", "ff", "gg", "hh", "ii", "jj", "kk", "ll" };
            var associatedModifierCodes = new List<string>();

            // Extract modifier codes from the item
            foreach (var section in modifierSections)
            {
                if (item[section] != null && item[section].Type == JTokenType.String && !string.IsNullOrEmpty(item[section].ToString()))
                {
                    associatedModifierCodes.Add(item[section].ToString());
                }
            }

            // Log the associated modifiers
            Console.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss.fff")}] Modifiers for item {itemTag}:");
            foreach (var modCode in associatedModifierCodes)
            {
                var modifierDef = modifierData["data"]
                    .FirstOrDefault(m => m["modcode"] != null && m["modcode"].ToString() == modCode);

                if (modifierDef != null)
                {
                    string modChoice = modifierDef["modchoice"]?.ToString() ?? "Unknown ModChoice";
                    Console.WriteLine($"ModCode: {modCode}, ModChoice: {modChoice}");
                }
            }
        }

        private void UpdateNavigationButtons()
        {
            bool previousButtonVisible = (currentScreenType == "Modifier" && currentModifierIndex > 1) || currentScreenType == "FinalSale";

            bool nextButtonVisible = false;
            if (currentScreenType == "Modifier" && currentModifierIndex < currentModifierCodes.Count)
            {
                var modifier = modifierData["data"]
                                    .FirstOrDefault(m => m["modcode"] != null && m["modcode"].ToString() == currentModifierCodes[currentModifierIndex - 1]);

                if (modifier != null && modifier["modchoice"]?.ToString() == "upsale")
                {
                    nextButtonVisible = true;
                }
            }

            previousButton.Visible = previousButtonVisible;
            nextButton.Visible = nextButtonVisible;

            // Log the next button visibility
            Console.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss.fff")}] Next button visibility: {nextButtonVisible}");
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
