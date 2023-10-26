using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;

namespace LaSSI
{
   public class SaveFile
   {
      public float TimeIndex { get; set; }
      public uint NextId { get; set; }
      public float DeltaTime { get; set; }
      public float PlayTime { get; set; }
      public uint SaveVersion { get; set; }
      public string GameMode { get; set; }
      public Dictionary<string, string> HUD;
      public TreeNode hudTreeNode { get; set; }
      public TreeNode galaxyTreeNode { get; set; }
      public TreeNode root { get; set; }
      public SaveFile(string filename)
      {
         GameMode = string.Empty;
         HUD = new Dictionary<string, string>();
         hudTreeNode = new TreeNode();
         hudTreeNode.Text = "HUD";
         hudTreeNode.Name = "HudTreeNode";
         galaxyTreeNode = new TreeNode();
         galaxyTreeNode.Text = "Galaxy";
         galaxyTreeNode.Name = "GalaxyTreeNode";
         root = new TreeNode(filename);
         root.Nodes.Add(hudTreeNode);
         root.Nodes.Add(galaxyTreeNode);
      }
      private static void LoadHUD(SaveFile saveFile, string[] HudData)
      {
         for (int i = 2; i < HudData.Length - 1; i += 2)
         {
            saveFile.hudTreeNode.Nodes.Add($"{HudData[i]}={HudData[i + 1]}");
            //            saveFile.HUD.Add(HudData[i], HudData[i + 1]);
         }
      }
      private static void LoadOneLiner(TreeNode node, string[] Data)
      {
         for (int i = 0; i < Data.Length - 1; i += 2)
         {
            string addlData = String.Empty;
            string addlName = String.Empty;

            if (node.Parent.Text == "Hazards" && Data[i] == "Type")
            {
               if (Data[i + 1] == "1") addlData = " (asteroid field)";
               else if (Data[i + 1] == "2") addlData = " (gas cloud)";
               if (addlData != String.Empty)
               {
                  addlName = addlData;
               }
            }
            else if (node.Parent.Text == "Objects" && node.Parent.Parent.Text == "Galaxy" && Data[i] == "Name")
            {
               addlName = $" ({Data[i + 1]})";
            }
            if (addlName != String.Empty)
            {
               node.Text = node.Text + addlName;
            }

            node.Nodes.Add($"{Data[i]}={Data[i + 1]}{addlData}");
            //            saveFile.HUD.Add(HudData[i], HudData[i + 1]);
         }
      }
      public static void LoadFile(SaveFile saveFile, string filename)
      {
         if (File.Exists(filename))
         {
            string subNodeRegex = @"""\[i \d+\]""";

            Stack<TreeNode> nodeStack = new Stack<TreeNode>();
            //saveFile = new SaveFile(Path.GetFileName(filename));
            nodeStack.Push(saveFile.root);
            Debug.WriteLine(filename);
            TextReader reader = new StreamReader(filename);
            string text = reader.ReadToEnd();
            bool quit = false;
            string[] foo = new string[5];
            foreach (string line in text.Split(Environment.NewLine))
            {
               if (line.Length < 1) continue;
               if (quit)
               {
                  break;
               }
               string[] lineParts = line.Split(" ").ToList().Where(x => !string.IsNullOrEmpty(x)).ToArray<string>();
               switch (lineParts[0])
               {
                  case "TimeIndex":
                     {
                        saveFile.TimeIndex = float.Parse(lineParts[1]);
                        break;
                     }
                  case "NextId":
                     {
                        if (saveFile.NextId == 0)
                        {
                           saveFile.NextId = uint.Parse(lineParts[1]);
                        }
                        break;
                     }
                  case "DeltaTime":
                     {
                        saveFile.DeltaTime = float.Parse(lineParts[1]);
                        break;
                     }
                  case "PlayTime":
                     {
                        saveFile.PlayTime = float.Parse(lineParts[1]);
                        break;
                     }
                  case "SaveVersion":
                     {
                        saveFile.SaveVersion = uint.Parse(lineParts[1]);
                        break;
                     }
                  case "GameMode":
                     {
                        saveFile.GameMode = lineParts[1];
                        break;
                     }
                  case "BEGIN":
                     {
                        if (lineParts.Length > 2)
                        {
                           string subnodeId = $"{lineParts[1]} {lineParts[2]}";
                           Match m = Regex.Match(subnodeId, subNodeRegex);
                           if (m.Success)
                           {
                              if (lineParts.Length == 3) //multi-line subnodes
                              {
                                 TreeNode node = new TreeNode(subnodeId);
                                 nodeStack.Peek().Nodes.Add(node);
                                 nodeStack.Push(node);
                              }
                              else //oneliners
                              {
                                 int dataLength = lineParts.Length - 4;
                                 string[] data = new string[dataLength];
                                 Array.Copy(lineParts, 3, data, 0, dataLength);
                                 LoadOneLiner(nodeStack.Peek().Nodes.Add(subnodeId), data);
                              }
                           }
                           else
                           {
                              switch (lineParts[1])
                              {
                                 case "HUD":
                                    {
                                       LoadHUD(saveFile, lineParts); //this assumes that HUD will only ever be on a single line!
                                       break;
                                    }
                                 default:
                                    {
                                       TreeNode node = new TreeNode(lineParts[1]);
                                       nodeStack.Peek().Nodes.Add(node);
                                       int dataLength = lineParts.Length - 2;
                                       string[] data = new string[dataLength];
                                       Array.Copy(lineParts, 2, data, 0, dataLength);
                                       LoadOneLiner(node, data);
                                       break;
                                    }
                              }
                           }
                        }
                        else
                        {
                           switch (lineParts[1])
                           {
                              case "Galaxy":
                                 {
                                    nodeStack.Push(saveFile.galaxyTreeNode);
                                    break;
                                 }
                              default:
                                 {
                                    TreeNode node = new TreeNode(lineParts[1]);
                                    nodeStack.Peek().Nodes.Add(node);
                                    if (lineParts.Length == 2)
                                    {
                                       nodeStack.Push(node);
                                    }
                                    else
                                    {
                                       int dataLength = lineParts.Length - 3;
                                       string[] data = new string[dataLength];
                                       Array.Copy(lineParts, 2, data, 0, dataLength);
                                       LoadOneLiner(node, data);
                                    }
                                    break;
                                 }
                           }
                        }
                        break;
                     }
                  case "END":
                     {
                        nodeStack.Pop();
                        if (nodeStack.Count == 0)
                        {

                        }
                        break;
                     }
                  default:
                     {
                        if (nodeStack.Count > 1 && nodeStack.Peek().Parent.Text == "Missions")
                        {
                           if (lineParts[0] == "Type") nodeStack.Peek().Text += $" ({lineParts[1]})";
                           //else if (lineParts[0] == "Resource") ;
                        }
                        nodeStack.Peek().Nodes.Add($"{lineParts[0]}={lineParts[1]}");
                        break;
                     }
               }
            }
         }

      }
      public static SaveFile LoadFile(string filename)
      {
         SaveFile saveFile = new SaveFile(filename);
         LoadFile(saveFile,filename);
         //uint indentation = 0;
         return saveFile;
      }

   }
   internal class Program
   {
      static Form1? Form1;
      /// <summary>
      ///  The main entry point for the application.
      /// </summary>
      [STAThread]
      static void Main()
      {
         // To customize application configuration such as set high DPI settings or default font,
         // see https://aka.ms/applicationconfiguration.
         ApplicationConfiguration.Initialize();
         Form1 = new Form1();
         Application.Run(Form1);
      }

