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

namespace LaSSI
{
   partial class MainForm : Form
   {
      private System.Uri savesFolder;
      private string saveFilePath;
      private SaveFilev2 saveFile;
      private List<InventoryGridItem> InventoryMasterList;
      private Dictionary<TreeGridItem, DynamicLayout> DetailPanelsCache;
      void InitializeComponent()
      {
         //this.SizeChanged += MainForm_SizeChanged;
         Title = "LaSSI (Last Starship Save Inspector)";
         MinimumSize = new Size(200, 200);
         Size = new Size(1024, 600);
         Location = AdjustForFormSize(GetScreenCenter(), Size);
         Padding = 10;
         savesFolder = GetSavesUri();
         InventoryMasterList = LoadInventoryMasterList();
         DetailPanelsCache = new Dictionary<TreeGridItem, DynamicLayout>();

         var openFileCommand = CreateOpenFileCommand();
         var quitCommand = CreateQuitCommand();
         var prefsCommand = new Command(PrefsCommand_Executed);
         var aboutCommand = CreateAboutCommand();
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
         PlatformSpecificNonsense();
         // create toolbar			
         /*ToolBar = new ToolBar { Items = { clickMe } };*/
      }
      /// <summary>
      /// This method takes care of any platform-specific setup/steps at the end of the mainform init phase
      /// </summary>
      private void PlatformSpecificNonsense()
      {
         if (EtoEnvironment.Platform.IsWindows)
         {
            Focus(); //required to prevent focus from being on the menu bar when the app launches on Windows
         }
         else if (EtoEnvironment.Platform.IsMac)
         {
            //BringToFront(); //may or may not be needed
         }
         //what do you suppose will be the weird thing I have to account for on Linux?
      }
      private Command CreateAboutCommand()
      {
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
         return aboutCommand;
      }
      private static Command CreateQuitCommand()
      {
         var quitCommand = new Command { MenuText = "Quit", Shortcut = Application.Instance.CommonModifier | Keys.Q };
         quitCommand.Executed += (sender, e) => Application.Instance.Quit();
         return quitCommand;
      }
      private Command CreateOpenFileCommand()
      {
         var openFileCommand = new Command { MenuText = "Open", Shortcut = Application.Instance.CommonModifier | Keys.O };
         openFileCommand.Executed += OpenFileCommand_Executed;
         return openFileCommand;
      }
      private static List<InventoryGridItem> LoadInventoryMasterList()
      {
         var InventoryMasterList = new List<InventoryGridItem>();
         using (var ItemListStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("LaSSI.InventoryMasterList.txt"))
         {
            TextReader reader = new StreamReader(ItemListStream!);
            string[] ItemListArray = reader.ReadToEnd().Split(Environment.NewLine);
            foreach (var line in ItemListArray)
            {
               InventoryMasterList.Add(new InventoryGridItem(line, 0));
            }
         }
         return InventoryMasterList;
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
         { // todo: this kinda sucks -- surely there's any number of better ways
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
      private void OpenFileCommand_Executed(object? sender, EventArgs e) //todo: make this not suck
      {
         OpenFileDialog fileDialog = new OpenFileDialog();
         fileDialog.Directory = savesFolder;
         fileDialog.Filters.Add("Last Starship save files|*.space");
         if (fileDialog.ShowDialog(this) == DialogResult.Ok)
         {
            //this.Cursor = Cursors.; they don't have a waiting cursor; todo: guess I'll add my own--later!
            saveFilePath = fileDialog.FileName;
            saveFile = new SaveFilev2(saveFilePath);
            Form progressForm = GetProgressForm();
            progressForm.Show();
            SaveFilev2.LoadFile(saveFile, saveFilePath);
            if (progressForm != null && progressForm.Visible) progressForm.Close();
            UpdateUiAfterLoad();
         }
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
      private void UpdateUiAfterLoad() // this method used to matter more when there were more independant textboxes to update
      {
         DetailPanelsCache.Clear();
         ClearDetails();
         UpdateTextbox("saveFileTextbox", saveFilePath);
         UpdateTreeView(); //todo: is there a better way to do this instead of regenerating the entire tree?

      }
      private void ClearDetails()
      {
         DynamicLayout bar = (DynamicLayout)this.Content;
         Scrollable x = (Scrollable)bar.Children.Where<Control>(x => (string)x.Tag == "DetailScrollable").First();
         x.Content = null;
      }
      private void UpdateTreeView()
      {
         DynamicLayout bar = (DynamicLayout)this.Content;
         TreeGridView x = (TreeGridView)bar.Children.Where<Control>(x => (string)x.Tag == "DataTreeView").First();
         x.DataStore = SaveFile2TreeGridItems();
         if(x.Columns.Count == 0)
         {
            x.Columns.Add(new GridColumn
            {
               AutoSize = true,
               DataCell = new TextBoxCell(0)
            });
         }
      }
      private TreeGridItemCollection SaveFile2TreeGridItems()
      {
         TreeGridItemCollection treeGridItems = new TreeGridItemCollection
         {
            WalkNodeTree(saveFile.Root)
         };
         treeGridItems[0].Expanded = true;
         return treeGridItems;
      }
      private TreeGridItem WalkNodeTree(Node node)
      {
         if (!node.HasChildren())
         {
            return new TreeGridItem(node.Name, node.Properties)
            {
               Tag = node.Name
            };
         }
         else
         {
            TreeGridItemCollection childItems = new TreeGridItemCollection();
            foreach (var child in node.Children)
            {
               childItems.Add(WalkNodeTree(child));
            }
            return new TreeGridItem(childItems, node.Name, node.Properties)
            {
               Tag = node.Name
            };
         }
      }
      /// <summary>
      /// Updates the text of a textbox matching the given tag.
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
         //rootLayout.Add(InitSaveStatsPanel());
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
      private TableLayout InitDetailsPanel()
      {
         TableLayout dataLayout = new TableLayout();

         TreeGridView treeView = new TreeGridView();
         treeView.SelectedItemChanged += TreeView_SelectedItemChanged;
         treeView.Tag = "DataTreeView";
         treeView.AllowEmptySelection = true;
         treeView.AllowMultipleSelection = true;
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
         sp.Panel2 = DetailScrollable;
         dataLayout.Rows.Add(sp);

         return dataLayout;
      }
      private static Label CreateDetailLabel(string text)
      {
         return new Label
         {
            Text = text,
            VerticalAlignment = VerticalAlignment.Center,
            Wrap = WrapMode.Word
         };
      }
      private static TextBox CreateDetailTextBox(string text)
      {
         int multiplicand = text.Length > 2 ? text.Length : 2;
         return new TextBox
         {
            Text = text,
            Width = multiplicand * 10
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
      private static StackLayout CreateDefaultFields(TreeGridItem item)
      {
         StackLayout defaultLayout = new StackLayout();
         foreach (var s in item.Values)
         {
            if (s is string)
            {
               //detailsLayout.Items.Add(new StackLayoutItem(new Label().Text = s.ToString()));
            }
            else if (s is Dictionary<string, string> dictionary && dictionary.Count != 0)
            {
               TableLayout innerLayout = new TableLayout
               {
                  Spacing = new Size(5, 0),
                  Padding = new Padding(0, 5)
               };
               foreach (var p in dictionary)
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
               defaultLayout.Items.Add(new StackLayoutItem(innerLayout));
            }
         }
         return defaultLayout;
      }
      private ListBuilder CreateListBuilder(TreeGridItem item)
      {
         ObservableCollection<InventoryGridItem> InStock = new ObservableCollection<InventoryGridItem>();
         ObservableCollection<InventoryGridItem> RemainingItems = new ObservableCollection<InventoryGridItem>(InventoryMasterList);
         foreach (var value in item.Values)
         {
            if(value is Dictionary<string,string> dictionary && dictionary.Count != 0)
            {
               foreach(var entry in dictionary)
               {
                  try
                  {
                     InventoryGridItem InvItem = RemainingItems.Where(x => x.Name == entry.Key).First();
                     RemainingItems.Remove(InvItem);
                     InvItem.Count = int.Parse(entry.Value);
                     InStock.Add(InvItem);
                  }
                  catch
                  {
                     Debug.WriteLine($"Item not found in master list: {entry.Key}");
                  }
               }
            }
         }
         return new ListBuilder(RemainingItems, new List<string> { "Remaining items" }, InStock, new List<string> { "In stock", "Count" });
      }
      private static string GetNodePath(TreeGridItem item)
      {
         string path = item.Values[0].ToString()!;
         while (item.Parent != null && item.Parent.Parent != null)
         {
            item = (TreeGridItem)item.Parent;
            path = $"{item.Values[0]}/{path}";
         }
         return path;
      }
      private static Label GetNodePathLabel(TreeGridItem item)
      {
         Label nodePathLabel = new Label { Text = GetNodePath(item), BackgroundColor = Colors.Silver, Font = new Font("Arial", 18, FontStyle.Bold) };
         return nodePathLabel;
      }
      private static DynamicLayout CreateNodePathLayout(TreeGridItem item)
      {
         DynamicLayout layout = new DynamicLayout();
         layout.Add(GetNodePathLabel(item));
         return layout;
      }
      private DynamicLayout CreateDetailsLayout(TreeGridItem item)
      {
         DynamicLayout detailsLayout = new()
         {
            Padding = new Padding(5, 0),
         };

         detailsLayout.Add(GetNodePathLabel(item));
         switch (item.Tag)
         {
            case "OurStock":
               {
                  detailsLayout.Add(CreateListBuilder(item));
                  break;
               }
            case "TheirStock":
               {
                  detailsLayout.Add(CreateListBuilder(item));
                  break;
               }
            case "Cells":
               {
                  detailsLayout.Add(CreateShipCellsLayout(item));
                  break;
               }
            default:
               {
                  detailsLayout.Add(CreateDefaultFields(item));
                  break;
               }
         }
         
         return detailsLayout;
      }

      private static TextArea CreateShipCellsLayout(TreeGridItem item)
      {
         //string cells = string.Empty;
         ////string row = string.Empty;
         //if (item.Values[1] is Dictionary<string, string> dictionary && dictionary.Count != 0)
         //{
         //   foreach(var p in dictionary)
         //   {
         //      //row = p.Key + "  " + p.Value + Environment.NewLine;
         //      cells += p.Key + "  " + p.Value + Environment.NewLine;
         //   }
         //}
         ////return new(new TextArea() { ReadOnly = true, Font = new Font("Courier", 12), Text = cells, });
         //return new TextArea() { ReadOnly = true, Font = new Font("Courier", 12), Text = cells, };
         return new TextArea() { ReadOnly = true, Font = new Font("Courier", 12)
            , Text = $"Coming soon!{Environment.NewLine}(Look, I tried, but it's going to take a whole new module to make the ship layout inspector usable and we both have way bigger fish to fry.)" };
      }

      private void TreeView_SelectedItemChanged(object? sender, EventArgs e)
      {
         if (sender is not null and TreeGridView)
         {
            TreeGridItem item = (TreeGridItem)((TreeGridView)sender).SelectedItem;
            if(item is not null)
            {
               UpdateDetailsPanel(item);
            }
            else
            {
               ClearDetailsPanel();
            }
         }
      }
      private void UpdateDetailsPanel(TreeGridItem item)
      {
         DynamicLayout bar = (DynamicLayout)this.Content;
         Scrollable s = (Scrollable)bar.Children.Where<Control>(x => (string)x.Tag == "DetailScrollable").First();
         if (!DetailPanelsCache.ContainsKey(item))
         {
            DetailPanelsCache.Add(item, CreateDetailsLayout(item));
         }
         s.Content = DetailPanelsCache[item];
      }
      private void ClearDetailsPanel()
      {
         DynamicLayout bar = (DynamicLayout)this.Content;
         Scrollable s = (Scrollable)bar.Children.Where<Control>(x => (string)x.Tag == "DetailScrollable").First();
         s.Content = null;
      }
   }
}
