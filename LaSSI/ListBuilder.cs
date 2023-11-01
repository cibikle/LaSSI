using System;
using Eto.Drawing;
using Eto.Forms;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Diagnostics;

namespace LaSSI
{
   public class ListBuilder : Panel //todo: this whole darn thing is too tailored to Inventory management!
   {
      private ObservableCollection<InventoryGridItem> LeftList { get; set; }
      private ObservableCollection<InventoryGridItem> RightList { get; set; }
      private GridView LeftGridView { get; set; }
      private GridView RightGridView { get; set; }
      private List<string>? LeftHeaders { get; }
      private List<string>? RightHeaders { get; }
      private int MaxEntryLength { get; }
      private List<Button> Buttons { get; set; } = new List<Button>();

      public ListBuilder(ObservableCollection<InventoryGridItem> leftList, ObservableCollection<InventoryGridItem> rightList)
      {
         LeftList = leftList;
         RightList = rightList;
         MaxEntryLength = GetListEntriesMaxLength();
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
         MaxEntryLength = GetListEntriesMaxLength();
         LeftGridView = CreateLeftGrid();
         RightGridView = CreateRightGrid();

         Content = CreateMainLayout();
      }
      public int GetListEntriesMaxLength()
      {
         int leftMaxLen = LeftList.OrderByDescending((InventoryGridItem x) => x.Name.Length).First().Name.Length;
         int rightMaxLen = RightList.OrderByDescending((InventoryGridItem x) => x.Name.Length).First().Name.Length;
         return leftMaxLen > rightMaxLen ? leftMaxLen : rightMaxLen;
      }
      
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
         int widthMultiplicand = MaxEntryLength;
         if (LeftHeaders != null && LeftHeaders.Count > 0)
         {
            int maxheaderlength = LeftHeaders.OrderByDescending(x => x.Length).First().Length;
            if(maxheaderlength > widthMultiplicand)
            {
               widthMultiplicand = maxheaderlength;
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
                  Width = widthMultiplicand * 7,
                  DataCell = new TextBoxCell
                  {
                     Binding = Binding.Property((InventoryGridItem i) => i.Name)
                  },
                  Sortable = true,
                  
               }
            },
         };
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
         int nameWidthMultiplicand = MaxEntryLength;
         int countWidthMultiplicand = 0;
         if (RightHeaders != null && RightHeaders.Count > 0)
         {
            int maxheaderlength = RightHeaders.OrderByDescending(x => x.Length).First().Length;
            if (maxheaderlength > nameWidthMultiplicand)
            {
               nameWidthMultiplicand = maxheaderlength;
            }
            countWidthMultiplicand = maxheaderlength;
         }

         GridView RightListBox = new GridView
         {
            GridLines = GridLines.Both,
            DataStore = RightList,
            AllowMultipleSelection = true,
            Tag = "RightGridView",
            Columns =
            {
               new GridColumn
               {
                  Width = (nameWidthMultiplicand + 1) * 7,
                  DataCell = new TextBoxCell
                  {
                     Binding = Binding.Property((InventoryGridItem i) => i.Name)
                  },
               },
               new GridColumn
               {
                  Width = countWidthMultiplicand * 7,
                  Editable = true,
                  DataCell = new TextBoxCell
                  {
                     Binding = Binding.Property((InventoryGridItem i) => i.Count).Convert(v => v.ToString(), s => {return int.TryParse(s, out int i) ? i : -1; })
                  },
               }
            }
         };
         if (RightHeaders != null)
         {
            for (int i = 0; i < RightHeaders.Count; i++)
            {
               RightListBox.Columns[i].HeaderText = RightHeaders[i];
            }
         }
         RightListBox.ColumnWidthChanged += RightListBox_ColumnWidthChanged;
         RightListBox.CellEdited += RightListBox_CellEdited;
         return RightListBox;
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
      private static void RightListBox_ColumnWidthChanged(object? sender, GridColumnEventArgs e) //todo: revise this
      {
         var foo = (GridView)sender;
         var bar = (ObservableCollection<InventoryGridItem>)foo.DataStore;
         int maxLength = bar.OrderByDescending(x => x.Name.Length).First().Name.Length;
         foo.Width = maxLength * 10;
      }
      private static void RightListBox_CellEdited(object? sender, GridViewCellEventArgs e) //todo: finish this
      {
         Debug.WriteLine("hello!");
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
      //private static Panel GetListBuilderMainPanel(Control sender)
      //{
      //   Panel? panel = null;
      //   try
      //   {
      //      panel = (Panel)((Control)sender!).Parents.Where<Widget>(x => (string)x.ID == "ListBuilderMainPanel").First();
      //   }
      //   catch (NullReferenceException)
      //   {
      //      Debug.WriteLine("Could not get ListBuilderMainPanel from the given sender");
      //   }
      //   return panel;
      //}
      private List<InventoryGridItem> GetSelectedItemsLeft()
      {
         var foo = LeftGridView.SelectedItems.ToArray();

         List<InventoryGridItem> leftList = new List<InventoryGridItem>();
         foreach (InventoryGridItem item in LeftGridView.SelectedItems)
         {
            leftList.Add(item);
         }
         return leftList;
      }
      private List<InventoryGridItem> GetSelectedItemsRight()
      {
         List<InventoryGridItem> rightList = new List<InventoryGridItem>();
         foreach (InventoryGridItem item in RightGridView.SelectedItems)
         {
            rightList.Add(item);
         }
         return rightList;
      }

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

      public static ObservableCollection<InventoryGridItem> CreateInventoryGridItemListFromStringList(List<string> itemNames)
      {
         ObservableCollection<InventoryGridItem> invItems = new();
         foreach (string name in itemNames)
         {
            invItems.Add(new InventoryGridItem(name, 0));
         }
         return invItems;
      }
   }
}

