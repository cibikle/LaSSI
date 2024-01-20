using System;
using Eto.Forms;
using LaSSI.LassiTools;

namespace LaSSI
{
   public class LassiTool
   {
      public string Name { get; set; } = string.Empty;
      internal LassiToolTrigger Trigger { get; }
      internal LassiToolFilter Filter { get; }
      //actions
      public LassiTool(string name, MainForm mainForm)
      {
         Name = name;
         Trigger = new(this);
         Filter = new(mainForm);
      }
      public void Executed(object? sender, EventArgs e)
      {
         MessageBox.Show($"Executed {Name}");
      }
   }
}

