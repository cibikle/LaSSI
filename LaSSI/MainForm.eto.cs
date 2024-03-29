using Eto;
using Eto.Drawing;
using Eto.Forms;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

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
      internal System.Uri savesFolder = GetSavesUri();
      internal string saveFilePath = string.Empty;
      internal SaveFilev2 saveFile = new();
      internal string backupDirectory = string.Empty;
      internal List<InventoryGridItem> InventoryMasterList = LoadInventoryMasterList();
      internal readonly string FileFormat = "Last Starship save files|*.space";
      internal ProgressBar LoadingBar = new();
      internal DataPanel DataPanel;
      internal CustomCommands? CustomCommands;
      internal SubMenuItem fileMenu;
      internal Prefs prefs;
      internal char WindowsMenuPrefix = '&';
      void InitializeComponent()
      {
         Closing += MainForm_Closing;
         Title = "LaSSI (the Last Starship Save Inspector)";
         MinimumSize = new Size(200, 200);
         Size = new Size(1024, 600);
         Location = AdjustForFormSize(GetScreenCenter(), Size);
         Padding = 10;
         DataPanel = new DataPanel(this, InventoryMasterList, Width);
         CustomCommands = new CustomCommands(this);
         prefs = new Prefs(this);
         //prefs.PrefsDialog.UiRefreshRequired += PrefsDialog_UiRefreshRequired;
         // create menu
         fileMenu = new() { Text = "&File" }; //the & is used in Windows only, ignored in Mac, and stripped out in Linux
         fileMenu.Items.AddRange(CustomCommands.FileCommands);
         SubMenuItem toolsMenu = new() { Text = "&Tools" }; //the & is used in Windows only, ignored in Mac, and stripped out in Linux
         toolsMenu.Items.AddRange(CustomCommands.ToolsList);
         Menu = new MenuBar
         {
            Items =
            {
               // File submenu
               fileMenu,
               toolsMenu
            // new SubMenuItem { Text = "&View", Items = { /* commands/items */ } },
            },
            ApplicationItems =
            {
               // application (OS X) or file menu (others)
               CustomCommands.CreatePrefsMenuItem(CustomCommands.prefsCommand),
            },
            QuitItem = CustomCommands.QuitCommand,
            AboutItem = new AboutCommand(this)

         };
         LoadingBar = new ProgressBar()
         {
            Visible = false,
            Indeterminate = true
         };
         Content = InitMainPanel();
         PlatformSpecificNonsense();
         Startup();
      }

      //private void PrefsDialog_UiRefreshRequired(object? sender, EventArgs e)
      //{
      //   DataPanel.RefreshTree();
      //}

      private void MainForm_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
      {
         if (CustomCommands != null)
         {
            e.Cancel = !CustomCommands.ReadyForQuit();
         }
      }

      internal void Startup()
      {
         if (CustomCommands is not null && prefs.FindPref("Startup behavior") is not null and Pref startupBehavior)
         {
            switch (startupBehavior.value)
            {
               /*case StartupBehavior.ShowFileChooser:
                  {
                     CustomCommands.OpenFileExecute();
                     break;
                  }*/
               case StartupBehavior.LoadFile:
               case StartupBehavior.LoadLastFile:
                  {
                     if (prefs.FindPref("Startup file") is not null and Pref startupFile && startupFile.value is not null and string filename && !string.IsNullOrEmpty(filename))
                     {
                        CustomCommands.LoadFile(Path.Combine(savesFolder.OriginalString, filename));
                     }
                     break;
                  }
            }
         }
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
#if DEBUG
            BringToFront(); // this is useful for dev but causes a problem with release builds
#endif
            Closeable = false;
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
            , ".local/share/Steam/steamapps/compatdata/1857080/pfx/drive_c/users/steamuser/AppData/Local/Introversion");
               //, "Steam", "steamapps", "compat", "1857080", "pfx", "drive_c", "users", "steamuser", "AppData", "Local", "Introversion");
         }
         savesFolderPath = Path.Combine(savesFolderPath, "LastStarship", "saves");
         Uri SavesUri = new(savesFolderPath);
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
      internal void UpdateUiAfterLoad()
      {
         _ = UpdateTextbox("saveFileTextbox", TrimFilePathForSafety(saveFilePath));
         DataPanel.Rebuild(saveFile.Root);
         LoadingBar.Visible = false;
         DataPanel.GetSearchBox().Text = string.Empty;
      }
      internal static string TrimFilePathForSafety(string filepath)
      {
         char pathSeparator = '/';
         if (EtoEnvironment.Platform.IsWindows) pathSeparator = '\\';
         int index = 0;
         int count = 0;
         int target = 3;
         for (int i = 0; i < filepath.Length && count < target; i++)
         {
            if (filepath[i] == pathSeparator)
            {
               count++;
               if (count == target)
               {
                  index = i;
               }
            }
         }

         return "~" + filepath[index..];
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
   }
}
