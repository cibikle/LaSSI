using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using Eto.Forms;

namespace LaSSI.LassiTools
{
   public class LassiToolFilter
   {
      private MainForm mainForm;
      public ObservableCollection<FilterConditionLine> conditionCollection;
      FilterSelectionScope selectionScope;
      string selectList = string.Empty;
      public FilterSelectionScope SelectionScope
      {
         get { return selectionScope; }
         set
         {
            selectionScope = value;
         }
      }
      public string SelectList
      {
         get { return selectList; }
         set
         {
            selectList = value;
         }
      }
      public bool SelectNodes = true;
      public LassiToolFilter(MainForm mainForm)
      {
         this.mainForm = mainForm;
         conditionCollection = new();
      }
      public void Add()
      {
         //conditionCollection.Add(conditionCollection.Count + 1, new FilterConditionLine());
         FilterConditionLine newline = new();
         if (conditionCollection.Count > 0)
         {
            //newline.LogicalOperator = FilterLineLogicalOperator.AND;
            newline.RuleType = FilterRuleType.RuleTypes[FilterRuleTypeEnum.Must];
         }
         conditionCollection.Add(newline);
      }
      public bool Remove(FilterConditionLine line)
      {
         return conditionCollection.Remove(line);
      }
      private List<FilterConditionLine> GetLineWithGenerationModifier(string property, bool not = false)
      {
         if (not)
         {
            return conditionCollection.Where(x => !Regex.IsMatch(x.Key, property, RegexOptions.IgnoreCase)).ToList<FilterConditionLine>();
         }
         else
         {
            return conditionCollection.Where(x => Regex.IsMatch(x.Key, property, RegexOptions.IgnoreCase)).ToList<FilterConditionLine>();
         }
      }
      private List<string> GetLineSets(IEnumerable<FilterConditionLine> lines)
      {
         List<string> searchList = new();
         foreach (var line in lines)
         {
            string key = line.Key;
            if (key.StartsWith("Parent."))
            {
               key = key.Replace("Parent.", "", true, null);
            }
            else if (key.StartsWith("Child."))
            {
               key = key.Replace("Child.", "", true, null);
            }

            if (key == "Name")
            {
               searchList.Add($"{line.Value}");
            }
            else
            {
               searchList.Add($"{key}:{line.Value}");
            }
         }

         return searchList;
      }
      //child.type must = scientist
      //type must = friendlyship
      private TreeGridItemCollection ProcessParentLines(IEnumerable<FilterConditionLine> parentLines)
      {
         string search = string.Join(' ', GetLineSets(parentLines)).Trim();
         return mainForm.saveFile.Search(search);
      }
      private TreeGridItemCollection ProcessSelfLines(IEnumerable<FilterConditionLine> selfLines, FilterConditionResults? parentResults)
      {
         TreeGridItemCollection results = new();
         List<string> searchList = GetLineSets(selfLines);
         string search = string.Join(' ', searchList).Trim();
         if (parentResults is not null && parentResults.Results is not null && parentResults.Results.Count > 0)
         {
            foreach (Node result in parentResults.Results)
            {
               results.AddRange(mainForm.saveFile.FindNodes(result, searchList.ToArray()));
            }
         }
         else
         {
            results = mainForm.saveFile.Search(search);
         }
         return results;
      }
      private TreeGridItemCollection ProcessChildLines(IEnumerable<FilterConditionLine> childLines, FilterConditionResults? priorResults)
      {
         TreeGridItemCollection results = new();
         List<string> searchList = GetLineSets(childLines);
         string search = string.Join(' ', searchList).Trim();
         if (priorResults is not null && priorResults.Results is not null && priorResults.Results.Count > 0)
         {
            foreach (Node result in priorResults.Results)
            {
               results.AddRange(mainForm.saveFile.FindNodes(result, searchList.ToArray()));
            }
         }
         else
         {
            results = mainForm.saveFile.Search(search);
         }
         return results;
      }
      private FilterConditionResults GetMusts(IEnumerable<FilterConditionLine> lines)
      {
         List<FilterConditionResults> resultsLists = new();

         IEnumerable<FilterConditionLine> parentLines = lines.Where((FilterConditionLine m) => Regex.IsMatch(m.Key, "parent", RegexOptions.IgnoreCase));
         IEnumerable<FilterConditionLine> selfLines = lines.Where((FilterConditionLine m) => !Regex.IsMatch(m.Key, "parent|child", RegexOptions.IgnoreCase));
         IEnumerable<FilterConditionLine> childLines = lines.Where((FilterConditionLine m) => Regex.IsMatch(m.Key, "child", RegexOptions.IgnoreCase));

         if (parentLines.ToList<FilterConditionLine>().Count > 0)
         {
            resultsLists.Add(new FilterConditionResults(ProcessParentLines(parentLines), GenerationModifier.Parent));
         }
         if (selfLines.ToList<FilterConditionLine>().Count > 0)
         {
            resultsLists.Add(new FilterConditionResults(ProcessSelfLines(selfLines, resultsLists.Count > 0 ? resultsLists[0] : null), GenerationModifier.Self));
         }
         //if (childLines.ToList<FilterConditionLine>().Count > 0)
         //{

         //}

         return resultsLists.Count > 0 ? resultsLists[^1] : new FilterConditionResults(new TreeGridItemCollection(), GenerationModifier.Self);
      }
      private FilterConditionResults CollectMays(IEnumerable<FilterConditionLine> lines, FilterConditionResults priorResults)
      {
         FilterConditionResults filteredResults = new(new TreeGridItemCollection(), GenerationModifier.Any);
         foreach (Node result in priorResults.Results)
         {
            foreach (var line in lines)
            {
               if (priorResults.Generation == GenerationModifier.Parent)
               {
                  filteredResults.Results.AddRange(result.FindChildren(line.Key, line.Value, true));
               }
               else
               {
                  if ((result.TryGetProperty(line.Key, out string propVal) && propVal == line.Value)
            || (line.Key == "Name" && Regex.IsMatch(result.Name, line.Value, RegexOptions.IgnoreCase)))
                  {
                     filteredResults.Results.Add(result);
                  }
               }
            }


         }
         return filteredResults;
      }
      private void RemoveMustNots(IEnumerable<FilterConditionLine> lines, FilterConditionResults priorResults)
      {
         Node[] priorResultsCopy = new Node[priorResults.Results.Count];
         priorResults.Results.CopyTo(priorResultsCopy, 0);
         foreach (var line in lines)
         {
            foreach (Node result in priorResultsCopy)
            {
               if ((result.TryGetProperty(line.Key, out string propVal) && propVal == line.Value)
                  || (line.Key == "Name" && Regex.IsMatch(result.Name, line.Value, RegexOptions.IgnoreCase)))
               {
                  priorResults.Results.Remove(result);
               }
            }
         }
      }
      public void RunSearch()
      {
         FilterConditionResults results = GetMusts(SelectsMustsFromCollection());
         FilterConditionResults filteredResults = null;
         IEnumerable<FilterConditionLine> mays = SelectsMaysFromCollection();
         if (mays.ToList().Count > 0)
         {
            filteredResults = CollectMays(SelectsMaysFromCollection(), results);
         }


         //TreeGridItemCollection doubleFilteredResults;
         if (filteredResults is not null && filteredResults.Results.Count > 0)
         {
            RemoveMustNots(SelectsMustNotsFromCollection(), filteredResults);
         }
         else if (results is not null && results.Results.Count > 0)
         {
            RemoveMustNots(SelectsMustNotsFromCollection(), results);
         }

         if (filteredResults is not null && filteredResults.Results.Count > 0)
         {
            CheckBoxListDialog dialog = new("Results", filteredResults.Results);
            dialog.ShowModal();

         }
         else if (results is not null && results.Results.Count > 0)
         {
            CheckBoxListDialog dialog = new("Results", results.Results);
            dialog.ShowModal();
         }
      }
      private IEnumerable<FilterConditionLine> SelectsMustsFromCollection()
      {
         return conditionCollection.Where((FilterConditionLine m) => m.RuleType == FilterRuleType.RuleTypes[FilterRuleTypeEnum.Must]);
      }
      private IEnumerable<FilterConditionLine> SelectsMaysFromCollection()
      {
         return conditionCollection.Where((FilterConditionLine m) => m.RuleType == FilterRuleType.RuleTypes[FilterRuleTypeEnum.May]);
      }
      private IEnumerable<FilterConditionLine> SelectsMustNotsFromCollection()
      {
         return conditionCollection.Where((FilterConditionLine m) => m.RuleType == FilterRuleType.RuleTypes[FilterRuleTypeEnum.MustNot]);
      }
   }

