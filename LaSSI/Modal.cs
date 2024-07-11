using System;
using Eto.Forms;
using System.Collections.Generic;

namespace LaSSI
{
   public class Modal : Dialog
   {
      public Modal(List<string> text, string? title, List<string>? links = null)
      {
         if (!string.IsNullOrEmpty(title))
         {
            this.Title = title;
         }
         var layout = new DynamicLayout();
         foreach (var s in text)
         {
            layout.AddCentered(new Label { Text = s }, xscale: true, yscale: true);
         }
         if (links is not null)
         {
            foreach (var l in links)
            {
               var linkButton = new LinkButton { Text = l, };
               linkButton.Click += delegate { Application.Instance.Open(linkButton.Text); };
               layout.Add(linkButton);
            }
         }

         AbortButton = CreateAbortButton();
         layout.BeginVertical();
         layout.AddRow(null, AbortButton);
         layout.EndVertical();

         Content = layout;
      }
      private Button CreateAbortButton()
      {
         var b = new Button { Text = "Close", Tag = "Abort button" };
         b.Click += delegate
         {
            Close();
         };
         return b;
      }
   }
}

