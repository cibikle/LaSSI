using Eto;
using Eto.Drawing;
using Eto.Forms;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
//using System.Windows.Documents;

namespace LaSSI
{
   partial class MainForm : Form
   {
      private System.Uri savesFolder;
      private string saveFilePath;
      private SaveFilev2 saveFile;
      private ObservableCollection<string> ItemList;
      void InitializeComponent()
      {
         //this.SizeChanged += MainForm_SizeChanged;
         Title = "LaSSI (Last Starship Save Inspector)";
         MinimumSize = new Size(200, 200);
         Size = new Size(1024, 600);
         Location = AdjustForFormSize(GetScreenCenter(), Size);
         Padding = 10;
         savesFolder = GetSavesUri();
         using (var ItemListStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("LaSSI.ItemList.txt"))
         {
            ItemList = new ObservableCollection<string>();
            TextReader reader = new StreamReader(ItemListStream!);
            string[] ItemListArray = reader.ReadToEnd().Split(Environment.NewLine);
            foreach (var line in ItemListArray)
            {
               ItemList.Add(line);
            }
         }

         var openFileCommand = new Command { MenuText = "Open", Shortcut = Application.Instance.CommonModifier | Keys.O };
         openFileCommand.Executed += OpenFileCommand_Executed;

         var quitCommand = new Command { MenuText = "Quit", Shortcut = Application.Instance.CommonModifier | Keys.Q };
         quitCommand.Executed += (sender, e) => Application.Instance.Quit();

         var prefsCommand = new Command(PrefsCommand_Executed);

         var aboutCommand = new Command { MenuText = "About..." };
         aboutCommand.Executed += (sender, e) =>
         {
            string title = "LaSSI (Last Starship Save Inspector)";
            string author = "CIBikle, 2023";
            string tlsOwner = "'The Last Starship' is the property of Introversion Software";
            string tlsOwnerLink = "https://www.introversion.co.uk/introversion/";
            string disclaimer = "This is a fan-made tool for educational and entertainment purposes";
            var dlg = GetModal($"{title}{Environment.NewLine}{author}{Environment.NewLine}{tlsOwner}{Environment.NewLine}{disclaimer}");
            DynamicLayout bar = (DynamicLayout)dlg.Content;
            Button b = (Button)bar.Children.Where(x => (string)x.Tag == "Abort button").First();
            bar.Remove(b);
            var linkButton1 = new LinkButton { Tag = "Link button 1", Text = tlsOwnerLink };
            linkButton1.Click += delegate { Process.Start("explorer", linkButton1.Text); };
            bar.Add(linkButton1);
            bar.Add(b);
            dlg.ShowModal(this);
         };

         // create menu
         Menu = new MenuBar
         {
            Items =
            {
					// File submenu
					new SubMenuItem { Text = "&File", Items = { openFileCommand } },
					// new SubMenuItem { Text = "&Edit", Items = { /* commands/items */ } },
					// new SubMenuItem { Text = "&View", Items = { /* commands/items */ } },
				},
            //ApplicationItems =
            //{
            //   // application (OS X) or file menu (others)
            //   new ButtonMenuItem { Text = "&Preferences...", Command = prefsCommand, Shortcut = Application.Instance.CommonModifier | Keys.Comma },
            //},
            QuitItem = quitCommand,
            AboutItem = aboutCommand

         };
         Content = InitMainPanel();
         Focus(); //required to prevent focus from being on the menu bar when the app launches on Windows
         BringToFront(); //for whatever reason, LaSSI was opening behind VS on Mac
         //what do you suppose will be the weird thing I have to account for on Linux?
         // create toolbar			
         /*ToolBar = new ToolBar { Items = { clickMe } };*/
      }
      private static Uri GetSavesUri()
      {
         string savesFolderPath = string.Empty; //todo: probably should put some sort of fallback on this
         if (EtoEnvironment.Platform.IsMac)
         {
            savesFolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "Library", "Application Support");
         }
         else if (EtoEnvironment.Platform.IsWindows)
         {
            savesFolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Introversion");
         } //todo: Linux
         savesFolderPath = Path.Combine(savesFolderPath, "LastStarship", "saves");
         Uri SavesUri = new Uri(savesFolderPath);
         return SavesUri;
      }
      private static Point GetScreenCenter()
      {
         var screenBounds = Screen.PrimaryScreen.Bounds;
         var screenWidth = screenBounds.Width / 2;
         var screenHeight = screenBounds.Height / 2;
         var screenCenter = new Point((int)(screenWidth), (int)(screenHeight));

         return screenCenter;
      }
      private static Point AdjustForFormSize(Point screenCenter, Size formSize)
      {
         var adjustedCenter = new Point(screenCenter.X - (formSize.Width / 2), screenCenter.Y - (formSize.Height / 2));
         return adjustedCenter;
      }
      private void OpenFileCommand_Executed(object? sender, EventArgs e)
      {
         OpenFileDialog foo = new OpenFileDialog();
         foo.Directory = savesFolder;
         foo.Filters.Add("Last Starship save files|*.space");
         if (foo.ShowDialog(this) == DialogResult.Ok)
         {
            //this.Cursor = Cursors.; they don't have a waiting cursor; todo: guess I'll add my own--later!
            saveFilePath = foo.FileName;
            saveFile = new SaveFilev2(saveFilePath);
            UpdateTextbox("saveFileTextbox", saveFilePath);
            Eto.Forms.Form progressForm = GetProgressForm();
            progressForm.Show();
            SaveFilev2.LoadFile(saveFile, saveFilePath);
            if (progressForm != null && progressForm.Visible) progressForm.Close();
            UpdateTextbox("gameModeTextbox", saveFile.GameMode);
            UpdateTextbox("saveVersionTextbox", saveFile.SaveVersion.ToString());
            UpdateTextbox("nextIdTextbox", saveFile.NextId.ToString());
            UpdateTextbox("timeIndexTextbox", saveFile.TimeIndex.ToString());
            UpdateTextbox("deltaTimeTextbox", saveFile.DeltaTime.ToString());
            UpdateTextbox("playTimeTextbox", saveFile.PlayTime.ToString());
            UpdateTreeView();
         }
      }
      private static Form GetProgressForm() // todo: fix this whole thing
      {
         Eto.Forms.Form progressForm = new Eto.Forms.Form();
         ProgressBar progressBar = new ProgressBar();
         progressBar.Indeterminate = true;
         progressForm.Content = progressBar;
         progressForm.Size = new Size(400, 50);
         progressForm.Title = "Loading...";
         progressForm.Location = GetScreenCenter() - 200; // todo: fix this
         return progressForm;
      }
      private void UpdateTreeView()
      {
         DynamicLayout bar = (DynamicLayout)this.Content;
         TreeGridView x = (TreeGridView)bar.Children.Where<Control>(x => (string)x.Tag == "DataTreeView").First();
         x.DataStore = SaveFile2TreeGridItems();
         x.Columns.Clear();
         x.Columns.Add(new GridColumn
         {
            AutoSize = true
            ,
            DataCell = new TextBoxCell(0)
         });
      }
      private TreeGridItemCollection SaveFile2TreeGridItems()
      {
         TreeGridItemCollection treeGridItems = new TreeGridItemCollection();
         treeGridItems.Add(WalkNodeTree(saveFile.root));
         treeGridItems[0].Expanded = true;
         return treeGridItems;
      }
      private TreeGridItem WalkNodeTree(Node node)
      {
         if (!node.HasChildren())
         {
            return new TreeGridItem(node.Name, node.Properties);
         }
         else
         {
            TreeGridItemCollection childItems = new TreeGridItemCollection();
            foreach (var child in node.Children)
            {
               childItems.Add(WalkNodeTree(child));
            }
            return new TreeGridItem(childItems, node.Name, node.Properties);
         }
      }
      private bool UpdateTextbox(string controlTag, string text)
      {
         bool success = false;
         DynamicLayout bar = (DynamicLayout)this.Content;
         TextBox x = (TextBox)bar.Children.Where<Control>(x => (string)x.Tag == controlTag).First();
         if (x != null)
         {
            x.Text = text;
            success = true;
         }
         return success;
      }
      private void PrefsCommand_Executed(Object? sender, EventArgs e)
      {
         var dlg = GetModal("Preferences not implemented");
         //dlg.Content.
         dlg.ShowModal(Application.Instance.MainForm);
      }
      private static Eto.Forms.Dialog GetModal(string text)
      {
         List<string> lines = text.Split(Environment.NewLine).ToList();
         int longestLineLength = lines.OrderByDescending(x => x.Length).First().Length;
         int numLines = lines.Count;
         var dlg = new Eto.Forms.Dialog();
         //dlg.ClientSize = new Size((5 * longestLineLength), (50 * numLines));
         dlg.DefaultButton = new Eto.Forms.Button { Text = "OK" };
         var layout = new DynamicLayout();
         layout.AddCentered(new Label { Text = text, Tag = "Label" }, xscale: true, yscale: true);
         dlg.AbortButton = new Button { Text = "Close", Tag = "Abort button" };
         dlg.AbortButton.Click += delegate
         {
            dlg.Close();
         };
         layout.BeginVertical();
         layout.AddRow(null, dlg.AbortButton);
         layout.EndVertical();

         dlg.Content = layout;

         return dlg;
      }
      private DynamicLayout InitMainPanel()
      {
         DynamicLayout rootLayout = new DynamicLayout();
         rootLayout.Spacing = new Size(0, 5);
         rootLayout.Add(InitFilePanel());
         rootLayout.Add(InitSaveStatsPanel());
         rootLayout.Add(InitDetailsPanel());

         //rootLayout.Add(null);
         return rootLayout;
      }
      private static TableLayout InitFilePanel()
      {
         TableLayout fileLayout = new TableLayout();
         fileLayout.Spacing = new Size(5, 0);
         TextBox filename = new TextBox();
         Label currentFile = new Label();
         currentFile.Text = "Current file:";
         currentFile.VerticalAlignment = VerticalAlignment.Center;
         filename.Tag = "saveFileTextbox";
         filename.Width = 500;
         fileLayout.Rows.Add(new TableRow(currentFile, new TableCell(filename, true), new TableCell(null)));
         return fileLayout;
      }
      private static TableLayout InitSaveStatsPanel()
      {
         TableLayout statsLayout = new TableLayout();
         statsLayout.Spacing = new Size(5, 0);

         TextBox gameModeTextbox = new TextBox();//todo: change to combobox?
         gameModeTextbox.Tag = "gameModeTextbox";
         gameModeTextbox.Width = 80;
         gameModeTextbox.Enabled = false;
         TextBox saveVersionTextBox = new TextBox();
         saveVersionTextBox.Tag = "saveVersionTextbox";
         saveVersionTextBox.Width = 20;
         saveVersionTextBox.Enabled = false;
         TextBox nextIdTextbox = new TextBox();
         nextIdTextbox.Tag = "nextIdTextbox";
         nextIdTextbox.Width = 60;
         nextIdTextbox.Enabled = false;
         TextBox timeIndexTextbox = new TextBox();//todo: why is this being reported wrong? also, put a warning on it!
         timeIndexTextbox.Tag = "timeIndexTextbox";
         timeIndexTextbox.Width = 120;
         timeIndexTextbox.Enabled = false;
         TextBox deltaTimeTextbox = new TextBox();//todo: why is this being reported wrong?
         deltaTimeTextbox.Tag = "deltaTimeTextbox";
         deltaTimeTextbox.Width = 150;
         deltaTimeTextbox.Enabled = false;
         TextBox playTimeTextbox = new TextBox();//todo: why is this being reported wrong?
         playTimeTextbox.Tag = "playTimeTextbox";
         playTimeTextbox.Width = 120;
         playTimeTextbox.Enabled = false;

         var gamemode = new Label();
         var savevers = new Label();
         var nextid = new Label();
         var timeindex = new Label();
         var deltatime = new Label();
         var playtime = new Label();
         gamemode.Text = "Game mode:";
         gamemode.VerticalAlignment = VerticalAlignment.Center;
         savevers.Text = "Save version:";
         savevers.VerticalAlignment = VerticalAlignment.Center;
         nextid.Text = "Next ID:";
         nextid.VerticalAlignment = VerticalAlignment.Center;
         timeindex.Text = "Time index:";
         timeindex.VerticalAlignment = VerticalAlignment.Center;
         deltatime.Text = "Delta time:";
         deltatime.VerticalAlignment = VerticalAlignment.Center;
         playtime.Text = "Play time:";
         playtime.VerticalAlignment = VerticalAlignment.Center;

         //statsLayout.Rows.Add(new TableRow(new TableCell(gamemode), new TableCell(gameModeTextbox), savevers, saveVersionTextBox, nextid, nextIdTextbox, null));
         statsLayout.Rows.Add(new TableRow(gamemode, gameModeTextbox, savevers, saveVersionTextBox, nextid, nextIdTextbox, timeindex, timeIndexTextbox, deltatime, deltaTimeTextbox, playtime, playTimeTextbox, null));
         return statsLayout;
      }
      private TableLayout InitDetailsPanel()
      {
         TableLayout dataLayout = new TableLayout();

         TreeGridView treeView = new TreeGridView();
         treeView.SelectedItemChanged += TreeView_SelectedItemChanged;
         treeView.Tag = "DataTreeView";
         treeView.AllowEmptySelection = true;
         treeView.AllowMultipleSelection = true;
         //Panel placeholder = new Panel();
         Scrollable DetailScrollable = new Scrollable
         {
            Tag = "DetailScrollable",
            ID = "DetailScrollable"
         };
         Splitter sp = new Splitter
         {
            Tag = "Splitter",
            Orientation = Orientation.Horizontal,
            SplitterWidth = 10,
            Panel1 = treeView
         };
         sp.Panel1.Width = this.Width / 2;
         //sp.Panel2 = DetailScrollable;
         //sp.Panel2 = InitDualListbox(ItemList, y.ToList<string>());
         ObservableCollection<InventoryGridItem> items = new ObservableCollection<InventoryGridItem>();
         items.Add(new InventoryGridItem("Crewmember", 7));
         items.Add(new InventoryGridItem("Wepo", 2));
         sp.Panel2 = InitDualListbox(ItemList, items);
         //DetailScrollable.Content = InitDualListbox(ItemList, items);
         //DetailScrollable.Scroll += DetailScrollable_Scroll; //todo: why does this cause the program to hitch on launch?
         DetailScrollable.Padding = new Padding(5, 5);
         /*         TextArea textArea = new TextArea();
                  textArea.Tag = "TextArea";
                  placeholder.Content = textArea;*/

         dataLayout.Rows.Add(sp);
         return dataLayout;
      }
      private static Label CreateDetailLabel(string text)
      {
         return new Label
         {
            Text = text
            ,
            VerticalAlignment = VerticalAlignment.Center
         };
      }
      private static TextBox CreateDetailTextBox(string text)
      {
         return new TextBox
         {
            Text = text
         };
      }
      private static DropDown CreateDetailDropMenu(string options)
      {
         DropDown dropDown = new DropDown();
         var optionsArray = options.Split(',');
         foreach (var option in optionsArray)
         {
            dropDown.Items.Add(option);
         }

         return dropDown;
      }
      private static DropDown CreateDetailDropMenu(string text, int defaultIndex)
      {
         DropDown dropDown = CreateDetailDropMenu(text);
         dropDown.SelectedIndex = defaultIndex;
         return dropDown;
      }
      private static bool IsValueTrueFalse(string value)
      {
         return String.Equals(value, "true", StringComparison.OrdinalIgnoreCase)
                     || String.Equals(value, "false", StringComparison.OrdinalIgnoreCase);
      }
      private static StackLayout GetDetailsPanel(TreeGridItem item)
      {
         StackLayout detailsPanel = new StackLayout
         {
            Orientation = Orientation.Vertical,
            //Spacing = 10,
            Padding = new Padding(5, 0)
         };
         foreach (var s in item.Values)
         {
            if (s is string)
            {
               detailsPanel.Items.Add(new StackLayoutItem(new Label().Text = s.ToString()));
            }
            else if (s is Dictionary<string, string> && ((Dictionary<string, string>)s).Count != 0)
            {
               TableLayout innerLayout = new TableLayout
               {
                  Spacing = new Size(5, 0)
               };
               foreach (var p in (Dictionary<string, string>)s)
               {
                  Label label = CreateDetailLabel(p.Key);
                  Control value;
                  if (IsValueTrueFalse(p.Value))
                  {
                     value = CreateDetailDropMenu("true,false", String.Equals(p.Value, "true", StringComparison.OrdinalIgnoreCase) ? 0 : 1);
                  }
                  else
                  {
                     value = CreateDetailTextBox(p.Value);
                  }

                  innerLayout.Rows.Add(new TableRow(new TableCell(label), new TableCell(value)));
               }
               detailsPanel.Items.Add(new StackLayoutItem(innerLayout));
            }
         }
         return detailsPanel;
      }
      private void TreeView_SelectedItemChanged(object? sender, EventArgs e)
      {
         if (sender is not null and TreeGridView)
         {
            TreeGridItem item = (TreeGridItem)((TreeGridView)sender).SelectedItem;
            DynamicLayout bar = (DynamicLayout)this.Content;
            Scrollable s = (Scrollable)bar.Children.Where<Control>(x => (string)x.Tag == "DetailScrollable").First();
            s.Content = GetDetailsPanel(item);
         }
      }
      private static GridView CreateLeftGrid(ObservableCollection<string> list, string? headerText)
      {
         int maxLength = list.OrderByDescending(x => x.Length).First().Length;
         GridView LeftListBox = new GridView
         {
            //GridLines = GridLines.Both,
            DataStore = list,
            AllowMultipleSelection = true,
            Tag = "LeftListBox",
            Columns =
            {
               new GridColumn
               {
                  //AutoSize = true,
                  Width = maxLength * 7,
                  DataCell = new TextBoxCell { Binding = Binding.Delegate((object o) => Convert.ToString(o))},
                  Sortable = true,
               }
            },
         };
         if (!string.IsNullOrEmpty(headerText))
         {
            LeftListBox.Columns[0].HeaderText = headerText;
         }
         return LeftListBox;
      }
      private static GridView CreateRightGrid(ObservableCollection<InventoryGridItem> list, List<string>? headerText)
      {
         GridView RightListBox = new GridView
         {
            GridLines = GridLines.Both,
            DataStore = list,
            AllowMultipleSelection = true,
            Tag = "RightListBox",
            Columns =
            {
               new GridColumn
               {
                  AutoSize = true,
                  DataCell = new TextBoxCell { Binding = Binding.Property((InventoryGridItem i) => i.Name)},
               },
               new GridColumn
               {
                  AutoSize = true,
                  DataCell = new TextBoxCell { Binding = Binding.Property((InventoryGridItem i) => i.Count).Convert(v => v.ToString(), s => {int i = 0; return int.TryParse(s, out i) ? i : -1; }) },
                  Editable = true,
               }
            }
         };
         if (headerText != null)
         {
            for (int i = 0; i < headerText.Count; i++)
            {
               RightListBox.Columns[i].HeaderText = headerText[i];
            }
         }
         RightListBox.ColumnWidthChanged += RightListBox_ColumnWidthChanged;
         RightListBox.CellEdited += RightListBox_CellEdited;
         return RightListBox;
      }

