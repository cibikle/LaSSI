using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using Eto.Drawing;
using Eto.Forms;
//using static LaSSI.DataPanel;

namespace LaSSI
{
   public class BaseDataPanel : Panel
   {
      public enum DataState
      {
         Unchanged,
         Unapplied,
         Unsaved,
         UnsavedAndUnapplied
      }

      protected MainForm? mainForm;
      protected DataState dataState = DataState.Unchanged;
      protected bool dirtyBit = false;
      protected readonly int ParentWidth = 0;
      protected Node? Root { get; set; }
      protected Dictionary<Node, DetailsLayout> DetailPanelsCache = new();
      private readonly Button Apply = new()
      {
         Text = "Apply",
         ID = "DetailsApplyButton",
         Enabled = false
      };
      private readonly Button Revert = new()
      {
         Text = "Revert",
         ID = "DetailsRevertButton",
         Enabled = false
      };
      public BaseDataPanel()
      {
      }
      public BaseDataPanel(MainForm mainform, int parentWidth)
      {
         mainForm = mainform;
         ParentWidth = parentWidth;
         Content = CreateContentLayout();
      }
      public BaseDataPanel(MainForm mainform, Node root, int parentWidth) : this(mainform, parentWidth)
      {
         Root = root;
      }
      private TableLayout CreateContentLayout() // any particular reason this is a TableLayout?
      {
         //Apply.Click += ApplyButton_Click;
         //Revert.Click += RevertButton_Click;

         Splitter splitter = CreateSplitter();
         splitter.Panel1 = CreateLeftPanel();
         splitter.Panel1.Width = ParentWidth / 2;
         splitter.Panel2 = CreateRightPanel();

         GroupBox box = new() // todo: I assume this is here because it's necessary, but I'd like to revisit it
         {
            Content = splitter
         };

         TableLayout dataLayout = new();
         dataLayout.Rows.Add(box);

         return dataLayout;
      }
      private static Splitter CreateSplitter()
      {
         return new()
         {
            Tag = "Splitter",
            ID = "Splitter",
            Orientation = Orientation.Horizontal,
            SplitterWidth = 10,
         };
      }
      private TableLayout CreateLeftPanel()
      {
         TableLayout leftPanelLayout = new()
         {
            ID = "LeftPanelLayout",
            Spacing = new Size(5, 5),
            Size = new Size(-1, -1),
         };

         leftPanelLayout.Rows.Add(CreateTreeGridSearch());

         TreeGridView treeView = new()
         {
            Tag = "DataTreeView",
            ID = "DataTreeView",
            ShowHeader = false,
            AllowEmptySelection = true,
            AllowMultipleSelection = true,
            ContextMenu = new ContextMenu(DeleteNode())
         };
         treeView.SelectedItemChanged += TreeView_SelectedItemChanged;
         treeView.SelectedItemsChanged += TreeView_SelectedItemsChanged;
         treeView.CellFormatting += TreeView_CellFormatting;

         leftPanelLayout.Rows.Add(treeView);

         return leftPanelLayout;
      }
      private DynamicLayout CreateTreeGridSearch()
      {
         DynamicLayout searchLayout = new()
         {
            ID = "SearchLayout",
            Spacing = new Size(5, 5),
            Padding = new Padding(2, 0),
         };
         TextBox search = new()
         {
            ID = "SearchTextbox",
            Enabled = true,
            PlaceholderText = "Search...",
         };
         search.TextChanged += Search_TextChanged;

         Button clearSearch = new()
         {
            Text = "Clear"
         };
         clearSearch.Click += ClearSearch_Click;

         searchLayout.BeginHorizontal();
         searchLayout.Add(search, true);
         searchLayout.Add(clearSearch, false);
         searchLayout.EndHorizontal();

         return searchLayout;
      }
      private DynamicLayout CreateRightPanel()
      {
         DynamicLayout rightPanelLayout = new()
         {
            ID = "RightPanelLayout",
            Spacing = new Size(0, 5),
         };
         DynamicLayout rightPanelDetailsLayout = new()
         {
            ID = "RightPanelDetailsLayout"
         };
         rightPanelLayout.AddSeparateRow(CreateApplyRevertButtonsLayout());
         rightPanelLayout.AddSeparateRow(rightPanelDetailsLayout);
         return rightPanelLayout;
      }
      private StackLayout CreateApplyRevertButtonsLayout()
      {
         StackLayout ApplyRevertLayout = new()
         {
            Orientation = Orientation.Horizontal,
            Spacing = 5
         };

         ApplyRevertLayout.Items.Add(Apply);
         ApplyRevertLayout.Items.Add(Revert);

         return ApplyRevertLayout;
      }

