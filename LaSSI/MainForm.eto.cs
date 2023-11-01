using Eto;
using Eto.Drawing;
using Eto.Forms;
using MonoMac.AppKit;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using static Eto.Mac.Forms.MemoryDataObjectHandler;
//using System.Windows.Documents;

namespace LaSSI
{
   partial class MainForm : Form
   {
      private System.Uri savesFolder;
      private string saveFilePath;
      private SaveFilev2 saveFile;
      private ObservableCollection<string> InventoryMasterList;
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
            InventoryMasterList = new ObservableCollection<string>();
            TextReader reader = new StreamReader(ItemListStream!);
            string[] ItemListArray = reader.ReadToEnd().Split(Environment.NewLine);
            foreach (var line in ItemListArray)
            {
               InventoryMasterList.Add(line);
            }
         }

         var openFileCommand = new Command { MenuText = "Open", Shortcut = Application.Instance.CommonModifier | Keys.O };
         openFileCommand.Executed += OpenFileCommand_Executed;

         var quitCommand = new Command { MenuText = "Quit", Shortcut = Application.Instance.CommonModifier | Keys.Q };
         quitCommand.Executed += (sender, e) => Application.Instance.Quit();

         var prefsCommand = new Command(PrefsCommand_Executed);

         var aboutCommand = new Command { MenuText = "About..." };
         aboutCommand.Executed += (sender, e) => //todo: break this out into its own handler method
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
            linkButton1.Click += delegate { Application.Instance.Open(linkButton1.Text); };
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
         //leftgrid.Height = rightgrid.Height;
         if (EtoEnvironment.Platform.IsWindows)
         {
            Focus(); //required to prevent focus from being on the menu bar when the app launches on Windows
         }
         else if (EtoEnvironment.Platform.IsMac)
         {
            //BringToFront(); //may or may not be needed
         }
         //what do you suppose will be the weird thing I have to account for on Linux?

         
         // create toolbar			
         /*ToolBar = new ToolBar { Items = { clickMe } };*/
      }
      private static Uri GetSavesUri()
      {
         string savesFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
         if (EtoEnvironment.Platform.IsMac)
         {
            savesFolderPath = Path.Combine(savesFolderPath, "Library", "Application Support");
         }
         else if (EtoEnvironment.Platform.IsWindows)
         {
            savesFolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Introversion");
         }
         else if (EtoEnvironment.Platform.IsLinux) //this reflects TLS when run on Linux through Proton; must be updated if TLS is ever ported
         {
            savesFolderPath = Path.Combine(savesFolderPath
               , "Steam", "steamapps", "compat", "1857080", "pfx", "drive_c", "users", "steamuser", "AppData", "Local", "Introversion");
         }
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
         //OpenFileDialog foo = new OpenFileDialog();
         //foo.Directory = savesFolder;
         //foo.Filters.Add("Last Starship save files|*.space");
         //if (foo.ShowDialog(this) == DialogResult.Ok)
         //{
         //   //this.Cursor = Cursors.; they don't have a waiting cursor; todo: guess I'll add my own--later!
         //   saveFilePath = foo.FileName;
         //   saveFile = new SaveFilev2(saveFilePath);
         //   UpdateTextbox("saveFileTextbox", saveFilePath);
         //   Eto.Forms.Form progressForm = GetProgressForm();
         //   progressForm.Show();
         //   SaveFilev2.LoadFile(saveFile, saveFilePath);
         //   if (progressForm != null && progressForm.Visible) progressForm.Close();
         //   UpdateTextbox("gameModeTextbox", saveFile.GameMode);
         //   UpdateTextbox("saveVersionTextbox", saveFile.SaveVersion.ToString());
         //   UpdateTextbox("nextIdTextbox", saveFile.NextId.ToString());
         //   UpdateTextbox("timeIndexTextbox", saveFile.TimeIndex.ToString());
         //   UpdateTextbox("deltaTimeTextbox", saveFile.DeltaTime.ToString());
         //   UpdateTextbox("playTimeTextbox", saveFile.PlayTime.ToString());
         //   UpdateTreeView(); //todo: this is all trash; find a better way
         //}
         //DynamicLayout dyla = (DynamicLayout)Content;
         //GridView leftgrid = (GridView)dyla.Children.Where<Control>(x => (string)x.Tag == "LeftListBox").First();
         //GridView rightgrid = (GridView)dyla.Children.Where<Control>(x => (string)x.Tag == "RightListBox").First();
         //foreach (var item in ItemList)
         //{
         //   ((ObservableCollection<string>)leftgrid.DataStore).Add(item);
         //}
         Debug.WriteLine($"{InventoryMasterList.Count}");
      }
      private static Form GetProgressForm() // todo: fix this whole thing
      {
         Eto.Forms.Form progressForm = new Form();
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
            AutoSize = true,
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
      /// <summary>
      /// This method changes the point's location by the given x- and y-offsets.
      /// <example>
      /// For example:
      /// <code>
      /// Point p = new Point(3,5);
      /// p.Translate(-1,3);
      /// </code>
      /// results in <c>p</c>'s having the value (2,8).
      /// </example>
      /// </summary>
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
         ObservableCollection<InventoryGridItem> copyofinvlist = new ObservableCollection<InventoryGridItem>();
         foreach(var v in InventoryMasterList)
         {
            copyofinvlist.Add(new InventoryGridItem(v, 0));
         }
         var ListBuilder = new ListBuilder(copyofinvlist, new List<string> { "Remaining inventory" }
                                          , items, new List<string> { "In stock", "Count" });
         //var gridview = (GridView)ListBuilder.Children.Where<Control>(x => (string)x.Tag == "LeftListBox").First();

         sp.Panel2 = ListBuilder;
         //DetailScrollable.Padding = new Padding(5, 5);
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
      private static StackLayout CreateDetailsPanel(TreeGridItem item)
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
            s.Content = CreateDetailsPanel(item);
         }
      }
   }
}
