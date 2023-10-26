namespace LaSSI
{
   partial class Form1
   {
      /// <summary>
      ///  Required designer variable.
      /// </summary>
      private System.ComponentModel.IContainer components = null;

      /// <summary>
      ///  Clean up any resources being used.
      /// </summary>
      /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
      protected override void Dispose(bool disposing)
      {
         if (disposing && (components != null))
         {
            components.Dispose();
         }
         base.Dispose(disposing);
      }

      #region Windows Form Designer generated code

      /// <summary>
      ///  Required method for Designer support - do not modify
      ///  the contents of this method with the code editor.
      /// </summary>
      private void InitializeComponent()
      {
         splitContainer1 = new SplitContainer();
         treeView1 = new TreeView();
         openFileDialog1 = new OpenFileDialog();
         textBox1 = new TextBox();
         label1 = new Label();
         button1 = new Button();
         button2 = new Button();
         button3 = new Button();
         label2 = new Label();
         GameModeTextBox = new TextBox();
         NextIdTextBox = new TextBox();
         label3 = new Label();
         SaveVersionTextBox = new TextBox();
         label4 = new Label();
         flowLayoutPanel1 = new FlowLayoutPanel();
         ((System.ComponentModel.ISupportInitialize)splitContainer1).BeginInit();
         splitContainer1.Panel1.SuspendLayout();
         splitContainer1.SuspendLayout();
         flowLayoutPanel1.SuspendLayout();
         SuspendLayout();
         // 
         // splitContainer1
         // 
         splitContainer1.Dock = DockStyle.Bottom;
         splitContainer1.Location = new Point(3, 61);
         splitContainer1.Name = "splitContainer1";
         // 
         // splitContainer1.Panel1
         // 
         splitContainer1.Panel1.Controls.Add(treeView1);
         splitContainer1.Panel1.Paint += splitContainer1_Panel1_Paint;
         splitContainer1.Size = new Size(906, 455);
         splitContainer1.SplitterDistance = 325;
         splitContainer1.SplitterWidth = 10;
         splitContainer1.TabIndex = 0;
         // 
         // treeView1
         // 
         treeView1.Dock = DockStyle.Fill;
         treeView1.Location = new Point(0, 0);
         treeView1.Name = "treeView1";
         treeView1.Size = new Size(325, 455);
         treeView1.TabIndex = 0;
         // 
         // openFileDialog1
         // 
         openFileDialog1.Filter = "Last Starship save files|*.space";
         openFileDialog1.FileOk += openFileDialog1_FileOk;
         // 
         // textBox1
         // 
         textBox1.Location = new Point(78, 3);
         textBox1.Name = "textBox1";
         textBox1.ReadOnly = true;
         textBox1.Size = new Size(563, 23);
         textBox1.TabIndex = 1;
         // 
         // label1
         // 
         label1.AutoSize = true;
         label1.Location = new Point(3, 0);
         label1.Name = "label1";
         label1.Size = new Size(69, 15);
         label1.TabIndex = 2;
         label1.Text = "Current file:";
         // 
         // button1
         // 
         button1.Location = new Point(647, 3);
         button1.Name = "button1";
         button1.Size = new Size(75, 23);
         button1.TabIndex = 3;
         button1.Text = "Open file";
         button1.UseVisualStyleBackColor = true;
         button1.Click += button1_Click;
         // 
         // button2
         // 
         button2.Location = new Point(728, 3);
         button2.Name = "button2";
         button2.Size = new Size(75, 23);
         button2.TabIndex = 4;
         button2.Text = "Reload file";
         button2.UseVisualStyleBackColor = true;
         // 
         // button3
         // 
         flowLayoutPanel1.SetFlowBreak(button3, true);
         button3.Location = new Point(809, 3);
         button3.Name = "button3";
         button3.Size = new Size(75, 23);
         button3.TabIndex = 5;
         button3.Text = "Save file";
         button3.UseVisualStyleBackColor = true;
         // 
         // label2
         // 
         label2.AutoSize = true;
         label2.Location = new Point(3, 29);
         label2.Name = "label2";
         label2.Size = new Size(75, 15);
         label2.TabIndex = 6;
         label2.Text = "Game mode:";
         // 
         // GameModeTextBox
         // 
         GameModeTextBox.Location = new Point(84, 32);
         GameModeTextBox.Name = "GameModeTextBox";
         GameModeTextBox.Size = new Size(143, 23);
         GameModeTextBox.TabIndex = 7;
         GameModeTextBox.TextChanged += textBox2_TextChanged;
         // 
         // NextIdTextBox
         // 
         NextIdTextBox.Location = new Point(288, 32);
         NextIdTextBox.Name = "NextIdTextBox";
         NextIdTextBox.Size = new Size(74, 23);
         NextIdTextBox.TabIndex = 9;
         // 
         // label3
         // 
         label3.AutoSize = true;
         label3.Location = new Point(233, 29);
         label3.Name = "label3";
         label3.Size = new Size(49, 15);
         label3.TabIndex = 8;
         label3.Text = "Next ID:";
         // 
         // SaveVersionTextBox
         // 
         SaveVersionTextBox.Location = new Point(449, 32);
         SaveVersionTextBox.Name = "SaveVersionTextBox";
         SaveVersionTextBox.ReadOnly = true;
         SaveVersionTextBox.Size = new Size(30, 23);
         SaveVersionTextBox.TabIndex = 11;
         // 
         // label4
         // 
         label4.AutoSize = true;
         label4.Location = new Point(368, 29);
         label4.Name = "label4";
         label4.Size = new Size(75, 15);
         label4.TabIndex = 10;
         label4.Text = "Save version:";
         // 
         // flowLayoutPanel1
         // 
         flowLayoutPanel1.Controls.Add(label1);
         flowLayoutPanel1.Controls.Add(textBox1);
         flowLayoutPanel1.Controls.Add(button1);
         flowLayoutPanel1.Controls.Add(button2);
         flowLayoutPanel1.Controls.Add(button3);
         flowLayoutPanel1.Controls.Add(label2);
         flowLayoutPanel1.Controls.Add(GameModeTextBox);
         flowLayoutPanel1.Controls.Add(label3);
         flowLayoutPanel1.Controls.Add(NextIdTextBox);
         flowLayoutPanel1.Controls.Add(label4);
         flowLayoutPanel1.Controls.Add(SaveVersionTextBox);
         flowLayoutPanel1.Controls.Add(splitContainer1);
         flowLayoutPanel1.Dock = DockStyle.Fill;
         flowLayoutPanel1.Location = new Point(0, 0);
         flowLayoutPanel1.Name = "flowLayoutPanel1";
         flowLayoutPanel1.Size = new Size(910, 602);
         flowLayoutPanel1.TabIndex = 12;
         // 
         // Form1
         // 
         AutoScaleDimensions = new SizeF(7F, 15F);
         AutoScaleMode = AutoScaleMode.Font;
         AutoSize = true;
         ClientSize = new Size(910, 602);
         Controls.Add(flowLayoutPanel1);
         Name = "Form1";
         Text = "LaSSI (Last Starship Save Inspector)";
         splitContainer1.Panel1.ResumeLayout(false);
         ((System.ComponentModel.ISupportInitialize)splitContainer1).EndInit();
         splitContainer1.ResumeLayout(false);
         flowLayoutPanel1.ResumeLayout(false);
         flowLayoutPanel1.PerformLayout();
         ResumeLayout(false);
      }

      #endregion

      private SplitContainer splitContainer1;
      private OpenFileDialog openFileDialog1;
      private TreeView treeView1;
      private TextBox textBox1;
      private Label label1;
      private Button button1;
      private Button button2;
      private Button button3;
      private Label label2;
      private TextBox GameModeTextBox;
      private TextBox NextIdTextBox;
      private Label label3;
      private TextBox SaveVersionTextBox;
      private Label label4;
      private FlowLayoutPanel flowLayoutPanel1;
   }
}