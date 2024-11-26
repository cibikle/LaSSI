using Eto.Drawing;
using Eto.Forms;
using System.Collections.Generic;
using System.Linq;

namespace LaSSI
{
   internal class LassiReport : Form
   {
      private readonly Dictionary<object, List<object>>? data;
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
         RectangleF screenBounds = Screen.PrimaryScreen.Bounds;
         float screenWidth = screenBounds.Width / 2;
         float screenHeight = screenBounds.Height / 2;
         Point screenCenter = new((int)(screenWidth), (int)(screenHeight));

         return screenCenter;
      }
      private static Point AdjustForFormSize(Point screenCenter, Size formSize)
      {
         Point adjustedCenter = new(screenCenter.X - (formSize.Width / 2), screenCenter.Y - (formSize.Height / 2));
         return adjustedCenter;
      }
      private DynamicLayout CreateReportLayout()
      {
         DynamicLayout layout = new();
         TreeGridView treeGridView = new()
         {
            AllowDrop = canRearrangeData,

         };

         TreeGridItemCollection treeGridItems = new();
         foreach ((KeyValuePair<object, List<object>> dataPoint, TreeGridItemCollection children) in from dataPoint in data
                                                                                                     let children = new TreeGridItemCollection()
                                                                                                     select (dataPoint, children))
         {
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