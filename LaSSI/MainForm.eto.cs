using Eto;
using Eto.Drawing;
using Eto.Forms;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace LaSSI
{
   enum NodeStatus
   {
      Default,
      Edited,
      ChildEdited,
      Deleted
   }
   partial class MainForm : Form
   {
      private System.Uri savesFolder = GetSavesUri();
      private string saveFilePath = string.Empty;
      private SaveFilev2 saveFile = new();
      private List<InventoryGridItem> InventoryMasterList = LoadInventoryMasterList();
      private readonly string FileFormat = "Last Starship save files|*.space";
      private ProgressBar LoadingBar = new();
      private DataPanel DataPanel;
      void InitializeComponent()
      {
         //this.SizeChanged += MainForm_SizeChanged;
         Title = "LaSSI (Last Starship Save Inspector)";
         MinimumSize = new Size(200, 200);
         Size = new Size(1024, 600);
         Location = AdjustForFormSize(GetScreenCenter(), Size);
         Padding = 10;
         DataPanel = new DataPanel(InventoryMasterList, Width);
         var openFileCommand = CustomCommands.CreateOpenFileCommand(OpenFileCommand_Executed);
         var saveFileAsCommand = CustomCommands.CreateSaveFileAsCommand(SaveFileAsCommand_Executed);
         var quitCommand = CustomCommands.CreateQuitCommand();
         /*var prefsCommand = new Command(PrefsCommand_Executed);*/
         var cleanDerelictsCommand = CustomCommands.CreateCleanDerelictsCommand(CleanDerelicts_Executed);
         var fixAssertionFailedCommand = CustomCommands.CreateFixAssertionFailedCommand(FixAssertionFailed_Executed);
         var resetCameraCommand = CustomCommands.CreateResetCameraCommand(ResetCamera_Executed);
         // ^^^ this is getting out of hand
         // create menu
         Menu = new MenuBar
         {
            Items =
            {
               // File submenu
               new SubMenuItem { Text = "&File", Items = { openFileCommand, saveFileAsCommand } },
               new SubMenuItem { Text = "&Tools", Items = { cleanDerelictsCommand, fixAssertionFailedCommand, resetCameraCommand } },
               // new SubMenuItem { Text = "&View", Items = { /* commands/items */ } },
            },
            //ApplicationItems =
            //{
            //   // application (OS X) or file menu (others)
            //   new ButtonMenuItem { Text = "&Preferences...", Command = prefsCommand, Shortcut = Application.Instance.CommonModifier | Keys.Comma },
            //},
            QuitItem = quitCommand,
            AboutItem = new AboutCommand(this)

         };
         LoadingBar = new ProgressBar()
         {
            Visible = false,
            Indeterminate = true
         };
         Content = InitMainPanel();
         PlatformSpecificNonsense();
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
            BringToFront(); //may or may not be needed
         }
         //what do you suppose will be the weird thing I have to account for on Linux?
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
      private void UpdateUiAfterLoad()
      {
         UpdateTextbox("saveFileTextbox", saveFilePath);
         DataPanel.Rebuild(saveFile.Root);
         LoadingBar.Visible = false;
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
      private DynamicLayout InitMainPanel()
      {
         DynamicLayout rootLayout = new()
         {
            Spacing = new Size(0, 5)
         };
         rootLayout.Add(InitFilePanel());
         rootLayout.Add(LoadingBar);
         rootLayout.Add(DataPanel);
         return rootLayout;
      }
      private static TableLayout InitFilePanel()
      {
         TableLayout fileLayout = new()
         {
            Spacing = new Size(5, 0)
         };
         TextBox filename = new();
         Label currentFile = new()
         {
            Text = "Current file:",
            VerticalAlignment = VerticalAlignment.Center
         };
         filename.Tag = "saveFileTextbox";
         filename.Width = 500;
         filename.Enabled = false;
         fileLayout.Rows.Add(new TableRow(currentFile, new TableCell(filename, true), new TableCell(null)));

         return fileLayout;
      }

      #region event handlers
      private void SaveFileAsCommand_Executed(object? sender, EventArgs e)
      {
         if (!DataPanel.ReadyForSave()) return;
         string barefilename = Path.GetFileNameWithoutExtension(saveFilePath);
         string dateappend = @"-\d{8}-\d{4}";
         Match m = Regex.Match(barefilename, dateappend);
         if (m.Success)
         {
            string foo = barefilename[..m.Index];
            barefilename = foo;
         }
         string date = DateTime.Now.ToString("yyyyMMdd-HHmm");
         string proposedfilename = $"{barefilename}-{date}.space";
         SaveFileDialog saveDialog = new()
         {
            Directory = savesFolder,
            FileName = proposedfilename,
         };
         saveDialog.Filters.Add(FileFormat);
         LoadingBar.Visible = true;
         if (saveDialog.ShowDialog(this) == DialogResult.Ok)
         {
            Debug.WriteLine($"{saveDialog.FileName}");
            DynamicLayout bar = (DynamicLayout)this.Content;
            TreeGridView x = (TreeGridView)bar.Children.Where<Control>(x => (string)x.Tag == "DataTreeView").First();
            TreeGridItem y = (TreeGridItem)(x.DataStore as TreeGridItemCollection)![0];
            FileWriter writer = new FileWriter();
            bool success = writer.WriteFile(y, saveDialog.FileName);
            LoadingBar.Visible = false;
         }
         else
         {
            LoadingBar.Visible = false;
         }
      }
      private void OpenFileCommand_Executed(object? sender, EventArgs e) //todo: make this not suck
      {
         OpenFileDialog fileDialog = new()
         {
            Directory = savesFolder
         };
         fileDialog.Filters.Add(FileFormat);
         LoadingBar.Visible = true;
         if (fileDialog.ShowDialog(this) == DialogResult.Ok)
         {
            //this.Cursor = Cursors.; they don't have a waiting cursor; todo: guess I'll add my own--later!

            saveFilePath = fileDialog.FileName;
            saveFile = new SaveFilev2(saveFilePath);
            saveFile.Load();
            UpdateUiAfterLoad();
            CustomCommands.EnableSaveAs(this.Menu);
            CustomCommands.EnableTools(Menu, DataPanel);
         }
         else
         {
            LoadingBar.Visible = false;
         }
      }
      internal void FixAssertionFailed_Executed(object? sender, EventArgs e)
      {
         if (DataPanel.AssertionFailureConditionExists(true))
         {
            _ = MessageBox.Show("Mission reassigned successfully", MessageBoxButtons.OK, MessageBoxType.Information, MessageBoxDefaultButton.OK);
         }
      }

      internal void CleanDerelicts_Executed(object? sender, EventArgs e)
      {
         RadioInputDialog r = new RadioInputDialog("Clean derelicts", new string[] { "current system", "sector-wide", /*"specific system"*/ });
         r.ShowModal(this);
         DialogResult d = r.GetDialogResult();
         if (d == DialogResult.Ok)
         {
            Debug.WriteLine(r.GetSelectedIndex());
            DataPanel.CleanDerelicts((DataPanel.DerelictsCleaningMode)r.GetSelectedIndex());
            _ = MessageBox.Show("Derelict ships removed", MessageBoxButtons.OK, MessageBoxType.Information, MessageBoxDefaultButton.OK);
         }
      }
      internal void ResetCamera_Executed(object? sender, EventArgs e)
      {
         //RadioInputDialog r = new RadioInputDialog("Reset camera to...", new string[] { "system center", "nearest friendly ship" });
         //r.ShowModal(this);
         //DialogResult d = r.GetDialogResult();
         //if (d == DialogResult.Ok)
         //{
         //   Debug.WriteLine(r.GetSelectedIndex());

         //}
         if (DataPanel.ResetCamera())
         {
            _ = MessageBox.Show("Camera reset to system center, viewsize 100", MessageBoxButtons.OK, MessageBoxType.Information, MessageBoxDefaultButton.OK);
         }
      }
      private void PrefsCommand_Executed(Object? sender, EventArgs e)
      {
         var dlg = new Modal(new List<string> { "Preferences not implemented" });
         //dlg.Content.
         dlg.ShowModal(Application.Instance.MainForm);
      }
      #endregion event handlers
   }
}
