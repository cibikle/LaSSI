using System;
using System.Collections.ObjectModel;
using Eto.Drawing;
using Eto.Forms;
using LaSSI.LassiTools.CustomCells;

namespace LaSSI.LassiTools
{
   public class LassiToolAction
   {
      // todo: add action lines collection
      public ObservableCollection<ActionLine> actionCollection;
      // todo: Add(), Remove() methods
      public void Add()
      {
         //conditionCollection.Add(conditionCollection.Count + 1, new FilterConditionLine());
         ActionLine newline = new();
         if (actionCollection.Count > 0)
         {
            //newline.LogicalOperator = FilterLineLogicalOperator.AND;
            //newline.RuleType = FilterRuleType.RuleTypes[FilterRuleTypeEnum.Must];
         }
         actionCollection.Add(newline);
      }
      public bool Remove(ActionLine line)
      {
         return actionCollection.Remove(line);
      }

      public LassiToolAction()
      {

      }
   }
   public class ActionLine
   {
      // todo: verb (delete, clear, set); noun (dictated by verb: delete=node, clear=property, set=property); value (only applies to set)
      string verb = "...";
      string @object = "...";
      string value = "...";

      public ActionLine()
      {

      }

      public ActionLine(string verb, string @object, string value)
      {
         this.verb = verb;
         this.@object = @object;
         this.value = value;
      }
   }
   internal class LassiToolActionLayout
   {
      public LassiToolActionLayout(LassiToolAction action)
      {
         DynamicLayout actionLayout = new()
         {
            Spacing = new Size(5, 0)
         };

         Label @do = new()
         {
            Text = "Do"
         };
         DynamicLayout clearTestButtons = ClearTestButtons(action);
         var AddAction = new Command { MenuText = "Add action" };
         var RemoveAction = new Command { MenuText = "Remove action" };
         GridView actionsGrid = ActionsGrid(action, new MenuItem[] { AddAction, RemoveAction });
         AddAction.Executed += (sender, e) => { action.Add(); };
         RemoveAction.Executed += (sender, e) => { if (actionsGrid.SelectedItem is not null and ActionLine line) { action.Remove(line); } };

         actionLayout.BeginHorizontal();
         //actionLayout.Add(SelectionLayout(filter));
         actionLayout.EndBeginHorizontal();
         actionLayout.Add(@do);
         actionLayout.EndBeginHorizontal();
         actionLayout.Add(actionsGrid, true, true);
         actionLayout.EndBeginHorizontal();
         actionLayout.Add(clearTestButtons);
         actionLayout.EndHorizontal();
         //return filterLayout;
      }

      private DynamicLayout ClearTestButtons(LassiToolAction action)
      {
         DynamicLayout buttonsLayout = new()
         {
            Spacing = new Size(5, 0)
         };

         return buttonsLayout;
      }

      internal static GridView ActionsGrid(LassiToolAction action, MenuItem[] commands)
      {
         GridView actionsGrid = new()
         {
            DataStore = action.actionCollection,
            AllowMultipleSelection = false,
            GridLines = GridLines.Both,
            ID = "ActionsGrid",
            ContextMenu = new ContextMenu(commands),
            Height = 120
         };
         //verb
         actionsGrid.Columns.Add(new GridColumn
         {
            HeaderText = "Verb",
            DataCell = //???
         });
         //noun
         actionsGrid.Columns.Add(new GridColumn
         {
            HeaderText = "Noun",
         });
         //value
         actionsGrid.Columns.Add(new GridColumn
         {
            HeaderText = "Value",
         });
         //key
         //actionsGrid.Columns.Add(new GridColumn
         //{
         //   HeaderText = "Key",
         //   DataCell = new FilterKeyCell(),
         //   Editable = true,
         //   AutoSize = true
         //});
         ////rule type
         //actionsGrid.Columns.Add(new GridColumn
         //{
         //   HeaderText = "Rule type",
         //   DataCell = new FilterRuleTypeCell(),
         //   Editable = true,
         //   //AutoSize = true
         //});
         ////eval op
         //actionsGrid.Columns.Add(new GridColumn
         //{
         //   HeaderText = "Evaluation operator",
         //   DataCell = new EvaluationOperatorCell(),
         //   Editable = true,
         //   AutoSize = true
         //});
         ////value
         //actionsGrid.Columns.Add(new GridColumn
         //{
         //   HeaderText = "Value",
         //   DataCell = new FilterValueCell(),
         //   Editable = true,
         //   AutoSize = true
         //});

         return actionsGrid;
      }
   }
}

