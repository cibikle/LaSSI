using Eto.Forms;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaSSI
{
   internal class CustomCommands
   {
      #region commands

      /**/
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
            Shortcut = Application.Instance.CommonModifier | Keys.J,
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
            Shortcut = Application.Instance.CommonModifier | Keys.R,
            ID = "ResetCameraTool"
         };
         resetCamera.Executed += ResetCamera_Executed;
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
         }

         return enablability;
      }
   }
}