      internal TextBox GetSearchBox()
      {
         return FindChild<TextBox>("SearchTextbox");
         //TableLayout lefthandLayout = (TableLayout)GetTreeGridView().Parent;
         //return (TextBox)lefthandLayout.FindChild("SearchTextbox");
         //return GetTreeGridView().Parent.Children.Where(x => x.ID == "searchTextBox");
         //return null;
      }
      internal TreeGridView GetTreeGridView()
      {
         return FindChild<TreeGridView>("DataTreeView");
         //return (TreeGridView)this.Children.Where<Control>(x => x.ID == "DataTreeView").First();
         // pretty sure this blows up if the data tree isn't found
      }
      protected TreeGridItemCollection GetTreeGridItems()
      {
         return (TreeGridItemCollection)GetTreeGridView().DataStore;
      }
      protected void SetTreeGridItems(TreeGridItemCollection items)
      {
         GetTreeGridView().DataStore = items;
      }
      protected DynamicLayout GetRightPanelDetailsLayout()
      {
         return (DynamicLayout)FindChild("RightPanelDetailsLayout");
      }

      protected Node GetSelectedNode()
      {
         return (Node)GetTreeGridView().SelectedItem;
      }
      protected Control GetDetailsControl()
      {
         //DynamicLayout detailsLayout = (DynamicLayout)GetRightPanelDetailsLayout().Content;
         //Control detailsControl = detailsLayout.FindChild("DefaultGridView");
         //if (detailsControl is null)
         //{
         //   detailsControl = detailsLayout.FindChild("ListBuilder");
         //}
         return GetRightPanelDetailsLayout().Content;
      }
      protected void SetDetailsControl(Control? detailsControl)
      {
         GetRightPanelDetailsLayout().Content = detailsControl;
      }
      protected Node? GetRoot()
      {
         if (mainForm is not null)
         {
            return mainForm.saveFile.Root;
         }
         return null;
      }
      private Command DeleteNode()
      {
         var deleteNode = new Command { MenuText = "Delete node" };
         deleteNode.Executed += DeleteNode_Executed;
         return deleteNode;
      }
      protected void ClearDetails()
      {
         SetDetailsControl(null);
         UpdateApplyRevertButtons(DetailsLayout.State.Unmodified);
      }
      protected void ClearItemFromCache(Node item)
      {
         if (item is not null && DetailPanelsCache.ContainsKey(item))
         {
            DetailPanelsCache.Remove(item);
         }
      }
      protected virtual DetailsLayout CreateDetailsLayout(Node item)
      {
         return new DetailsLayout();
      }
      internal void UpdateDetailsPanel(Node item, bool clearPreexisting = false)
      {
         if (item is not null)
         {
            if (clearPreexisting)
            {
               ClearItemFromCache(item);
            }
            DynamicLayout detailslayout = GetRightPanelDetailsLayout();
            if (!DetailPanelsCache.ContainsKey(item))
            {
               DetailPanelsCache.Add(item, CreateDetailsLayout(item));
            }
            detailslayout.Content = DetailPanelsCache[item];
            UpdateApplyRevertButtons(DetailPanelsCache[item].Status);
         }
      }