      private static void LoadHUD(SaveFile saveFile, string[] HudData)
      {
         for (int i = 2; i < HudData.Length - 1; i += 2)
         {
            saveFile.hudTreeNode.Nodes.Add($"{HudData[i]}={HudData[i + 1]}");
//            saveFile.HUD.Add(HudData[i], HudData[i + 1]);
         }
      }
      private static void LoadOneLiner(TreeNode node, string[] Data)
      {
         for (int i = 0; i < Data.Length - 1; i += 2)
         {
            string addlData = String.Empty;
            string addlName = String.Empty;
            
            if(node.Parent.Text == "Hazards" && Data[i] == "Type")
            {
               if (Data[i + 1] == "1") addlData = " (asteroid field)";
               else if (Data[i + 1] == "2") addlData = " (gas cloud)";
               if(addlData != String.Empty)
               {
                  addlName = addlData;
               }
            }
            else if(node.Parent.Text == "Objects" && node.Parent.Parent.Text == "Galaxy" && Data[i] == "Name")
            {
               addlName = $" ({Data[i + 1]})";
            }
            if(addlName != String.Empty)
            {
               node.Text = node.Text + addlName;
            }
            
            node.Nodes.Add($"{Data[i]}={Data[i + 1]}{addlData}");
            //            saveFile.HUD.Add(HudData[i], HudData[i + 1]);
         }
      }

