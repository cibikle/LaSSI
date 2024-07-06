using System;
using Eto.Forms;

namespace LaSSI.LassiTools.CustomCells
{
   public class FilterValueCell : CustomCell
   {
      protected override Control OnCreateCell(CellEventArgs args)
      {
         var control = new Button();
         //control.bin
         control.TextBinding.BindDataContext((FilterConditionLine m) => m.Value);
         control.Click += (sender, e) =>
         {
            if (sender is not null and Button b && b.ParentWindow is not null and ToolManager t)
            {

               //FilterValueInputDialog d = new("Set value", t.mainForm, (FilterConditionLine)b.Bindings[0]);
               //d.ShowModal();
               TextInputDialog textInput = new($"Set value");
               textInput.SetText(b.Text);
               textInput.ShowModal();
               if (textInput.GetDialogResult() == DialogResult.Ok)
               {
                  b.Text = textInput.GetInput();
                  b.Bindings[0].Update(BindingUpdateMode.Source);
               }
            }
         };
         return control;
      }
   }
}

