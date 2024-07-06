using System;
using Eto.Forms;

namespace LaSSI.LassiTools
{
   public class FilterConditionValue
   {
      private FilterValueMode mode;
      private string value = string.Empty;
      private string subfilterText = string.Empty;
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
      public override string ToString()
      {
         if (mode == FilterValueMode.subfilter)
         {
            return "{filter}";
         }
         else
         {
            return Value;
         }
      }
      public string GetFilterText()
      {
         // todo: write a ...deparser? on ToolFilter that turns the filter into a string block of sql-like text
         return string.Empty;
      }
      public void ParseFilterText(string filterText)
      {
         // todo: write a parser on ToolFilter that turns the filter text into a ToolFilter
      }
   }
   internal enum FilterValueMode
   {
      value,
      subfilter
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
}

