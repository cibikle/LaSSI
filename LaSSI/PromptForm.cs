using System;
using System.Collections.Generic;
using Eto.Drawing;
using Eto.Forms;

namespace LaSSI
{
   public class PromptForm : Form
   {
      string Statement = string.Empty;
      string Question = string.Empty;
      List<Button>? Buttons = null;
      public PromptForm()
      {

      }
      public PromptForm(string title)
      {
         Title = title;
      }
      public PromptForm(string title, string statement, string question)
      {
         Title = title;
         SetStatementQuestion(statement, question);
         Content = CreateMainLayout();
      }
      public PromptForm(string title, string statement, string question, List<Button> buttons)
      {
         Title = title;
         SetStatementQuestion(statement, question);
         Buttons = buttons;
         Content = CreateMainLayout();
      }
      private void SetStatementQuestion(string s, string q)
      {
         Statement = s;
         Question = q;
      }
      private DynamicLayout CreateMainLayout()
      {
         DynamicLayout mainLayout = new DynamicLayout()
         {

         };
         StackLayout lableStack = new StackLayout() { Orientation = Orientation.Vertical, Spacing = 5 };
         lableStack.Items.Add(new Label() { Text = Statement });
         lableStack.Items.Add(new Label() { Text = Question });
         mainLayout.BeginVertical(new Padding(5, 5), new Size(5, 5), false, false);
         mainLayout.Add(lableStack);
         mainLayout.EndVertical();
         mainLayout.AddSpace();
         if (Buttons is not null && Buttons.Count > 0)
         {
            StackLayout buttonsStack = new StackLayout() { Orientation = Orientation.Vertical };
            foreach (var button in Buttons)
            {
               buttonsStack.Items.Add(button);
            }

            mainLayout.AddCentered(buttonsStack);
         }

         return mainLayout;
      }
   }
}