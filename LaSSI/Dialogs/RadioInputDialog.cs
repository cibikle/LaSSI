using Eto.Forms;
using Eto.Drawing;
using System;

namespace LaSSI
{
   public class RadioInputDialog : Dialog
   {

      public RadioInputDialog()
      {

      }
      //public RadioInputDialog(string title)
      //{
      //   CommonSetup(title);
      //}
      public RadioInputDialog(string title, string[] options)
      {
         //TextBox.PlaceholderText = hint;
         Options = options;
         CommonSetup(title);
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
            Padding = new Padding(5, 5),
            Spacing = new Size(0, 5)
         };
         Content = layout;

         buttonList.Orientation = Orientation.Vertical;
         buttonList.Spacing = new Size(0, 5);
         foreach (var opt in Options)
         {
            buttonList.Items.Add(opt);
         }
         layout.AddCentered(buttonList);
         buttonList.SelectedIndex = 0;
         layout.AddCentered(ButtonsLayout());
      }

      public int GetSelectedIndex()
      {
         return buttonList.SelectedIndex;
      }

      private StackLayout ButtonsLayout()
      {
         OK = new(OK_clicked)
         {
            Text = "OK",
         };
         //OK = ok;
         DefaultButton = OK;
         Button cancel = new() { Text = "Cancel" };
         cancel.Click += delegate
         {
            Result = DialogResult.Cancel;
            Close();
         };
         this.AbortButton = cancel;
         return new StackLayout(OK, cancel) { Orientation = Orientation.Horizontal, Spacing = 5 };
      }

      private void OK_clicked(object? sender, EventArgs e)
      {
         Result = DialogResult.Ok;
         Close();
      }

      //public string Hint { get; set; } = string.Empty;
      //private readonly TextBox TextBox = new();
      private DialogResult Result = DialogResult.None;
      private readonly string[] Options = Array.Empty<string>();
      private Button? OK;
      readonly RadioButtonList buttonList = new();
   }
}

