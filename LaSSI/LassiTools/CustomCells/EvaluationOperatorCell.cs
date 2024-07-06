using System;
using Eto.Forms;

namespace LaSSI.LassiTools.CustomCells
{
   public class EvaluationOperatorCell : CustomCell
   {
      protected override Control OnCreateCell(CellEventArgs args)
      {
         var control = new DropDown();
         FilterEvaulationOperator.LoadControl(control.Items);
         control.SelectedKeyBinding.BindDataContext((FilterConditionLine m) => m.EvaluationOperator);

         control.SelectedValueChanged += (sender, e) =>
         {
            if (sender is not null and EnumDropDown<FilterOperator> d)
            {
               d.Bindings[0].Update(BindingUpdateMode.Source);
            }
         };
         return control;
      }
   }
}

