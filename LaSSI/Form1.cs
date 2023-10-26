namespace LaSSI
{
   public partial class Form1 : Form
   {
      string savesFolder;
      public Form1()
      {
         InitializeComponent();
         this.StartPosition = FormStartPosition.CenterScreen;
         savesFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Introversion\\LastStarship\\saves");
      }

      public void DisplaySaveData(SaveFile saveFile)
      {
         GameModeTextBox.Text = saveFile.GameMode;
         NextIdTextBox.Text = saveFile.NextId.ToString();
         SaveVersionTextBox.Text = saveFile.SaveVersion.ToString();
         treeView1.Nodes.Clear();
         treeView1.Nodes.Add(saveFile.root);
         treeView1.TopNode.Expand();
      }

      private void splitContainer1_Panel1_Paint(object sender, PaintEventArgs e)
      {

      }

      private void openFileDialog1_FileOk(object sender, System.ComponentModel.CancelEventArgs e)
      {
         textBox1.Text = openFileDialog1.FileName;
         Program.LoadFile(openFileDialog1.FileName);
      }

      private void button1_Click(object sender, EventArgs e)
      {
         openFileDialog1.InitialDirectory = savesFolder;
         openFileDialog1.ShowDialog();
      }

      private void textBox2_TextChanged(object sender, EventArgs e)
      {

      }

      private void button4_Click(object sender, EventArgs e)
      {

      }
   }
}