      #region ApplyRevert and DataState
      internal void UpdateApplyRevertButtons(DetailsLayout.State status)
      {
         switch (status)
         {
            case DetailsLayout.State.Unmodified:
               {
                  Apply.Enabled = Revert.Enabled = false;
                  break;
               }
            case DetailsLayout.State.Modified:
               {
                  Apply.Enabled = Revert.Enabled = true;
                  break;
               }
            case DetailsLayout.State.Applied:
               {
                  Apply.Enabled = Revert.Enabled = false;//todo: if the user wants to undo an applied change, they need to reload the node
                                                         //todo: add a way to reload the node
                  break;
               }
         }
      }
      internal void ResetDataState()
      {
         dataState = DataState.Unchanged;
      }
      internal bool DataStateMatches(DataState state)
      {
         return dataState == state;
      }
      internal void AddUnappliedToDataState()
      {
         if (dataState == DataState.Unchanged)
         {
            dataState = DataState.Unapplied;
         }
         else if (dataState == DataState.Unsaved)
         {
            dataState = DataState.UnsavedAndUnapplied;
         }
      }
      internal void AddUnsavedToDataState()
      {
         if (dataState == DataState.Unchanged)
         {
            dataState = DataState.Unsaved;
         }
         else if (dataState == DataState.Unapplied)
         {
            dataState = DataState.UnsavedAndUnapplied;
         }
      }
      internal void SubtractUnappliedFromDataState()
      {
         if (dataState == DataState.Unapplied)
         {
            dataState = DataState.Unchanged;
         }
         else if (dataState == DataState.UnsavedAndUnapplied)
         {
            dataState = DataState.Unsaved;
         }
      }
      internal void SubtractUnsavedFromDataState()
      {
         if (dataState == DataState.Unsaved)
         {
            dataState = DataState.Unchanged;
         }
         else if (dataState == DataState.UnsavedAndUnapplied)
         {
            dataState = DataState.Unapplied;
         }
      }
      protected static void ApplyChange(Node item, Control detailControl)
      {
         OrderedDictionary itemDictionary = new OrderedDictionary();

         if (detailControl is not null)
         {
            if (detailControl is GridView gridView)
            {
               ObservableCollection<Oncler> gridCollection = (ObservableCollection<Oncler>)gridView.DataStore;
               foreach (var oncler in gridCollection)
               {
                  itemDictionary.Add(oncler.Key, oncler.Value);
               }
            }
            else if (detailControl is ListBuilder lb)
            {
               ObservableCollection<InventoryGridItem> listItems = lb.GetRightList();
               foreach (var entry in listItems)
               {
                  itemDictionary.Add(entry.Name, entry.Count);
               }
            }
            item.Properties = itemDictionary;
         }
      }
      protected void DetailsApplied()
      {
         ((DetailsLayout)GetRightPanelDetailsLayout().Content).Status = DetailsLayout.State.Applied;
         UpdateApplyRevertButtons(DetailsLayout.State.Applied);
      }
      public bool ChangesAreUnapplied()
      {
         foreach (var v in DetailPanelsCache)
         {
            if (((DetailsLayout)v.Value).Status == DetailsLayout.State.Modified)
            {
               return true;
            }
         }
         return false;
      }
      internal void ApplyAllChanges()
      {
         foreach (var v in DetailPanelsCache)
         {
            if (((DetailsLayout)v.Value).Status == DetailsLayout.State.Modified)
            {
               ApplyChange(v.Key, v.Value.Children.First(x => x.ID == "DefaultGridView" || x.ID == "ListBuilder")); // todo: _really_ need to genericize this!
               v.Value.Status = DetailsLayout.State.Applied;
            }
         }
         UpdateApplyRevertButtons(DetailsLayout.State.Applied);
         dataState = DataState.Unsaved;
      }
      internal void RevertAllUnappliedChanges()
      {
         List<KeyValuePair<Node, DetailsLayout>> cachedPanels = DetailPanelsCache.ToList();
         foreach (var panel in cachedPanels)
         {
            if (((DetailsLayout)panel.Value).Status == DetailsLayout.State.Modified)
            {
               UpdateDetailsPanel(panel.Key, true);
            }
         }
         UpdateApplyRevertButtons(DetailsLayout.State.Unmodified);
         SubtractUnappliedFromDataState();
      }
      internal virtual void DetailsModified()
      {
         Apply.Enabled = Revert.Enabled = true;

         //((DetailsLayout)GetRightPanelDetailsLayout().Content).Status = DetailsLayout.State.Modified;
      }
      internal virtual void DetailsUnmodified()
      {
         Apply.Enabled = Revert.Enabled = false;

         //((DetailsLayout)GetRightPanelDetailsLayout().Content).Status = DetailsLayout.State.Unmodified;
      }
      #endregion

