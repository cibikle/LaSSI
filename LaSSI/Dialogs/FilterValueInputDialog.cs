using Eto.Forms;
using Eto.Drawing;
using System;

namespace LaSSI
{
   public class FilterValueInputDialog : Dialog
   {

      public FilterValueInputDialog()
      {

      }
      public FilterValueInputDialog(string title)
      {
         CommonSetup(title);
      }
      //public FilterValueInputDialog(string title, string hint)
      //{
      //   TextBox.PlaceholderText = hint;
      //   CommonSetup(title);
      //}
      public string GetInput()
      {
         return TextBox.Text;
      }
      public void SetText(string text)
      {
         TextBox.Text = text;
      }
      public DialogResult GetDialogResult()
      {
         return Result;
      }
      private void CommonSetup(string title)
      {
         Title = title;
         DynamicLayout layout = new()
         {
            Padding = new Padding(5, 5)
         };
         Content = layout;
         layout.BeginCentered(new Padding(5, 5));
         layout.Add(TextBox);
         layout.EndCentered();
         layout.BeginCentered(new Padding(5, 5));
         layout.AddCentered(ButtonsLayout());
         layout.EndCentered();
         TextBox.TextChanged += TextBox_TextChanged;
      }

      private void TextBox_TextChanged(object? sender, EventArgs e)
      {
         if (sender is not null and TextBox t && OK is not null)
         {
            OK.Enabled = t.Text.Trim().Length > 0;
         }
      }

      private StackLayout ButtonsLayout()
      {
         Button ok = new(OK_clicked)
         {
            Text = "OK",
            Enabled = false,
         };
         OK = ok;
         this.DefaultButton = ok; // this seems bad but I'm not fixing it now
         Button cancel = new() { Text = "Cancel" };
         cancel.Click += delegate
         {
            Result = DialogResult.Cancel;
            Close();
         };
         this.AbortButton = cancel;
         return new StackLayout(ok, cancel) { Orientation = Orientation.Horizontal, Spacing = 5 };
      }

      private void OK_clicked(object? sender, EventArgs e)
      {
         Result = DialogResult.Ok;
         Close();
      }

      public string Hint { get; set; } = string.Empty;
      private readonly TextBox TextBox = new();
      private MainForm mainForm;
      private DynamicLayout filterLayout = LassiTools.LassiToolFilterLayout.CreateFilterLayout(new LassiTools.LassiToolFilter(mainForm));
      private DialogResult Result = DialogResult.None;
      private Button? OK;
   }
}

