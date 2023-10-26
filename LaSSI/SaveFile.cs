using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace LaSSI
{
   public class SaveFilev2
   {
      public string TimeIndex { get; set; }
      public string NextId { get; set; }
      public string DeltaTime { get; set; }
      public string PlayTime { get; set; }
      public string SaveVersion { get; set; }
      public string GameMode { get; set; }
      public Dictionary<string, string> HUD;
      public Node hudNode { get; set; }
      public Node galaxyNode { get; set; }
      public Node root { get; set; }
      public SaveFilev2(string filename)
      {
         GameMode = string.Empty;
         HUD = new Dictionary<string, string>();
         hudNode = new Node();
         hudNode.Text = "HUD";
         hudNode.Name = "HudNode";
         galaxyNode = new Node();
         galaxyNode.Text = "Galaxy";
         galaxyNode.Name = "GalaxyNode";
         root = new Node(filename);
         root.Children.Add(hudNode);
         root.Children.Add(galaxyNode);
      }
      private static void LoadHUD(SaveFilev2 saveFile, string[] HudData)
      {
         for (int i = 2; i < HudData.Length - 1; i += 2)
         {
            saveFile.hudNode.Properties.Add($"{HudData[i]}",$"{HudData[i + 1]}");
            //            saveFile.HUD.Add(HudData[i], HudData[i + 1]);
         }
      }
      private static void LoadOneLiner(Node node, string[] Data)
      {
         for (int i = 0; i < Data.Length - 1; i += 2)
         {
            string addlData = String.Empty;
            string addlName = String.Empty;

            if (node.Parent != null && node.Parent.Text == "Hazards" && Data[i] == "Type")
            {
               if (Data[i + 1] == "1") addlData = " (asteroid field)";
               else if (Data[i + 1] == "2") addlData = " (gas cloud)";
               if (addlData != String.Empty)
               {
                  addlName = addlData;
               }
            }
            else if (node.Parent != null && node.Parent.Text == "Objects" && node.Parent.Parent != null && node.Parent.Text == "Galaxy" && Data[i] == "Name")
            {
               addlName = $" ({Data[i + 1]})";
            }
            if (addlName != String.Empty)
            {
               node.Text = node.Text + addlName;
            }

            node.Children.Add(new Node($"{Data[i]}={Data[i + 1]}{addlData}"));
            //            saveFile.HUD.Add(HudData[i], HudData[i + 1]);
         }
      }
      public static void LoadFile(SaveFilev2 saveFile, string filename)
      {
         if (File.Exists(filename))
         {
            string subNodeRegex = @"""\[i \d+\]""";

            Stack<Node> nodeStack = new Stack<Node>();
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
                        saveFile.TimeIndex = lineParts[1];
                        break;
                     }
                  case "NextId":
                     {
                        if (saveFile.NextId == string.Empty)
                        {
                           saveFile.NextId = lineParts[1];
                        }
                        break;
                     }
                  case "DeltaTime":
                     {
                        saveFile.DeltaTime = lineParts[1];
                        break;
                     }
                  case "PlayTime":
                     {
                        saveFile.PlayTime = lineParts[1];
                        break;
                     }
                  case "SaveVersion":
                     {
                        saveFile.SaveVersion = lineParts[1];
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
                                 Node node = new Node(subnodeId);
                                 nodeStack.Peek().Children.Add(node);
                                 nodeStack.Push(node);
                              }
                              else //oneliners
                              {
                                 int dataLength = lineParts.Length - 4;
                                 string[] data = new string[dataLength];
                                 Array.Copy(lineParts, 3, data, 0, dataLength);
                                 Node subNode = new Node(subnodeId);
                                 LoadOneLiner(subNode, data);
                                 nodeStack.Peek().Children.Add(subNode);
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
                                       Node node = new Node(lineParts[1]);
                                       nodeStack.Peek().Children.Add(node);
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
                                    nodeStack.Push(saveFile.galaxyNode);
                                    break;
                                 }
                              default:
                                 {
                                    Node node = new Node(lineParts[1]);
                                    nodeStack.Peek().Children.Add(node);
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
                        if (nodeStack.Count > 1 && nodeStack.Peek().Parent != null && nodeStack.Peek().Parent!.Text == "Missions")
                        {
                           if (lineParts[0] == "Type") nodeStack.Peek().Text += $" ({lineParts[1]})";
                           //else if (lineParts[0] == "Resource") ;
                        }
                        nodeStack.Peek().Children.Add(new Node($"{lineParts[0]}={lineParts[1]}"));
                        break;
                     }
               }
            }
         }

      }
      public static SaveFilev2 LoadFile(string filename)
      {
         SaveFilev2 saveFile = new SaveFilev2(filename);
         LoadFile(saveFile, filename);
         //uint indentation = 0;
         return saveFile;
      }

   }
}
