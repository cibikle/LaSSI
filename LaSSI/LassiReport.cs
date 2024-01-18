using Eto.Drawing;
using Eto.Forms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaSSI
{
   internal class LassiReport : Form
   {
      private Dictionary<string, List<string>>? data;
      public LassiReport() { }
      public LassiReport(string title)
      {
         Title = title;
      }
      public LassiReport(string title, Dictionary<string, List<string>> data) 
      {
         Title = title;
         this.data = data;
         Content = CreateReportLayout();
         Size = new Size(400, 200);
         Location = AdjustForFormSize(GetScreenCenter(), Size);
      }
      private static Point GetScreenCenter()
      {
         var screenBounds = Screen.PrimaryScreen.Bounds;
         var screenWidth = screenBounds.Width / 2;
         var screenHeight = screenBounds.Height / 2;
         var screenCenter = new Point((int)(screenWidth), (int)(screenHeight));

         return screenCenter;
      }
      private static Point AdjustForFormSize(Point screenCenter, Size formSize)
      {
         var adjustedCenter = new Point(screenCenter.X - (formSize.Width / 2), screenCenter.Y - (formSize.Height / 2));
         return adjustedCenter;
      }
      private DynamicLayout CreateReportLayout()
      {
         DynamicLayout layout = new();
         TreeGridView treeGridView = new()
         {


         };

         TreeGridItemCollection treeGridItems = new TreeGridItemCollection();
         foreach(var dataPoint in data)
         {
            
            var children = new TreeGridItemCollection();
            foreach (var value in dataPoint.Value)
            {
               children.Add(new TreeGridItem()
               {
                  Tag = value
               });
            }
            var item = new TreeGridItem(children)
            {
               Tag = dataPoint.Key,
               Expanded = true
            };
            treeGridItems.Add(item);
         }
         treeGridView.DataStore = treeGridItems;

         if (treeGridView.Columns.Count == 0)
         {
            GridColumn column = new()
            {
               AutoSize = true,
               DataCell = new TextBoxCell("Tag")
            };
            treeGridView.Columns.Add(column);
         }

         layout.BeginCentered(new Padding(5, 5));
         layout.Add(treeGridView);
         layout.EndCentered();
         return layout;
      }
   }
}