   public class FilterConditionLine
   {
      //FilterLineLogicalOperator logicalOperator = FilterLineLogicalOperator.NULL;
      //string logicalOperator = FilterLogicalOperator.Operators[FilterLineLogicalOperator.NULL];
      string key = "...";
      //public FilterConditionValue value = new("...");
      string filterValue = "...";
      //FilterLineEvaluationOperator evaluationOperator = FilterLineEvaluationOperator.NULL;
      string evaluationOperator = FilterEvaulationOperator.Operators[FilterOperator.Equals];
      string ruleType = FilterRuleType.RuleTypes[FilterRuleTypeEnum.Must];
      //string conditionGroup = "-1";

      //public FilterLineLogicalOperator LogicalOperator
      //{
      //   get { return logicalOperator; }
      //   set { logicalOperator = value; }
      //}
      //public string LogicalOperator
      //{
      //   get { return logicalOperator; }
      //   set { logicalOperator = value; }
      //}
      public string RuleType
      {
         get { return ruleType; }
         set { ruleType = value; }
      }
      public string EvaluationOperator
      {
         get { return evaluationOperator; }
         set { evaluationOperator = value; }
      }
      public string Key
      {
         get { return key; }
         set { key = value; }
      }
      //public string Group
      //{
      //   get { return conditionGroup; }
      //   set { conditionGroup = value; }
      //}
      public string Value
      {
         get { return filterValue; }
         set { filterValue = value; }
      }
      public FilterConditionLine()
      {

      }
      public FilterConditionLine(string key, string value, FilterOperator EvaluationOperator = FilterOperator.Equals, FilterRuleTypeEnum RuleType = FilterRuleTypeEnum.Must)
      {
         this.key = key;
         this.filterValue = value;
         evaluationOperator = FilterEvaulationOperator.Operators[EvaluationOperator];
         ruleType = FilterRuleType.RuleTypes[RuleType];
         //logicalOperator = FilterLogicalOperator.Operators[LogicalOperator];
         //this.conditionGroup = conditionGroup;
      }
   }

