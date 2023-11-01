using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace LaSSI
{
   public class SaveFilev2
   {
      public string TimeIndex { get; set; } = string.Empty;
      public string NextId { get; set; } = string.Empty;
      public string DeltaTime { get; set; } = string.Empty;
      public string PlayTime { get; set; } = string.Empty;
      public string SaveVersion { get; set; } = string.Empty;
      public string GameMode { get; set; } = "FreeRoam";
      public Dictionary<string, string> HUD;
      public Node hudNode { get; set; }
      public Node galaxyNode { get; set; }
      public Node root { get; set; }
      public static readonly string NewLineUnix = @"\n";
      public static readonly string NewLineWindows = @"\r\n";
      public SaveFilev2(string filename)
      {
         //GameMode = string.Empty;
         HUD = new Dictionary<string, string>();
         hudNode = new Node("Hud");
         galaxyNode = new Node("Galaxy");
         root = new Node($"{filename}");
         root.Children.Add(hudNode);
         root.Children.Add(galaxyNode);
      }
      private static void LoadHUD(SaveFilev2 saveFile, string[] HudData)
      {
         for (int i = 2; i < HudData.Length - 1; i += 2)
         {
            saveFile.hudNode.Properties.Add($"{HudData[i]}", $"{HudData[i + 1]}");
         }
      }
      private static Dictionary<string, string> LoadDictionary(string[] Data, int start, int end)
      {
         Dictionary<string, string> Dictionary = new Dictionary<string, string>();
         for (int i = start; i < end; i += 2)
         {
            Dictionary.Add(Data[i], Data[i + 1]);
         }
         return Dictionary;
      }
      public static void LoadFile(SaveFilev2 saveFile, string filename)
      {
         if (File.Exists(filename))
         {
            string subNodeRegex = @"""\[i \d+\]""";
            string newlinechar = Environment.NewLine;
            Stack<Node> nodeStack = new Stack<Node>();
            //saveFile = new SaveFile(Path.GetFileName(filename));
            nodeStack.Push(saveFile.root);
            Debug.WriteLine(filename);
            TextReader reader = new StreamReader(filename);
            string text = reader.ReadToEnd();
            reader.Dispose();
            bool quit = false;
            string[] foo = new string[5];
            if (text.Contains(Environment.NewLine))
            {
               //newlinechar = Environment.NewLine;
               Debug.WriteLine("file matches this system");
            }
            else
            {
               if (text.Contains(NewLineUnix))
               {
                  newlinechar = NewLineUnix;
                  Debug.WriteLine("file uses Unix new line characters");
               }
               else if (text.Contains(NewLineWindows))
               {
                  newlinechar = NewLineWindows;
                  Debug.WriteLine("file uses Windows new line characters");
               }
               else
               {
                  Debug.WriteLine("Um, what? Save file apparently does not use Unix or Windows new line characters");
               }
            }
            foreach (string line in text.Split(newlinechar))
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
                           if (m.Success && lineParts.Length == 3) //we found an array line, multi-part
                           {
                              Node node = new Node(subnodeId);
                              nodeStack.Peek().Add(node);
                              nodeStack.Push(node);
                           }
                           else //not an array line or a one-liner
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
                                       int start = 2; // non-array one-liners (e.g., BEGIN Orders Salvage true...) have worthwhile data starting at index 2
                                       if (m.Success) // OTOH, array one-liners (e.g., BEGIN "[i 0]"      StringId mission_sectorrescue_title...) have worthwhile data starting at index 3 (damn off-by-ones...)
                                       {
                                          start++;
                                       }
                                       else
                                       {
                                          subnodeId = lineParts[1];
                                       }
                                       int end = lineParts.Length - start + 1;
                                       Node node = new Node(subnodeId, LoadDictionary(lineParts, start, end), nodeStack.Peek());
                                       nodeStack.Peek().Add(node);
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
                                    nodeStack.Peek().Add(node);
                                    if (lineParts.Length == 2)
                                    {
                                       nodeStack.Push(node);
                                    }
                                    else
                                    {
                                       Debug.WriteLine("Hey, something that shouldn't be both not greater than 2 and not equal to 2 somehow was!");
                                    }
                                    break;
                                 }
                           }
                        }
                        break;
                     }
                  case "END":
                     {
                        Node node = nodeStack.Pop();
                        node.AddAddlNameDetails();
                        
                        /*if (nodeStack.Count == 0)
                        {

                        }*/
                        break;
                     }
                  case "NextId":
                     {
                        if (nodeStack.Peek() == saveFile.root && saveFile.NextId == string.Empty)
                        {
                           saveFile.NextId = lineParts[1];
                        }
                        else
                        {
                           nodeStack.Peek().Properties.Add(lineParts[0], lineParts[1]);
                        }
                        break;
                     }
                  default:
                     {
                        Node curNode = nodeStack.Peek();
                        string key = lineParts[0], value = lineParts[1];
                        if (curNode.IsResearch())
                        {
                           if (lineParts.Length > 2)
                           {
                              value = line[line.IndexOf("\"")..].TrimStart('"', '[').TrimEnd(']', '"', ' ');
                           }
                           else
                           {
                              value = lineParts[1].TrimStart('[').TrimEnd(']');
                           }
                        }
                        else if (curNode.IsLayer()) //remember, layers include both "FreeSpace" and all ships/stations!
                        {
                           if(lineParts.Length > 2)
                           {
                              if(key == "Name")
                              {
                                 value = line[line.IndexOf("\"")..].Trim();
                              }
                           }
                        }
                        if (key != string.Empty && value != string.Empty) //unneccessary?
                        {
                           curNode.Properties.Add(key, value);
                        }
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
