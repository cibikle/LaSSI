using Eto.Forms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace LaSSI
{
   public class DualListbox : Eto.Forms.CommonControl
   {
      List<string> LeftList { get; set; } = new List<string>();
      List<string> RightList { get; set; } = new List<string>();
      Eto.Forms.Panel MainPanel { get; set; } = new Eto.Forms.Panel();
      Eto.Forms.Button MoveLeft {  get; set; } = new Eto.Forms.Button();
      Eto.Forms.Button MoveRight { get; set; } = new Eto.Forms.Button();
      Eto.Forms.Button MoveLeftAll { get; set; } = new Eto.Forms.Button();
      Eto.Forms.Button MoveRightAll { get; set; } = new Eto.Forms.Button();
      Eto.Forms.ListBox LeftListBox { get; set; } = new Eto.Forms.ListBox();
      Eto.Forms.ListBox RightListBox { get; set; } = new Eto.Forms.ListBox();


      public DualListbox()
      {

      }
      public DualListbox(List<string> leftList, List<string> rightList)
      {
         this.LeftList = leftList;
         this.RightList = rightList;
         this.LeftListBox.DataStore = LeftList;
         this.RightListBox.DataStore = RightList;
         this.MainPanel.Content = GetMainLayout();
         this.MoveLeft.Click += MoveLeft_Click;
         this.MoveLeftAll.Click += MoveLeftAll_Click;
         this.MoveRight.Click += MoveRight_Click;
         this.MoveRightAll.Click += MoveRightAll_Click;
      }
      private StackLayout GetButtonsLayout()
      {
         StackLayout layout = new StackLayout();
         layout.Orientation = Eto.Forms.Orientation.Vertical;
         layout.Items.Add(MoveLeftAll);
         layout.Items.Add(MoveLeft);
         layout.Items.Add(MoveRight);
         layout.Items.Add(MoveRightAll);
         return layout;
      }
      private StackLayout GetMainLayout()
      {
         StackLayout mainLayout = new StackLayout
         {
            Orientation = Eto.Forms.Orientation.Horizontal
         };
         mainLayout.Items.Add(LeftListBox);
         mainLayout.Items.Add(GetButtonsLayout());
         mainLayout.Items.Add(RightListBox);
         return mainLayout;
      }
      private void MoveLeft_Click(object? sender, EventArgs e)
      {

      }
      private void MoveLeftAll_Click(object? sender, EventArgs e)
      {

      }
      private void MoveRight_Click(object? sender, EventArgs e)
      {

      }
      private void MoveRightAll_Click(object? sender, EventArgs e)
      {

      }
   }
}
