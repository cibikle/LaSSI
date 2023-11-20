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

      /**/internal static Command CreateCleanDerelictsCommand(EventHandler<EventArgs> CleanDerelicts_Executed)
      {
         var cleanDerelicts = new Command { MenuText = "Clean derelicts", Shortcut = Application.Instance.CommonModifier | Keys.D };
         cleanDerelicts.Executed += CleanDerelicts_Executed;
         cleanDerelicts.Enabled = false;
         return cleanDerelicts;
      }
      internal static Command CreateFixAssertionFailedCommand(EventHandler<EventArgs> FixAssertionFailed_Executed)
      {
         var cleanDerelicts = new Command { MenuText = "Fix assertion failed", /*Shortcut = Application.Instance.CommonModifier | Keys.D*/ };
         cleanDerelicts.Executed += FixAssertionFailed_Executed;
         cleanDerelicts.Enabled = false;
         return cleanDerelicts;
      }
      internal static Command CreateQuitCommand()
      {
         var quitCommand = new Command { MenuText = "Quit", Shortcut = Application.Instance.CommonModifier | Keys.Q };
         quitCommand.Executed += (sender, e) => Application.Instance.Quit();
         return quitCommand;
      }
      internal static Command CreateOpenFileCommand(EventHandler<EventArgs> OpenFileCommand_Executed)
      {
         var openFileCommand = new Command { MenuText = "Open", Shortcut = Application.Instance.CommonModifier | Keys.O };
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
            Tag = "SaveFileAsCommand"
         };
         saveFileAsCommand.Executed += SaveFileAsCommand_Executed;
         return saveFileAsCommand;
      }
      #endregion commands


      internal static void EnableSaveAs(MenuBar menu)
      {
         var SaveAsCommand = ((SubMenuItem)menu.Items.First(menuItem => menuItem.Text == "&File")).Items.Select(submenuItem
            => submenuItem.Command as Command).First(command => command != null && command.Tag == (object)"SaveFileAsCommand");
         if (SaveAsCommand is not null) SaveAsCommand.Enabled = true;
      }
      internal static void EnableTools(MenuBar menu)
      {
         var ToolsMenu = (SubMenuItem)menu.Items.First(menuItem => menuItem.Text == "&Tools");
         if (ToolsMenu is not null)
         {
            foreach (var o in ToolsMenu.Items)
            {
               o.Enabled = true;
            }

         }
      }
   }
}