      public static void LoadFile(string filename)
      {
         //uint indentation = 0;
         if (File.Exists(filename))
         {
            string subNodeRegex = @"""\[i \d+\]""";
            
            Stack <TreeNode> nodeStack = new Stack<TreeNode>();
            SaveFile saveFile = new SaveFile(Path.GetFileName(filename));
            nodeStack.Push(saveFile.root);
            Debug.WriteLine(filename);
            TextReader reader = new StreamReader(filename);
            string text = reader.ReadToEnd();
            bool quit = false;
            string[] foo = new string[5];
            foreach (string line in text.Split(Environment.NewLine))
            {
               if(line.Length < 1) continue;
               if(quit)
               {
                  break;
               }
               string[] lineParts = line.Split(" ").ToList().Where(x => !string.IsNullOrEmpty(x)).ToArray<string>();
               switch (lineParts[0])
               {
                  case "TimeIndex":
                     {
                        saveFile.TimeIndex = float.Parse(lineParts[1]);
                        break;
                     }
                  case "NextId":
                     {
                        saveFile.NextId = uint.Parse(lineParts[1]);
                        break;
                     }
                  case "DeltaTime":
                     {
                        saveFile.DeltaTime = float.Parse(lineParts[1]);
                        break;
                     }
                  case "PlayTime":
                     {
                        saveFile.PlayTime = float.Parse(lineParts[1]);
                        break;
                     }
                  case "SaveVersion":
                     {
                        saveFile.SaveVersion = uint.Parse(lineParts[1]);
                        break;
                     }
                  case "GameMode":
                     {
                        saveFile.GameMode = lineParts[1];
                        break;
                     }
                  case "BEGIN":
                     {
                        if (lineParts.Length > 2)
                        {
                           string subnodeId = $"{lineParts[1]} {lineParts[2]}";
                           Match m = Regex.Match(subnodeId, subNodeRegex);
                           if (m.Success)
                           {
                              if (lineParts.Length == 3) //multi-line subnodes
                              {
                                 TreeNode node = new TreeNode(subnodeId);
                                 nodeStack.Peek().Nodes.Add(node);
                                 nodeStack.Push(node);
                              }
                              else //oneliners
                              {
                                 int dataLength = lineParts.Length - 4;
                                 string[] data = new string[dataLength];
                                 Array.Copy(lineParts, 3, data, 0, dataLength);
                                 LoadOneLiner(nodeStack.Peek().Nodes.Add(subnodeId), data);
                              }
                           }
                           else
                           {
                              switch (lineParts[1])
                              {
                                 case "HUD":
                                    {
                                       LoadHUD(saveFile, lineParts); //this assumes that HUD will only ever be on a single line!
                                       break;
                                    }
                                 default:
                                    {
                                       TreeNode node = new TreeNode(lineParts[1]);
                                       nodeStack.Peek().Nodes.Add(node);
                                       int dataLength = lineParts.Length - 2;
                                       string[] data = new string[dataLength];
                                       Array.Copy(lineParts, 2, data, 0, dataLength);
                                       LoadOneLiner(node, data);
                                       break;
                                    }
                              }
                           }
                        }
                        else
                        {
                           switch (lineParts[1])
                           {
                              case "Galaxy":
                                 {
                                    nodeStack.Push(saveFile.galaxyTreeNode);
                                    break;
                                 }
                              default:
                                 {
                                    TreeNode node = new TreeNode(lineParts[1]);
                                    nodeStack.Peek().Nodes.Add(node);
                                    if (lineParts.Length == 2)
                                    {
                                       nodeStack.Push(node);
                                    }
                                    else
                                    {
                                       int dataLength = lineParts.Length - 3;
                                       string[] data = new string[dataLength];
                                       Array.Copy(lineParts, 2, data, 0, dataLength);
                                       LoadOneLiner(node, data);
                                    }
                                    break;
                                 }
                           }
                        }
                        break;
                     }
                  case "END":
                     {
                        nodeStack.Pop();
                        if(nodeStack.Count == 0)
                        {

                        }
                        break;
                     }
                     default:
                     {
                        if (nodeStack.Count > 1 && nodeStack.Peek().Parent.Text == "Missions")
                        {
                           if (lineParts[0] == "Type") nodeStack.Peek().Text += $" ({lineParts[1]})";
                           //else if (lineParts[0] == "Resource") ;
                        }
                        nodeStack.Peek().Nodes.Add($"{lineParts[0]}={lineParts[1]}");
                        break;
                     }
               }
            }
            Form1!.DisplaySaveData(saveFile);
         }
      }
   }
}