using System;
using Eto.Drawing;
using Eto.Forms;
using LaSSI.LassiTools.CustomCells;

namespace LaSSI.LassiTools
{
   public class LassiToolFilterLayout
   {
      public LassiToolFilterLayout()
      {
      }
      internal static DynamicLayout CreateFilterLayout(LassiToolFilter filter)
      {
         DynamicLayout filterLayout = new()
         {
            Spacing = new Size(5, 0)
         };

         Label where = new()
         {
            Text = "Where"
         };
         DynamicLayout clearTestButtons = ClearTestButtons(filter);
         var AddCondition = new Command { MenuText = "Add condition" };
         var RemoveCondition = new Command { MenuText = "Remove condition" };
         GridView conditionsGrid = ConditionsGrid(filter, new MenuItem[] { AddCondition, RemoveCondition });
         AddCondition.Executed += (sender, e) => { filter.Add(); };
         RemoveCondition.Executed += (sender, e) => { if (conditionsGrid.SelectedItem is not null and FilterConditionLine line) { filter.Remove(line); } };

         filterLayout.BeginHorizontal();
         filterLayout.Add(SelectionLayout(filter));
         filterLayout.EndBeginHorizontal();
         filterLayout.Add(where);
         filterLayout.EndBeginHorizontal();
         filterLayout.Add(conditionsGrid, true, true);
         filterLayout.EndBeginHorizontal();
         filterLayout.Add(clearTestButtons);
         filterLayout.EndHorizontal();
         return filterLayout;
      }
      internal static DynamicLayout ClearTestButtons(LassiToolFilter filter)
      {
         DynamicLayout layout = new()
         {
            Spacing = new Size(5, 0),
            Padding = new Padding(0, 5, 0, 0)
         };

         Button clear = new()
         {
            ID = "ClearFilterButton",
            Text = "Clear filter",
            Command = new Command((sender, e) =>
            {
               if (MessageBox.Show("Are you sure you want to clear the filter?", "This cannot be undone", MessageBoxButtons.OKCancel, MessageBoxType.Question, MessageBoxDefaultButton.No) == DialogResult.Ok)
               {
                  filter.conditionCollection.Clear();
               }
            }),
            Enabled = filter.conditionCollection.Count > 0
         };
         Button test = new()
         {
            Text = "Test filter",
            ID = "TestFilterButton",
            Enabled = filter.conditionCollection.Count > 0
         };
         filter.conditionCollection.CollectionChanged += (sender, e) =>
         {
            clear.Enabled = filter.conditionCollection.Count > 0;
            test.Enabled = filter.conditionCollection.Count > 0;
         };

         layout.BeginHorizontal();
         layout.Add(clear);
         layout.Add(test);
         layout.AddSpace();
         layout.EndHorizontal();

         return layout;
      }
      internal static GridView ConditionsGrid(LassiToolFilter filter, MenuItem[] commands)
      {
         GridView conditionsGrid = new()
         {
            DataStore = filter.conditionCollection,
            AllowMultipleSelection = false,
            GridLines = GridLines.Both,
            ID = "ConditionsGrid",
            ContextMenu = new ContextMenu(commands),
            Height = 120
         };
         //key
         conditionsGrid.Columns.Add(new GridColumn
         {
            HeaderText = "Key",
            DataCell = new FilterKeyCell(),
            Editable = true,
            AutoSize = true
         });
         //rule type
         conditionsGrid.Columns.Add(new GridColumn
         {
            HeaderText = "Rule type",
            DataCell = new FilterRuleTypeCell(),
            Editable = true,
            //AutoSize = true
         });
         //eval op
         conditionsGrid.Columns.Add(new GridColumn
         {
            HeaderText = "Evaluation operator",
            DataCell = new EvaluationOperatorCell(),
            Editable = true,
            AutoSize = true
         });
         //value
         conditionsGrid.Columns.Add(new GridColumn
         {
            HeaderText = "Value",
            DataCell = new FilterValueCell(),
            Editable = true,
            AutoSize = true
         });

         return conditionsGrid;
      }

      internal static DynamicLayout SelectionLayout(LassiToolFilter filter)
      {
         DynamicLayout layout = new();
         var scopeDropDown = new EnumDropDown<FilterSelectionScope>()
         {
            ID = "FilterSelectionComboBox",
         };
         scopeDropDown.SelectedValueBinding.Bind(filter, f => f.SelectionScope);

         TextBox fieldList = new()
         {
            //Width = 30,
            ID = "FieldListTextBox",
            Enabled = false
         };
         fieldList.Bind(c => c.Text, filter, r => r.SelectList);

         RadioButtonList nodesOrFields = new()
         {
            Orientation = Orientation.Horizontal,
            Padding = new Padding(5, 0, 0, 0),
         };
         nodesOrFields.SelectedIndexChanged += (sender, e) => { filter.SelectNodes = !(fieldList.Enabled = nodesOrFields.SelectedIndex == 1); };
         nodesOrFields.Items.Add("Node(s)");
         nodesOrFields.Items.Add("Field(s)");
         nodesOrFields.SelectedIndex = 0;

         layout.BeginHorizontal();
         layout.Add(scopeDropDown);
         layout.Add(nodesOrFields);
         layout.Add(fieldList);
         layout.EndHorizontal();
         return layout;
      }
   }
}