      private static void RightListBox_ColumnWidthChanged(object? sender, GridColumnEventArgs e)
      {
         var foo = (GridView)sender;
         var bar = (ObservableCollection<InventoryGridItem>)foo.DataStore;
         int maxLength = bar.OrderByDescending(x => x.Name.Length).First().Name.Length;
         foo.Width = maxLength * 10;
      }

      private static void RightListBox_CellEdited(object? sender, GridViewCellEventArgs e)
      {
         Debug.WriteLine("hello!");
      }

      private static List<Button> CreateDualListboxButtons()
      {
         Button MoveLeft = new Button
         {
            Text = "<",
            Width = 25
         };
         MoveLeft.Click += MoveLeft_Click;
         Button MoveLeftAll = new Button
         {
            Text = "<<",
            Width = 25
         };
         MoveLeftAll.Click += MoveLeftAll_Click;
         Button MoveRight = new Button
         {
            Text = ">",
            Width = 25
         };
         ;
         MoveRight.Click += MoveRight_Click;
         Button MoveRightAll = new Button
         {
            Text = ">>",
            Width = 25
         };
         MoveRightAll.Click += MoveRightAll_Click;
         List<Button> buttons = new List<Button>();
         buttons.Add(MoveRightAll);
         buttons.Add(MoveRight);
         buttons.Add(MoveLeft);
         buttons.Add(MoveLeftAll);

         return buttons;
      }
      public static Panel InitDualListbox(ObservableCollection<string> leftList, ObservableCollection<InventoryGridItem> rightList)
      {
         GridView LeftListBox = CreateLeftGrid(leftList, "Remaing catalog");
         GridView RightListBox = CreateRightGrid(rightList, new List<string> { "In stock", "Count" });
         List<Button> buttons = CreateDualListboxButtons();

         Panel MainPanel = new Panel
         {
            Content = GetMainLayout(LeftListBox, RightListBox, buttons/*, "Remaining catalog", "Items in stock"*/),
            ID = "ListBuilderMainPanel"
         };

         return MainPanel;
      }
      private static StackLayout GetButtonsLayout(List<Button> buttons)
      {
         StackLayout layout = new StackLayout();
         layout.Orientation = Eto.Forms.Orientation.Vertical;
         layout.Tag = "ButtonsLayout";
         layout.Padding = new Padding(0, 50, 0, 0);
         foreach (Button button in buttons)
         {
            layout.Items.Add(button);
         }
         return layout;
      }
      private static DynamicLayout GetMainLayout(GridView LeftListBox, GridView RightListBox, List<Button> buttons)
      {
         DynamicLayout dynamicLayout = new DynamicLayout
         {
            Spacing = new Size(20, 0)
         };
         Scrollable leftScrollable = new Scrollable
         {
            Tag = "LeftDualListboxScrollable",
            ID = "LeftDualListboxScrollable",
            Content = LeftListBox,
         };
         Scrollable rightScrollable = new Scrollable
         {
            Tag = "RightDualListboxScrollable",
            ID = "RightDualListboxScrollable",
            Content = RightListBox,
         };
         dynamicLayout.BeginHorizontal();
         dynamicLayout.Add(leftScrollable);
         dynamicLayout.Add(GetButtonsLayout(buttons));
         dynamicLayout.Add(rightScrollable);
         dynamicLayout.AddSpace();
         dynamicLayout.EndHorizontal();
         return dynamicLayout;
      }
      private static void MoveRightAll_Click(object? sender, EventArgs e)
      {
         throw new NotImplementedException();
      }
      private static bool InventoryGridListContains(ObservableCollection<InventoryGridItem> list, string name)
      {
         foreach (InventoryGridItem item in list)
         {
            if (item.Name == name) return true;
         }
         return false;
      }
      private static void MoveRight_Click(object? sender, EventArgs e)
      {
         //Button button = ;
         //GridView left = (GridView)button.Parent.Children.Where<Control>(x => (string)x.Tag == "LeftListBox").First();
         //Scrollable s = (Scrollable)((Button)sender!).Parents.Where<Widget>(x => (string)x.ID == "DetailScrollable").First();
         Panel p = (Panel)((Button)sender!).Parents.Where<Widget>(x => (string)x.ID == "ListBuilderMainPanel").First();
         GridView left = (GridView)p.Children.Where<Control>(x => (string)x.Tag == "LeftListBox").First();
         GridView right = (GridView)p.Children.Where<Control>(x => (string)x.Tag == "RightListBox").First();
         //GridView right = (GridView)button.Parent.Children.Where<Control>(x => (string)x.Tag == "RightListBox").First();
         //List<string> foo = (List<string>)left.DataStore;
         ObservableCollection<InventoryGridItem> bar = (ObservableCollection<InventoryGridItem>)right.DataStore;

         if (left.SelectedRow > -1 && left.SelectedItem.ToString() != string.Empty && !InventoryGridListContains(bar, left.SelectedItem.ToString()!)) //todo: support multi-select. or turn it off, I guess
         {
            string name = left.SelectedItem.ToString()!;
            bar.Add(new InventoryGridItem(name, 1));
            //right.DataStore = bar; //todo: this doesn't seem right. I'm definitely doing a *lot* of this wrong.
            //todo: remove the selected item(s) from the left grid
         }
      }
      private static void MoveLeftAll_Click(object? sender, EventArgs e)
      {
         throw new NotImplementedException();
      }
      private static void MoveLeft_Click(object? sender, EventArgs e)
      {
         throw new NotImplementedException();
      }
      /*      private void DetailScrollable_Scroll(object? sender, ScrollEventArgs e)
            {
               Scrollable x = (Scrollable)sender;
               StackLayout buttons = (StackLayout)x.Children.Where<Control>(x => (string)x.Tag == "ButtonsLayout").First();
               buttons.Padding = new Padding(0, x.VisibleRect.Top + 50, 0, 0);
            }*/
      public class InventoryGridItem : GridItem
      {
         public string Name { get; set; } = string.Empty;
         public int Count { get; set; } = -1;
         public InventoryGridItem(string name, int count)
         {
            this.Name = name;
            this.Count = count;
         }
      }
      public class ListBuilder : Panel
      {

      }
   }
}
