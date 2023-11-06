using System;
using Eto.Drawing;
using Eto.Forms;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Diagnostics;
using System.Collections;
using System.IO;

namespace LaSSI
{
   public class ListBuilder : Panel //todo: this whole darn thing is too tailored to Inventory management!
   {
      #region class variables
      private ObservableCollection<InventoryGridItem> LeftList { get; set; }
      private ObservableCollection<InventoryGridItem> RightList { get; set; }
      private GridView LeftGridView { get; set; }
      private GridView RightGridView { get; set; }
      private List<string>? LeftHeaders { get; }
      private List<string>? RightHeaders { get; }
      private int WidthMultiplier { get; set; }
      private List<Button> Buttons { get; set; } = new List<Button>();
      private int MaxHeight { get; set; } = 0;
      private int WidthMultiplicand { get; } = 8;
      #endregion

      #region constructors
      public ListBuilder(ObservableCollection<InventoryGridItem> leftList, ObservableCollection<InventoryGridItem> rightList)
      {
         LeftList = leftList;
         RightList = rightList;
         WidthMultiplier = GetWidthMultiplier();
         LeftGridView = CreateLeftGrid();
         RightGridView = CreateRightGrid();

         Content = CreateMainLayout();
      }
      public ListBuilder(ObservableCollection<InventoryGridItem> leftList, List<string> leftHeaders
         , ObservableCollection<InventoryGridItem> rightList, List<string> rightHeaders)
      {
         LeftHeaders = leftHeaders;
         RightHeaders = rightHeaders;
         LeftList = leftList;
         RightList = rightList;
         WidthMultiplier = GetWidthMultiplier();
         LeftGridView = CreateLeftGrid();
         RightGridView = CreateRightGrid();

         Content = CreateMainLayout();
      }
      #endregion

