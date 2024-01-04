using Eto.Forms;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace LaSSI
{
   internal class CustomCommands
   {
      internal List<Command> FileCommands { get; set; } = new List<Command>();
      internal Command QuitCommand { get; set; }
      internal List<Command> ToolsList { get; set; } = new List<Command>();
      internal MainForm MainForm;
      internal Command prefsCommand { get; }

      public CustomCommands(MainForm mainForm)
      {
         MainForm = mainForm;
         FileCommands.Add(CreateOpenFileCommand(OpenFileCommand_Executed));
         FileCommands.Add(CreateSaveFileAsCommand(SaveFileAsCommand_Executed));
         FileCommands.Add(CreateBrowseSavesCommand(BrowseSavesCommand_Executed));
         FileCommands.Add(CreateBrowseBackupsCommand(BrowseBackupsCommand_Executed));
         QuitCommand = CreateQuitCommand(QuitCommand_Executed);
         ToolsList.Add(CreateCleanDerelictsCommand(CleanDerelicts_Executed));
         ToolsList.Add(CreateFixAssertionFailedCommand(FixAssertionFailed_Executed));
         ToolsList.Add(CreateResetCameraCommand(ResetCamera_Executed));
         ToolsList.Add(CreateResetCometCommand(ResetComet_Executed));
         ToolsList.Add(CreateTurnOffMeteorsCommand(TurnOffMeteors_Executed));
         ToolsList.Add(CreateCrossSectorMissionFixCommand(CrossSectorMissionFix_Executed));
         ToolsList.Add(CreateCleanupDeadCrew_Command(CleanupDeadCrew_Executed));
         ToolsList.Add(CreateRemoveHab_Command(RemoveHab_Executed));
         prefsCommand = new Command(PrefsCommand_Executed);
      }

      #region tools
      internal static Command CreateCleanDerelictsCommand(EventHandler<EventArgs> CleanDerelicts_Executed)
      {
         var cleanDerelicts = new Command
         {
            MenuText = "Clean derelicts",
            Shortcut = Application.Instance.CommonModifier | Keys.D,
            ID = "CleanDerelictsTool"
         };
         cleanDerelicts.Executed += CleanDerelicts_Executed;
         cleanDerelicts.Enabled = false;
         return cleanDerelicts;
      }
      internal static Command CreateFixAssertionFailedCommand(EventHandler<EventArgs> FixAssertionFailed_Executed)
      {
         var fixAssertionFailed = new Command
         {
            MenuText = "Fix assertion failed",
            ID = "FixAssertionFailedTool"
         };
         fixAssertionFailed.Executed += FixAssertionFailed_Executed;
         fixAssertionFailed.Enabled = false;
         return fixAssertionFailed;
      }
      internal static Command CreateResetCameraCommand(EventHandler<EventArgs> ResetCamera_Executed)
      {
         var resetCamera = new Command
         {
            MenuText = "Reset camera",
            ID = "ResetCameraTool"
         };
         resetCamera.Executed += ResetCamera_Executed;
         resetCamera.Enabled = false;
         return resetCamera;
      }
      internal static Command CreateResetCometCommand(EventHandler<EventArgs> ResetComet_Executed)
      {
         var resetCamera = new Command
         {
            MenuText = "Reset comet(s)",
            ID = "ResetCometTool"
         };
         resetCamera.Executed += ResetComet_Executed;
         resetCamera.Enabled = false;
         return resetCamera;
      }
      internal static Command CreateTurnOffMeteorsCommand(EventHandler<EventArgs> TurnOffMeteors_Executed)
      {
         var turnOffMeteors = new Command
         {
            MenuText = "Turn off meteors",
            ID = "TurnOffMeteorsTool"
         };
         turnOffMeteors.Executed += TurnOffMeteors_Executed;
         turnOffMeteors.Enabled = false;
         return turnOffMeteors;
      }
      internal static Command CreateCrossSectorMissionFixCommand(EventHandler<EventArgs> CrossSectorMissionFix_Executed)
      {
         var crossSectorMissionFix = new Command
         {
            MenuText = "Fix cross-sector passenger missions",
            ID = "CrossSectorMissionFixTool"
         };
         crossSectorMissionFix.Executed += CrossSectorMissionFix_Executed;
         crossSectorMissionFix.Enabled = false;
         return crossSectorMissionFix;
      }
      internal static Command CreateCleanupDeadCrew_Command(EventHandler<EventArgs> CleanupDeadCrew_Executed)
      {
         var cleanupDeadCrewCommand = new Command
         {
            MenuText = "Clean up dead crew",
            ID = "CleanupDeadCrewTool"
         };
         cleanupDeadCrewCommand.Executed += CleanupDeadCrew_Executed;
         cleanupDeadCrewCommand.Enabled = false;
         return cleanupDeadCrewCommand;
      }
      internal static Command CreateRemoveHab_Command(EventHandler<EventArgs> RemoveHab_Executed)
      {
         var cleanupDeadCrewCommand = new Command
         {
            MenuText = "Remove Habitation from ship",
            ID = "RemoveHabTool"
         };
         cleanupDeadCrewCommand.Executed += RemoveHab_Executed;
         cleanupDeadCrewCommand.Enabled = false;
         return cleanupDeadCrewCommand;
      }
      #endregion tools
      #region commands
      internal static ButtonMenuItem CreatePrefsMenuItem(Command prefsCommand)
      {
         return new ButtonMenuItem { Text = "&Preferences...", Command = prefsCommand, Shortcut = Application.Instance.CommonModifier | Keys.Comma };
      }
      internal static Command CreatePrefsCommand(EventHandler<EventArgs> PrefsCommand_Executed)
      {
         var prefsCommand = new Command
         {
            MenuText = "&Preferences",
            Shortcut = Application.Instance.CommonModifier | Keys.Comma,
            ID = "PrefsCommand"
         };
         prefsCommand.Executed += PrefsCommand_Executed;
         return prefsCommand;
      }
      internal static Command CreateQuitCommand(EventHandler<EventArgs> QuitCommand_Executed)
      {
         var quitCommand = new Command
         {
            MenuText = "Quit",
            Shortcut = Application.Instance.CommonModifier | Keys.Q,
            ID = "QuitCommand"
         };
         quitCommand.Executed += QuitCommand_Executed;
         return quitCommand;
      }
      internal static Command CreateOpenFileCommand(EventHandler<EventArgs> OpenFileCommand_Executed)
      {
         var openFileCommand = new Command
         {
            MenuText = "Open",
            Shortcut = Application.Instance.CommonModifier | Keys.O,
            ID = "OpenFileCommand"
         };
         openFileCommand.Executed += OpenFileCommand_Executed;
         return openFileCommand;
      }
      internal static Command CreateSaveFileAsCommand(EventHandler<EventArgs> SaveFileAsCommand_Executed)
      {
         var saveFileAsCommand = new Command
         {
            MenuText = "Save As",
            Shortcut = Application.Instance.CommonModifier | /*Keys.Shift |*/ Keys.S, // todo: after Save is implemented, put the Shift back
            Enabled = false,
            ID = "SaveFileAsCommand"
         };
         saveFileAsCommand.Executed += SaveFileAsCommand_Executed;
         return saveFileAsCommand;
      }
      internal static Command CreateBrowseSavesCommand(EventHandler<EventArgs> BrowseSavesCommand_Executed)
      {
         var browseBackupsCommand = new Command
         {
            MenuText = "Browse saves",
            Shortcut = Application.Instance.CommonModifier | Keys.B,
            Enabled = true,
            ID = "BrowseSavesCommand"
         };
         browseBackupsCommand.Executed += BrowseSavesCommand_Executed;
         return browseBackupsCommand;
      }
      internal static Command CreateBrowseBackupsCommand(EventHandler<EventArgs> BrowseBackupsCommand_Executed)
      {
         var browseBackupsCommand = new Command
         {
            MenuText = "Browse backups",
            Shortcut = Application.Instance.CommonModifier | Keys.Shift | Keys.B,
            Enabled = true,
            ID = "BrowseBackupsCommand"
         };
         browseBackupsCommand.Executed += BrowseBackupsCommand_Executed;
         return browseBackupsCommand;
      }
      #endregion commands
      internal void OpenFileExecute()
      {
         Command openfile = FileCommands.First(x => x.ID == "OpenFileCommand");
         openfile.Execute();
      }

      #region utility
      internal static void EnableSaveAs(MenuBar menu)
      {
         var SaveAsCommand = ((SubMenuItem)menu.Items.First(menuItem => menuItem.Text == "&File")).Items.Select(submenuItem
            => submenuItem.Command as Command).First(command => command != null && command.ID == "SaveFileAsCommand");
         if (SaveAsCommand is not null) SaveAsCommand.Enabled = true;
      }
      internal static void EnableTools(MenuBar menu, DataPanel data)
      {
         var ToolsMenu = (SubMenuItem)menu.Items.First(menuItem => menuItem.Text == "&Tools");
         if (ToolsMenu is not null)
         {
            foreach (var o in ToolsMenu.Items)
            {
               o.Enabled = CheckEnablablility(o, data);
            }

         }
      }
      internal static bool CheckEnablablility(MenuItem tool, DataPanel data) // probably ought to revise this
      {
         bool enablability = false;
         switch (tool.ID)
         {
            case "CleanDerelictsTool":
               {
                  enablability = data.DerelictsPresent();
                  break;
               }
            case "FixAssertionFailedTool":
               {
                  enablability = data.AssertionFailureConditionExists();
                  break;
               }
            case "ResetCameraTool":
               {
                  enablability = true;
                  break;
               }
            case "ResetCometTool":
               {
                  enablability = data.CometExists();
                  break;
               }
            case "TurnOffMeteorsTool":
               {
                  enablability = data.DetectMeteors();
                  break;
               }
            case "CrossSectorMissionFixTool":
               {
                  enablability = data.CrossSectorMissionsExist();
                  break;
               }
            case "CleanupDeadCrewTool":
               {
                  enablability = data.FindDeadCrew();
                  break;
               }
            case "RemoveHabTool":
               {
                  enablability = true; // todo: probably expand this
                  break;
               }
         }

         return enablability;
      }
      internal bool ReadyForQuit()
      {
         // todo: add check to see if prefs need to be written to disk
         // todo: also maybe figure out how to prevent all this from running on start-up if the behavior is set to load file/open file
         switch ((StartupBehavior)MainForm.prefs.startupBehavior.value!)
         {
            case StartupBehavior.LoadLastFile:
               {
                  if (!string.IsNullOrEmpty(MainForm.saveFilePath))
                  {
                     MainForm.prefs.startupFile.SetValue(Path.GetFileName(MainForm.saveFilePath));
                  }
                  break;
               }
            case StartupBehavior.Nothing:
               //case StartupBehavior.ShowFileChooser:
               {
                  MainForm.prefs.startupFile.SetValue(string.Empty);
                  break;
               }
            default:
               {
                  break;
               }
         }
         MainForm.prefs.SavePrefs();
         bool readyForQuit;
         if (MainForm.DataPanel.DataStateMatches(DataPanel.DataState.Unchanged))
         {
            readyForQuit = true;
         }
         else
         {
            readyForQuit = GetReadyForQuit();
         }

         if (readyForQuit)
         {
            if (!string.IsNullOrEmpty(MainForm.backupDirectory))
            {
               CleanupBackup(MainForm.backupDirectory);
            }
         }

         return readyForQuit;
      }
      internal bool ReadyForSave()
      {
         if (MainForm.DataPanel.ChangesAreUnapplied())
         {
            return GetReadyForSave();
         }
         else
         {
            return true;
         }
      }
      internal bool GetReadyForQuit()
      {
         DialogResult result = GetPreferredAction((AlwaysNeverPrompt)MainForm.prefs.saveBeforeQuitting.value!, "Unsaved", "Save", "closing");

         switch (result)
         {
            case DialogResult.Yes:
               {
                  SaveFileAsCommand_Executed(null, null);
                  return MainForm.DataPanel.DataStateMatches(DataPanel.DataState.Unchanged);
               }
            case DialogResult.No:
               {
                  return true;
               }
            default:
               {
                  return false;
               }
         }
      }
      internal bool GetReadyForSave()
      {
         DialogResult result = GetPreferredAction((AlwaysNeverPrompt)MainForm.prefs.applyBeforeSaving.value!, "Unapplied", "Apply", "saving");

         switch (result)
         {
            case DialogResult.Yes:
               {
                  MainForm.DataPanel.ApplyAllChanges();
                  return true;
               }
            case DialogResult.No:
               {
                  MainForm.DataPanel.RevertAllUnappliedChanges();
                  return true;
               }
            default:
               {
                  return false;
               }
         }
      }
      internal static DialogResult GetPreferredAction(AlwaysNeverPrompt pref, string state, string action1, string action2)
      {
         switch (pref)
         {
            case AlwaysNeverPrompt.prompt:
               {
                  return PromptForSave(state, action1, action2);
               }
            case AlwaysNeverPrompt.always:
               {
                  return DialogResult.Yes;
               }
            case AlwaysNeverPrompt.never:
               {
                  return DialogResult.No;
               }
            default:
               {
                  return DialogResult.Cancel;
               }
         }
      }
      internal static DialogResult PromptForSave(string state, string action1, string action2)
      {

         return MessageBox.Show($"There are {state} changes!{Environment.NewLine}{action1} before {action2}?{Environment.NewLine}{state} changes will be discarded."
               , "Warning", MessageBoxButtons.YesNoCancel, MessageBoxType.Warning, MessageBoxDefaultButton.Yes);

      }
      internal void LoadFile(string filename, bool IsReloadAfterSave = false)
      {
         //this.Cursor = Cursors.; they don't have a waiting cursor; todo: guess I'll add my own--later!

         MainForm.saveFilePath = filename;
         MainForm.saveFile = new SaveFilev2(MainForm.saveFilePath);
         MainForm.saveFile.Load();
         MainForm.UpdateUiAfterLoad();
         EnableSaveAs(MainForm.Menu);
         EnableTools(MainForm.Menu, MainForm.DataPanel);
         MainForm.DataPanel.ResetDataState();

         if (!IsReloadAfterSave)
         {
            MainForm.backupDirectory = StartBackup(filename);
            Debug.WriteLine($"Backup started in {MainForm.backupDirectory}");
         }

      }
      internal static string StartBackup(string filename)
      {
         string date = DateTime.Now.ToString("yyyyMMdd-HHmmss");
         string appSupportDirectory = Path.Combine(Prefs.GetAppSupportDirectory(), $"backup_{date}");
         Directory.CreateDirectory(appSupportDirectory);
         string fileDestination = Path.Combine(appSupportDirectory, $"orig_{Path.GetFileName(filename)}");
         if (!File.Exists(fileDestination))
         {
            File.Copy(filename, fileDestination);
         }

         return appSupportDirectory;
      }
      internal static void AddToBackup(string backupDirectory, string filepath)
      {
         if (Directory.Exists(backupDirectory))
         {
            int numberOfFiles = Directory.GetFiles(backupDirectory).Length;

            string filename = $"{numberOfFiles}_{Path.GetFileName(filepath)}";
            string destination = Path.Combine(backupDirectory, filename);

            File.Copy(filepath, destination);
         }
      }
      internal static void CleanupBackup(string backupDirectory)
      {
         if (Directory.Exists(backupDirectory) && Directory.GetFiles(backupDirectory).Length < 2)
         {
            Directory.Delete(backupDirectory, true);
         }
      }
      #endregion utility
      #region event handlers
      private void PrefsCommand_Executed(object? sender, EventArgs e)
      {
         PrefsDialog f = new PrefsDialog(MainForm.prefs);
         f.ShowModal(MainForm);
         MainForm.DataPanel.RefreshTree(); // todo: this should only trigger if the user made a change that requires it
      }
      private void QuitCommand_Executed(object? sender, EventArgs e)
      {
         //if ((EtoEnvironment.Platform.IsMac && ReadyForQuit()) || !EtoEnvironment.Platform.IsMac)
         //{
         Application.Instance.Quit();
         //}
      }
      private void SaveFileAsCommand_Executed(object? sender, EventArgs? e)
      {
         if (!ReadyForSave()) { return; }
         string barefilename = Path.GetFileNameWithoutExtension(MainForm.saveFilePath);
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
            Directory = MainForm.savesFolder,
            FileName = proposedfilename,
         };
         saveDialog.Filters.Add(MainForm.FileFormat);
         MainForm.LoadingBar.Visible = true;
         if (saveDialog.ShowDialog(MainForm) == DialogResult.Ok)
         {
            Debug.WriteLine($"{saveDialog.FileName}");
            DynamicLayout bar = (DynamicLayout)MainForm.Content;
            FileWriter writer = new FileWriter();
            bool success = writer.WriteFile(MainForm.saveFile.Root, saveDialog.FileName);
            MainForm.LoadingBar.Visible = false;

            MainForm.DataPanel.ResetDataState();
            AddToBackup(MainForm.backupDirectory, saveDialog.FileName);

            LoadFile(saveDialog.FileName, true);
         }
         else
         {
            MainForm.LoadingBar.Visible = false;
         }
      }
      private void OpenFileCommand_Executed(object? sender, EventArgs e)
      {
         if (!ReadyForQuit())
         {
            return;
         }
         OpenFileDialog openDialog = new()
         {
            Directory = MainForm.savesFolder
         };
         openDialog.Filters.Add(MainForm.FileFormat);
         MainForm.LoadingBar.Visible = true;
         if (openDialog.ShowDialog(MainForm) == DialogResult.Ok)
         {
            LoadFile(openDialog.FileName);
         }
         else
         {
            MainForm.LoadingBar.Visible = false;
         }
      }
      private void BrowseSavesCommand_Executed(object? sender, EventArgs e)
      {
         string savesDirectory = MainForm.savesFolder.OriginalString;
         if (Directory.Exists(savesDirectory))
         {
            Process p = new Process();
            p.StartInfo.UseShellExecute = true;
            p.StartInfo.FileName = savesDirectory;
            p.Start();
         }
      }
      private void BrowseBackupsCommand_Executed(object? sender, EventArgs e)
      {
         string appSupportDirectory = Prefs.GetAppSupportDirectory();
         if (Directory.Exists(appSupportDirectory))
         {
            Process p = new Process();
            p.StartInfo.UseShellExecute = true;
            p.StartInfo.FileName = appSupportDirectory;
            p.Start();
         }
      }
      #endregion event handlers
      #region tool event handlers
      internal void FixAssertionFailed_Executed(object? sender, EventArgs e)
      {
         if (sender is Command c and not null && MainForm.DataPanel.AssertionFailureConditionExists(true))
         {
            _ = MessageBox.Show("Mission reassigned successfully", "Complete", MessageBoxButtons.OK, MessageBoxType.Information, MessageBoxDefaultButton.OK);
            c.Enabled = false;
            MainForm.DataPanel.AddUnsavedToDataState();
         }
      }
      internal void CleanDerelicts_Executed(object? sender, EventArgs e)
      {
         //RadioInputDialog r = new RadioInputDialog("Clean derelicts", new string[] { "sector-wide", "current system(s)",  /*"specific system"*/ });
         //r.ShowModal(this);
         //DialogResult d = r.GetDialogResult();
         //if (d == DialogResult.Ok)
         //{
         //Debug.WriteLine(r.GetSelectedIndex());
         MainForm.DataPanel.CleanDerelicts(DataPanel.DerelictsCleaningMode.SectorWide);
         _ = MessageBox.Show("Derelict ships removed", "Complete", MessageBoxButtons.OK, MessageBoxType.Information, MessageBoxDefaultButton.OK);
         EnableTools(MainForm.Menu, MainForm.DataPanel);
         MainForm.DataPanel.AddUnsavedToDataState();
         //}
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
         if (sender is Command c and not null && MainForm.DataPanel.ResetCamera())
         {
            _ = MessageBox.Show("Camera reset to system center, viewsize 100", "Complete", MessageBoxButtons.OK, MessageBoxType.Information, MessageBoxDefaultButton.OK);
            c.Enabled = false;
            MainForm.DataPanel.AddUnsavedToDataState();
         }
      }
      internal void ResetComet_Executed(object? sender, EventArgs e)
      {
         //RadioInputDialog r = new RadioInputDialog("Reset camera to...", new string[] { "system center", "nearest friendly ship" });
         //r.ShowModal(this);
         //DialogResult d = r.GetDialogResult();
         //if (d == DialogResult.Ok)
         //{
         //   Debug.WriteLine(r.GetSelectedIndex());

         //}
         // todo: prompt for clarification if more than 1 comet?

         if (sender is Command c and not null && MainForm.DataPanel.ResetComet())
         {
            _ = MessageBox.Show("Comet(s) reset to system center", "Complete", MessageBoxButtons.OK, MessageBoxType.Information, MessageBoxDefaultButton.OK);
            c.Enabled = false;
            MainForm.DataPanel.AddUnsavedToDataState();
         }
      }
      internal void TurnOffMeteors_Executed(object? sender, EventArgs e)
      {
         // todo: prompt for clarification if more than 1 system has meteors?
         if (sender is Command c and not null && MainForm.DataPanel.TurnOffMeteors())
         {
            _ = MessageBox.Show("Meteors turned off", "Complete", MessageBoxButtons.OK, MessageBoxType.Information, MessageBoxDefaultButton.OK);
            c.Enabled = false;
            MainForm.DataPanel.AddUnsavedToDataState();
         }
      }
      internal void CrossSectorMissionFix_Executed(object? sender, EventArgs e)
      {
         if (sender is Command c and not null && MainForm.DataPanel.SetCrossSectorMissionsDestination())
         {
            _ = MessageBox.Show("Cross-sector passenger missions updated", "Complete", MessageBoxButtons.OK, MessageBoxType.Information, MessageBoxDefaultButton.OK);
            c.Enabled = false;
            MainForm.DataPanel.AddUnsavedToDataState();
         }
      }
      internal void CleanupDeadCrew_Executed(object? sender, EventArgs e)
      {
         if (sender is Command c and not null && MainForm.DataPanel.ClearDeadCrew())
         {
            _ = MessageBox.Show("Dead crew cleaned up", "Complete", MessageBoxButtons.OK, MessageBoxType.Information, MessageBoxDefaultButton.OK);
            c.Enabled = false;
            MainForm.DataPanel.AddUnsavedToDataState();
         }
      }
      internal void RemoveHab_Executed(object? sender, EventArgs e)
      {

         if (sender is Command c and not null)
         {
            List<Node> friendlyShips = MainForm.DataPanel.GetFriendlyShips();
            List<string> shipnames = friendlyShips.Select(x => x.Name).ToList();
            CheckBoxListDialog dialog = new("Remove Habitation: select ships", shipnames);
            dialog.ShowModal(MainForm);
            if (dialog.GetDialogResult() == DialogResult.Ok)
            {
               IEnumerable<string> selectedItems = dialog.GetSelectedItems();
               foreach (string item in selectedItems)
               {
                  int start = item.IndexOf("(") + 1;
                  int length = item.IndexOf(",") - start;
                  string shipId = item.Substring(start, length); //"1829";
                  if (MainForm.DataPanel.RemoveHab(shipId))
                  {
                     _ = MessageBox.Show($"Removed Habitation from {shipId}", "Complete", MessageBoxButtons.OK, MessageBoxType.Information, MessageBoxDefaultButton.OK);

                     MainForm.DataPanel.AddUnsavedToDataState();
                  }
               }
            }
         }
      }
      #endregion tool event handlers
   }
}
