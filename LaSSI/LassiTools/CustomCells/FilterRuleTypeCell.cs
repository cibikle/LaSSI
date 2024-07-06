using System;
using Eto.Forms;

namespace LaSSI.LassiTools.CustomCells
{
   public class FilterRuleTypeCell : CustomCell
   {
      protected override Control OnCreateCell(CellEventArgs args)
      {
         var control = new DropDown();
         FilterRuleType.LoadControl(control.Items);
         control.SelectedKeyBinding.BindDataContext((FilterConditionLine m) => m.RuleType);

         control.SelectedValueChanged += (sender, e) =>
         {
            if (sender is not null and EnumDropDown<FilterRuleTypeEnum> d)
            {
               d.Bindings[0].Update(BindingUpdateMode.Source);
            }
         };
         return control;
      }
   }
}