      #region initializers
      private DynamicLayout CreateMainLayout()
      {
         ID = "ListBuilderMainPanel";
         CreateListBuilderButtons();
         DynamicLayout dynamicLayout = new DynamicLayout
         {
            Spacing = new Size(20, 0)
         };
         Scrollable leftScrollable = new Scrollable
         {
            Tag = "LeftListBuilderScroller",
            ID = "LeftListBuilderScroller",
            Content = LeftGridView,
         };
         Scrollable rightScrollable = new Scrollable
         {
            Tag = "RightListBuilderScroller",
            ID = "RightListBuilderScroller",
            Content = RightGridView,
         };
         dynamicLayout.BeginHorizontal();
         dynamicLayout.Add(leftScrollable);
         dynamicLayout.Add(CreateButtonsLayout());
         dynamicLayout.Add(rightScrollable);
         dynamicLayout.AddSpace();
         dynamicLayout.EndHorizontal();
         return dynamicLayout;
      }
      private GridView CreateLeftGrid()
      {
         int widthMultiplier = WidthMultiplier;
         if (LeftHeaders != null && LeftHeaders.Count > 0)
         {
            int maxheaderlength = LeftHeaders.OrderByDescending(x => x.Length).First().Length;
            if(maxheaderlength > widthMultiplier)
            {
               widthMultiplier = maxheaderlength;
            }
         }
         GridView LeftGridView = new GridView
         {
            GridLines = GridLines.Both,
            DataStore = LeftList,
            AllowMultipleSelection = true,
            Tag = "LeftGridView",
            Columns =
            {
               new GridColumn
               {
                  Width = WidthMultiplicand * widthMultiplier,
                  DataCell = new TextBoxCell
                  {
                     Binding = Binding.Property((InventoryGridItem i) => i.Name)
                  },
                  Sortable = true,
                  
               }
            },
         };
         LeftGridView.Shown += LeftGridView_Shown;
         LeftList.CollectionChanged += LeftList_CollectionChanged;
         if (LeftHeaders != null && LeftHeaders.Count > 0)
         {
            LeftGridView.ShowHeader = true;
            foreach(GridColumn column in LeftGridView.Columns)
            {
               column.HeaderText = LeftHeaders[column.DisplayIndex];
            }
         }
         else
         {
            LeftGridView.ShowHeader = false;
         }
         return LeftGridView;
      }
      private GridView CreateRightGrid()
      {
         int nameWidthMultiplier = WidthMultiplier;
         int countWidthMultiplier = 0;
         if (RightHeaders != null && RightHeaders.Count > 0)
         {
            int maxheaderlength = RightHeaders.OrderByDescending(x => x.Length).First().Length;
            if (maxheaderlength > nameWidthMultiplier)
            {
               nameWidthMultiplier = maxheaderlength;
            }
            countWidthMultiplier = maxheaderlength;
         }

         GridView RightGridView = new GridView
         {
            GridLines = GridLines.Both,
            DataStore = RightList,
            AllowMultipleSelection = true,
            Tag = "RightGridView",
            //Height = MaxHeight,
            Columns =
            {
               new GridColumn
               {
                  Width = WidthMultiplicand * nameWidthMultiplier,
                  DataCell = new TextBoxCell
                  {
                     Binding = Binding.Property((InventoryGridItem i) => i.Name)
                  },
               },
               new GridColumn
               {
                  Width = WidthMultiplicand * countWidthMultiplier,
                  Editable = true,
                  DataCell = new TextBoxCell
                  {
                     Binding = Binding.Property((InventoryGridItem i) => i.Count).Convert(v => v.ToString(), s => {return int.TryParse(s, out int i) ? i : -1; })
                  },
               }
            }
         };
         RightGridView.Shown += RightGridView_Shown;
         RightList.CollectionChanged += RightList_CollectionChanged;
         if (RightHeaders != null)
         {
            for (int i = 0; i < RightHeaders.Count; i++)
            {
               RightGridView.Columns[i].HeaderText = RightHeaders[i];
            }
         }
         //RightListBox.ColumnWidthChanged += RightListBox_ColumnWidthChanged;
         RightGridView.CellEdited += RightListBox_CellEdited;
         return RightGridView;
      }
      private StackLayout CreateButtonsLayout()
      {
         StackLayout layout = new StackLayout();
         layout.Orientation = Orientation.Vertical;
         layout.Tag = "ButtonsLayout";
         layout.Padding = new Padding(0, 50, 0, 0);
         foreach (Button button in Buttons)
         {
            layout.Items.Add(button);
         }
         return layout;
      }
      private void CreateListBuilderButtons()
      {
         Button MoveLeft = new Button
         {
            Text = "<",
            Width = 25
         };
         MoveLeft.Click += MoveLeft_Click;
         Button MoveLeftAll = new Button
         {
            Text = "<<",
            Width = 25
         };
         MoveLeftAll.Click += MoveLeftAll_Click;
         Button MoveRight = new Button
         {
            Text = ">",
            Width = 25
         };
         ;
         MoveRight.Click += MoveRight_Click;
         Button MoveRightAll = new Button
         {
            Text = ">>",
            Width = 25
         };
         MoveRightAll.Click += MoveRightAll_Click;
         //List<Button> buttons = new List<Button>();
         Buttons.Add(MoveRightAll);
         Buttons.Add(MoveRight);
         Buttons.Add(MoveLeft);
         Buttons.Add(MoveLeftAll);
      }
      #endregion
      #region utility
      private static int GetAlphabeticalPosition(ObservableCollection<InventoryGridItem> collection, string value)
      {
         int index = 0;
         while (index < collection.Count && String.Compare(collection[index].ToString(), value, StringComparison.Ordinal) < 0)
         {
            index++;
         }
         return index;
      }
      private static int GetAlphabeticalPosition(ObservableCollection<string> collection, string value)
      {
         int index = 0;
         while (index < collection.Count && String.Compare(collection[index].ToString(), value, StringComparison.Ordinal) < 0)
         {
            index++;
         }
         return index;
      }
      public static ObservableCollection<InventoryGridItem> CreateInventoryGridItemListFromStringList(List<string> itemNames)
      {
         ObservableCollection<InventoryGridItem> invItems = new();
         foreach (string name in itemNames)
         {
            invItems.Add(new InventoryGridItem(name, 0));
         }
         return invItems;
      }
      private int GetTheHeigthUnderControl(Control control) // I don't love this, but it _frelling_ works
      {
         if (control.Height < control.ParentWindow.Height)
         {
            int offset = 0;
            if (control is Scrollable s)
            {
               var iter = s.Children.Where(x => x.Parent == this.Parent && x != this && x.Height <= s.Height).GetEnumerator();
               while (iter.MoveNext())
               {
                  offset += iter.Current.Height;
               }
            }
            return control.Height - offset;
         }
         if (control.Parent != null)
         {
            control.Height = GetTheHeigthUnderControl(control.Parent);
         }
         return control.Height;
      }
      public int GetWidthMultiplier()
      {
         int leftMaxLen = LongestEntryLength(LeftList);
         int rightMaxLen = LongestEntryLength(RightList);

         return leftMaxLen > rightMaxLen ? leftMaxLen : rightMaxLen;
      }
      private int LongestEntryLength(ObservableCollection<InventoryGridItem> items)
      {
         if(items.Count > 0)
         {
            return items.OrderByDescending((InventoryGridItem x) => x.Name.Length).First().Name.Length;
         }
         return 0;
      }
      private void UpdateColumnWidthMultiplicand(IEnumerator iter)
      {
         int longest = 0;
         while (iter.MoveNext())
         {
            int stringLenth = iter.Current.ToString().Length;
            if (stringLenth > longest) longest = stringLenth;
         }
         if (longest > WidthMultiplier) WidthMultiplier = longest;
      }
      #endregion

