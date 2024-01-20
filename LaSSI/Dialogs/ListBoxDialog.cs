using Eto.Forms;
using Eto.Drawing;
using System;
using System.Collections.Generic;

namespace LaSSI
{
   public class ListBoxDialog : Dialog
   {

      public ListBoxDialog()
      {

      }
      public ListBoxDialog(string[] options)
      {
         CommonSetup(options);
      }
      public ListBoxDialog(string[] options, string hint)
      {
         //ListBox.PlaceholderText = hint;
         CommonSetup(options);
      }
      //public string GetInput()
      //{
      //   return ListBox.Text;
      //}
      public DialogResult GetDialogResult()
      {
         return Result;
      }
      private void CommonSetup(string[] options)
      {
         //Title = title;
         foreach (var option in options)
         {
            ListBox.Items.Add(option);
         }

         DynamicLayout layout = new()
         {
            Padding = new Padding(5, 5)
         };
         Content = layout;
         layout.BeginCentered(new Padding(5, 5));
         layout.Add(ListBox);
         layout.EndCentered();
         layout.BeginCentered(new Padding(5, 5));
         layout.AddCentered(ButtonsLayout());
         layout.EndCentered();
         //ListBox.TextChanged += TextBox_TextChanged;
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
      private readonly ListBox ListBox = new();
      private DialogResult Result = DialogResult.None;
      private Button? OK;
   }
}