   public class FilterConditionValue
   {
      private string value = string.Empty;
      private LassiToolFilter? subfilter;
      public LassiToolFilter? Subfilter
      {
         get { return subfilter; }
         set { Value = "{filter}"; subfilter = value; }
      }
      public string Value
      {
         get { return value; }
         set { this.value = value; }
      }
      public FilterConditionValue(string Value)
      {
         value = Value;
      }
      public FilterConditionValue(LassiToolFilter Value)
      {
         Subfilter = Value;
      }
   }
   public class FilterConditionResults
   {
      private TreeGridItemCollection? results;
      private GenerationModifier generation;

      public TreeGridItemCollection? Results { get { return results; } set { results = value; } }
      public GenerationModifier Generation { get { return generation; } set { generation = value; } }

      public FilterConditionResults(TreeGridItemCollection results, GenerationModifier generation)
      {
         this.results = results;
         this.generation = generation;
      }
   }
   public class FilterKeyCell : CustomCell
   {
      protected override Control OnCreateCell(CellEventArgs args)
      {
         var control = new Button();
         control.TextBinding.BindDataContext((FilterConditionLine m) => m.Key);
         control.Click += (sender, e) =>
         {
            if (sender is not null and Button b)
            {
               TextInputDialog textInput = new($"Set key");
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
   public class FilterValueCell : CustomCell
   {
      protected override Control OnCreateCell(CellEventArgs args)
      {
         var control = new Button();
         //control.bin
         control.TextBinding.BindDataContext((FilterConditionLine m) => m.Value);
         control.Click += (sender, e) =>
         {
            if (sender is not null and Button b)
            {
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
   //public class FilterGroupCell : CustomCell
   //{
   //   protected override Control OnCreateCell(CellEventArgs args)
   //   {
   //      var control = new Button();
   //      control.TextBinding.BindDataContext((FilterConditionLine m) => m.Group);
   //      //control.Bindings[0].Update(BindingUpdateMode.Destination);
   //      control.Click += (sender, e) =>
   //      {
   //         if (sender is not null and Button b)
   //         {
   //            TextInputDialog textInput = new($"Set group");
   //            textInput.SetText(b.Text);
   //            textInput.ShowModal();
   //            if (textInput.GetDialogResult() == DialogResult.Ok)
   //            {
   //               b.Text = textInput.GetInput();
   //               b.Bindings[0].Update(BindingUpdateMode.Source);
   //            }
   //         }
   //      };
   //      return control;
   //   }
   //}
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

   //public class FilterLogicalOperator
   //{
   //   public static Dictionary<FilterLineLogicalOperator, string> Operators = new()
   //   {
   //      { FilterLineLogicalOperator.AND, "AND" },
   //      { FilterLineLogicalOperator.OR, "OR" },
   //      { FilterLineLogicalOperator.NULL, "NULL" }
   //   };
   //   public static void LoadControl(ListItemCollection control)
   //   {
   //      foreach (var op in Operators.Values)
   //      {
   //         control.Add(op);
   //      }
   //   }
   //}



   public class FilterRuleType
   {
      public static Dictionary<FilterRuleTypeEnum, string> RuleTypes = new()
      {
         {FilterRuleTypeEnum.May,"May" },
         {FilterRuleTypeEnum.Must,"Must" },
         {FilterRuleTypeEnum.MustNot,"Must Not" }
      };
      public static void LoadControl(ListItemCollection control)
      {
         foreach (var op in RuleTypes.Values)
         {
            control.Add(op);
         }
      }
   }

   public class FilterEvaulationOperator
   {
      public static Dictionary<FilterOperator, string> Operators = new()
         {
            { FilterOperator.Equals, "Match" } // todo: implement more operators
            
            //{ FilterOperator.NotEquals, "NotEquals" },
            //{ FilterOperator.GreaterThan, "GreaterThan" },
            //{ FilterOperator.GreaterThanOrEquals, "GreaterThanOrEquals" },
            //{ FilterOperator.LessThan, "LessThan" },
            //{ FilterOperator.LessThanOrEquals, "LessThanOrEquals" },
            //{ FilterOperator.Contains, "Contains" },
            //{ FilterOperator.NotContains, "NotContains" },
            //{ FilterOperator.StartsWith, "StartsWith" },
            //{ FilterOperator.NotStartsWith, "NotStartsWith" },
            //{ FilterOperator.EndsWith, "EndsWith" },
            //{ FilterOperator.NotEndsWith, "NotEndsWith" },
            //{ FilterOperator.In, "In" },
            //{ FilterOperator.NotIn, "NotIn" }
         };
      public static void LoadControl(ListItemCollection control)
      {
         foreach (var op in Operators.Values)
         {
            control.Add(op);
         }
      }
   }
   public enum GenerationModifier
   {
      Self,
      Parent,
      Child,
      Any
   }
   public enum FilterOperator
   {
      Equals,
      NotEquals,
      GreaterThan,
      GreaterThanOrEquals,
      LessThan,
      LessThanOrEquals,
      Contains,
      NotContains,
      StartsWith,
      NotStartsWith,
      EndsWith,
      NotEndsWith,
      In,
      NotIn
   }

   public enum FilterRuleTypeEnum
   {
      May,
      Must,
      MustNot
   }

   public enum FilterSelectionScope
   {
      All,
      Some,
      One,
      Any,
      First
   }
}