      #region event handlers
      private void MoveRightAll_Click(object? sender, EventArgs e)
      {
         foreach (InventoryGridItem item in LeftList)
         {
            string name = item.Name;
            if (name != string.Empty
               && !InventoryGridItem.ListContains(RightList, name))
            {
               item.Count = 1;
               RightList.Insert(GetAlphabeticalPosition(RightList, name), item);
            }
         }
         LeftList.Clear();
      }
      private void MoveLeftAll_Click(object? sender, EventArgs e)
      {
         foreach (InventoryGridItem item in RightList)
         {
            string name = item.Name;
            if (name != string.Empty
               && !InventoryGridItem.ListContains(LeftList, name))
            {
               item.Count = 0;
               LeftList.Insert(GetAlphabeticalPosition(LeftList, name), item);
            }
         }
         RightList.Clear();
      }
      private void MoveRight_Click(object? sender, EventArgs e)
      {
         var leftSelectedItems = LeftGridView.SelectedItems.ToArray();
         foreach (InventoryGridItem item in leftSelectedItems)
         {
            string name = item.Name;
            if (name != string.Empty
               && !InventoryGridItem.ListContains(RightList, name!))
            {
               item.Count = 1;
               RightList.Insert(GetAlphabeticalPosition(RightList, name), item);
               LeftList.Remove(item);
            }
         }
      }
      private void MoveLeft_Click(object? sender, EventArgs e)
      {
         var rightSelectedItems = RightGridView.SelectedItems.ToArray();
         foreach (InventoryGridItem item in rightSelectedItems)
         {
            string name = item.Name;
            if (name != string.Empty
               && !InventoryGridItem.ListContains(LeftList, name))
            {
               item.Count = 0;
               LeftList.Insert(GetAlphabeticalPosition(LeftList, item.Name), item);
               RightList.Remove(item);
            }
         }
      }

      private void LeftGridView_Shown(object? sender, EventArgs e)
      {
         if (MaxHeight == 0)
         {
            MaxHeight = GetTheHeigthUnderControl(this);
         }
         this.LeftGridView.Height = MaxHeight;
      }
      private void RightGridView_Shown(object? sender, EventArgs e)
      {
         if (MaxHeight == 0)
         {
            MaxHeight = GetTheHeigthUnderControl(this);
         }
         this.RightGridView.Height = MaxHeight;
      }
      private void LeftList_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
      {
         if (e != null && e.NewItems != null)
         {
            UpdateColumnWidthMultiplicand(e.NewItems.GetEnumerator());
         }
         int proposedWidth = WidthMultiplicand * WidthMultiplier;
         if (proposedWidth > LeftGridView.Columns[0].Width) LeftGridView.Columns[0].Width = proposedWidth;
      }
      private void RightList_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
      {
         if (e != null && e.NewItems != null)
         {
            UpdateColumnWidthMultiplicand(e.NewItems.GetEnumerator());
         }
         int proposedWidth = WidthMultiplicand * WidthMultiplier;
         if (proposedWidth > RightGridView.Columns[0].Width) RightGridView.Columns[0].Width = proposedWidth;
      }
      private static void RightListBox_CellEdited(object? sender, GridViewCellEventArgs e) //todo: finish this
      {
         Debug.WriteLine("hello!");
      }
      #endregion
      
   }
}