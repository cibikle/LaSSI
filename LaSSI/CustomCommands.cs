﻿using Eto.Forms;
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

      public CustomCommands(MainForm mainForm)
      {
         MainForm = mainForm;
         FileCommands.Add(CustomCommands.CreateOpenFileCommand(OpenFileCommand_Executed));
         FileCommands.Add(CustomCommands.CreateSaveFileAsCommand(SaveFileAsCommand_Executed));
         QuitCommand = CustomCommands.CreateQuitCommand();
         /*var prefsCommand = new Command(PrefsCommand_Executed);*/
         ToolsList.Add(CustomCommands.CreateCleanDerelictsCommand(CleanDerelicts_Executed));
         ToolsList.Add(CustomCommands.CreateFixAssertionFailedCommand(FixAssertionFailed_Executed));
         ToolsList.Add(CustomCommands.CreateResetCameraCommand(ResetCamera_Executed));
         ToolsList.Add(CustomCommands.CreateResetCometCommand(ResetComet_Executed));
      }

      #region commands
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
      internal static Command CreateQuitCommand()
      {
         var quitCommand = new Command
         {
            MenuText = "Quit",
            Shortcut = Application.Instance.CommonModifier | Keys.Q,
            ID = "QuitCommand"
         };
         quitCommand.Executed += (sender, e) => Application.Instance.Quit();
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
            Shortcut = Application.Instance.CommonModifier | Keys.Shift | Keys.S, // todo: after Save is implemented, put the Shift back
            Enabled = false,
            ID = "SaveFileAsCommand"
         };
         saveFileAsCommand.Executed += SaveFileAsCommand_Executed;
         return saveFileAsCommand;
      }
      #endregion commands

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
      internal static bool CheckEnablablility(MenuItem tool, DataPanel data)
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
         }

         return enablability;
      }
      #endregion utility
      //internal static void SetToolEnabled(MenuItem tool, bool enabled)
      //{
      //   tool.Enabled = enabled;
      //}
      #region event handlers
      private void SaveFileAsCommand_Executed(object? sender, EventArgs e)
      {
         if (!MainForm.DataPanel.ReadyForSave()) return;
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
            TreeGridView x = (TreeGridView)bar.Children.Where<Control>(x => (string)x.Tag == "DataTreeView").First();
            TreeGridItem y = (TreeGridItem)(x.DataStore as TreeGridItemCollection)![0];
            FileWriter writer = new FileWriter();
            bool success = writer.WriteFile(y, saveDialog.FileName);
            MainForm.LoadingBar.Visible = false;
         }
         else
         {
            MainForm.LoadingBar.Visible = false;
         }
      }
      private void OpenFileCommand_Executed(object? sender, EventArgs e) //todo: make this not suck
      {
         OpenFileDialog fileDialog = new()
         {
            Directory = MainForm.savesFolder
         };
         fileDialog.Filters.Add(MainForm.FileFormat);
         MainForm.LoadingBar.Visible = true;
         if (fileDialog.ShowDialog(MainForm) == DialogResult.Ok)
         {
            //this.Cursor = Cursors.; they don't have a waiting cursor; todo: guess I'll add my own--later!

            MainForm.saveFilePath = fileDialog.FileName;
            MainForm.saveFile = new SaveFilev2(MainForm.saveFilePath);
            MainForm.saveFile.Load();
            MainForm.UpdateUiAfterLoad();
            CustomCommands.EnableSaveAs(MainForm.Menu);
            CustomCommands.EnableTools(MainForm.Menu, MainForm.DataPanel);
         }
         else
         {
            MainForm.LoadingBar.Visible = false;
         }
      }
      internal void FixAssertionFailed_Executed(object? sender, EventArgs e)
      {
         if (sender is Command c and not null && MainForm.DataPanel.AssertionFailureConditionExists(true))
         {
            _ = MessageBox.Show("Mission reassigned successfully", MessageBoxButtons.OK, MessageBoxType.Information, MessageBoxDefaultButton.OK);
            c.Enabled = true;
            //todo: invalidate/refresh details if selected
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
         _ = MessageBox.Show("Derelict ships removed", MessageBoxButtons.OK, MessageBoxType.Information, MessageBoxDefaultButton.OK);
         CustomCommands.EnableTools(MainForm.Menu, MainForm.DataPanel);
         //todo: invalidate/refresh details if selected
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
            _ = MessageBox.Show("Camera reset to system center, viewsize 100", MessageBoxButtons.OK, MessageBoxType.Information, MessageBoxDefaultButton.OK);
            c.Enabled = false;
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
            _ = MessageBox.Show("Comet(s) reset to system center", MessageBoxButtons.OK, MessageBoxType.Information, MessageBoxDefaultButton.OK);
            c.Enabled = false;
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