      // EVENT HANDLERS //
      #region event handlers
      private void DeleteNode_Executed(object? sender, EventArgs e)
      {
         List<Node> nodesToDelete = GetTreeGridView().SelectedItems.Cast<Node>().ToList();
         ClearDetails();
         foreach (Node node in nodesToDelete)
         {
            if (node.GetParent() is not null and Node parent)
            {
               parent.RemoveChild(node);
               ClearItemFromCache(node);
            }
         }
         AddUnsavedToDataState();
         RebuildTreeView(Root!);
      }
      private void ClearSearch_Click(object? sender, EventArgs e)
      {
         if (GetSearchBox() is not null and TextBox searchBox)
         {
            searchBox.Text = string.Empty;
         }
      }
      private void Search_TextChanged(object? sender, EventArgs e)
      {
         if (sender is not null and TextBox textBox)
         {
            string searchtext = textBox.Text;
            if (searchtext != string.Empty && mainForm is not null)
            {
               SetTreeGridItems(mainForm.saveFile.Search(searchtext.Trim()));
            }
            else
            {
               SetTreeGridItems(new TreeGridItemCollection() { GetRoot() });
            }
         }
      }
      private void TreeView_SelectedItemChanged(object? sender, EventArgs e)
      {
         if (sender is not null and TreeGridView view)
         {
            UpdateDetailsPanel((Node)view.SelectedItem);

            /* Node item = (Node)view.SelectedItem;
             //item.SetValue(2, Colors.Magenta);
             if (item is not null)
             {

             }*/
            //else
            //{
            //   ClearDetailsPanel();
            //}
         }
      }
      private void TreeView_SelectedItemsChanged(object? sender, EventArgs e)
      {
         if (sender is not null and TreeGridView view)
         {
            if (view.SelectedItems.Count() > 1)
            {
               ClearDetails();
            }
         }
      }
      private void RevertButton_Click(object? sender, EventArgs e)
      {
         Node item = GetSelectedNode();
         UpdateDetailsPanel(item, true);
      }
      private void ApplyButton_Click(object? sender, EventArgs e) // todo: generic way to get ahold of the current details-details panel (damn, I've really screwed up the nomenclature...)
      {
         Node item = GetSelectedNode();
         Control detailControl = GetDetailsControl();
         if (detailControl is not null)
         {
            dirtyBit = true;
            dataState = DataState.Unsaved;
            ApplyChange(item, detailControl);

            DetailsApplied();
         }
      }
      private void TreeView_CellFormatting(object? sender, GridCellFormatEventArgs e)
      {
         if (sender is not null and TreeGridView tree)
         {
            if (mainForm is not null)
            {
               if (mainForm.prefs.FindPref("Holiday fun") is not null and Pref pref && pref.value is not null and yesno holidayfun && holidayfun == yesno.yes)
               {
                  var today = DateTime.Today;

                  DateTime ChristmasDay = new(DateTime.Now.Year, 12, 25, 0, 0, 0);
                  DateTime NewYearDay = new(DateTime.Now.Year, 1, 1, 0, 0, 0);

                  //today = ChristmasDay.AddDays(8);
                  if ((today >= ChristmasDay.AddDays(-5)) && (today <= ChristmasDay.AddDays(7)))
                  {
                     if (e.Item is not null /*and Node item*/)
                     {
                        e.ForegroundColor = e.Row % 2 == 0 ? Colors.Green : Colors.Red;
                        //e.BackgroundColor = (Color)newColor;
                     }
                  }
                  else if (today >= NewYearDay && today < NewYearDay.AddDays(3))
                  {
                     e.ForegroundColor = Colors.SaddleBrown;
                  }
               }
            }
         }
      }
      public virtual void Rebuild(Node root)
      {
         DetailPanelsCache.Clear();
         ClearDetails();
         Root = root;
         RebuildTreeView(Root);
      }

      private void RebuildTreeView(Node root)
      {
         TreeGridView treeView = GetTreeGridView();
         TreeGridItemCollection collection = new()
         {
            root
         };
         collection[0].Expanded = true;
         treeView.DataStore = collection;

         if (treeView.Columns.Count == 0)
         {
            GridColumn column = new()
            {
               AutoSize = true,
               DataCell = new TextBoxCell("Name")
            };
            treeView.Columns.Add(column);
         }
      }
      #endregion
   }
}