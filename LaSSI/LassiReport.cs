using Eto.Drawing;
using Eto.Forms;
using System.Collections.Generic;

namespace LaSSI
{
   internal class LassiReport : Form
   {
      private Dictionary<object, List<object>>? data;
      private bool canRearrangeData = false;
      public bool CanRearrangeData { get { return canRearrangeData; } set { canRearrangeData = value; } }
      public LassiReport() { }
      public LassiReport(string title)
      {
         Title = title;
         CommonSetup();
      }
      public LassiReport(string title, Dictionary<object, List<object>> data, bool CanRearrangeData = false)
      {
         Title = title;
         this.data = data;
         this.CanRearrangeData = CanRearrangeData;
         CommonSetup();
      }
      private void CommonSetup()
      {
         Content = CreateReportLayout();
         Location = AdjustForFormSize(GetScreenCenter(), new Size(400, 200));
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
            AllowDrop = canRearrangeData,

         };

         TreeGridItemCollection treeGridItems = new TreeGridItemCollection();
         foreach (var dataPoint in data)
         {
            var children = new TreeGridItemCollection();
            foreach (var value in dataPoint.Value)
            {
               string valueText = string.Empty;
               if (value is string s1)
               {
                  valueText = s1;
               }
               else if (value is Node n)
               {
                  valueText = n.Name;
               }
               children.Add(new TreeGridItem()
               {
                  Tag = valueText,
               });
            }
            string keyText = string.Empty;
            if (dataPoint.Key is string s)
            {
               keyText = s;
            }
            else if (dataPoint.Key is Node n)
            {
               keyText = n.Name;
            }
            var item = new TreeGridItem(children)
            {
               Tag = keyText,
               Expanded = true,
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
