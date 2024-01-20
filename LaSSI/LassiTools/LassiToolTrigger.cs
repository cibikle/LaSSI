using System;
using Eto.Forms;

namespace LaSSI.LassiTools
{
   public class LassiToolTrigger
   {
      public bool OnLoad { get; set; }
      public bool Enabled { get; set; }
      public Command Trigger { get; }
      public LassiToolTrigger(LassiTool tool, bool onLoad = false, Keys shortcut = Keys.None)
      {
         OnLoad = onLoad;
         Trigger = new Command()
         {
            MenuText = tool.Name,
            Shortcut = shortcut
         };
         Trigger.Executed += tool.Executed;
      }
   }
}

