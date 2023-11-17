using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace LaSSI
{
   public class SaveFilev2
   {
      public string Filename = string.Empty;
      public Node HudNode { get; set; } = new Node("HUD");
      public Node GalaxyNode { get; set; } = new Node("Galaxy");
      public Node Root { get; set; } = new Node();
      public static readonly string NewLineUnix = "\n";
      public static readonly string NewLineRegexUnix = @"(?<!\r)\n";
      public static readonly string NewLineWindows = "\r\n";
      public static readonly string NewLineRegexWindows = @"\r\n";
      private static readonly string subNodeRegex = @"""\[i \d+\]""";
      private static readonly List<string> RootPropertyNames = new()
      {
         "TimeIndex",
         "NextId",
         "DeltaTime",
         "PlayTime",
         "SaveVersion",
         "GameMode",
         "ShipArrivalTimer"
      };
      public SaveFilev2()
      {
         
      }
      public SaveFilev2(string filename)
      {
         Filename = filename;
         //GameMode = string.Empty;
         Root = new Node($"{Path.GetFileName(filename)}");
         Root.Children.Add(HudNode);
         Root.Children.Add(GalaxyNode);
      }
      private static void LoadHUD(SaveFilev2 saveFile, string[] HudData)
      {
         for (int i = 2; i < HudData.Length - 1; i += 2)
         {
            saveFile.HudNode.Properties.Add($"{HudData[i]}", $"{HudData[i + 1]}");
         }
      }
      private static OrderedDictionary LoadDictionary(string[] Data)
      {
         OrderedDictionary Dictionary = new OrderedDictionary();
         for (int i = 0; i < Data.Length; i += 2)
         {
            Dictionary.Add(Data[i], Data[i + 1]);
         }
         return Dictionary;
      }
      private static string GetNewLineChar(string sample)
      {
         string newlinechar = Environment.NewLine;
         if (Regex.IsMatch(sample, NewLineRegexUnix))
         {
            newlinechar = NewLineUnix;
            Debug.WriteLine("file uses Unix new line characters");
         }
         else if (Regex.IsMatch(sample, NewLineRegexWindows))
         {
            newlinechar = NewLineWindows;
            Debug.WriteLine("file uses Windows new line characters");
         }
         else
         {
            Debug.WriteLine("Um, what? Save file apparently does not use Unix or Windows new line characters");
         }

         return newlinechar;
      }
      private void AddPropertyToRootNode(string key, string value)
      {
         this.Root.Properties.Add(key, value);
      }
      private static bool IsRootNodeProperty(string name)
      {
         return RootPropertyNames.Contains(name);
      }
      public void Load()
      {
         LoadFile(this, this.Filename);
      }
      public static void LoadFile(SaveFilev2 saveFile, string filename)
      {
         if (!File.Exists(filename))
         {
            return;//todo: uh, display a message? throw an exception?
         }

         Stack<Node> nodeStack = new Stack<Node>();
         nodeStack.Push(saveFile.Root);
         Debug.WriteLine(filename);
         TextReader reader = new StreamReader(filename);
         string text = reader.ReadToEnd();
         reader.Dispose();
         bool quit = false;
         string newlinechar = GetNewLineChar(text[0..50]);
         string[] lines = text.Split(newlinechar, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

         foreach (string line in lines)
         {
            if (line.Length < 1) continue;
            if (quit)
            {
               break;
            }
            string[] lineParts = line.Split(" ").ToList().Where(x => !string.IsNullOrEmpty(x)).ToArray<string>();
            switch (lineParts[0])
            {
               case "BEGIN":
                  {
                     if (lineParts.Length > 2)
                     {
                        ProcessComplexLine(nodeStack, lineParts, saveFile);
                     }
                     else
                     {
                        ProcessSimpleLine(nodeStack, lineParts, saveFile);
                     }
                     break;
                  }
               case "END":
                  {
                     Node node = nodeStack.Pop();
                     node.AddAddlNameDetails();
                     break;
                  }
               default:
                  {
                     if (nodeStack.Peek() == saveFile.Root && IsRootNodeProperty(lineParts[0]))
                     {
                        saveFile.AddPropertyToRootNode(lineParts[0], lineParts[1]);
                        continue;
                     }
                     Node curNode = nodeStack.Peek();
                     string key = lineParts[0], value = lineParts[1];
                     if (curNode.IsResearch() || lineParts[0] == "Entities" || lineParts[0] == "Workers"
                        || lineParts[0] == "UnloadRequests" || lineParts[0] == "Items") // todo: clean this up
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
                     else if (curNode.IsLayer(true) || curNode.IsEditor(true)) //remember, layers include both "FreeSpace" and all ships/stations!
                     {
                        if (lineParts.Length > 2)
                        {
                           if (key == "Name" || key == "Author" || key.StartsWith("row"))//todo: uh, something better than writing out all possibilities
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
      /// <summary>
      /// Processes a "complex" line, i.e., more than two parts (e.g., the HUD or a Hazard).
      /// </summary>
      /// <param name="nodeStack"></param>
      /// <param name="lineParts"></param>
      /// <param name="saveFile"></param>
      private static void ProcessComplexLine(Stack<Node> nodeStack, string[] lineParts, SaveFilev2 saveFile)
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
                     LoadHUD(saveFile, lineParts); //this handles the HUD if it's one line (mostly for backwards compat)
                     break;
                  }
               default:
                  {
                     ProcessComplexLineDefault(nodeStack.Peek(), lineParts, m, subnodeId);
                     break;
                  }
            }
         }
      }
      /// <summary>
      /// Handles the default case for complex lines.
      /// </summary>
      /// <param name="currentNode"></param>
      /// <param name="lineParts"></param>
      /// <param name="m"></param>
      /// <param name="subnodeId"></param>
      private static void ProcessComplexLineDefault(Node currentNode, string[] lineParts, Match m, string subnodeId)
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
         if (currentNode.IsPalette())
         {
            string key = lineParts[1];
            string value = string.Empty;
            for (int i = start; i < lineParts.Length - 1; i++)
            {
               value += lineParts[i] + " ";
            }
            currentNode.Properties.Add(key, value.Trim());
         }
         else if (currentNode.IsPowerGrid())
         {
            string CatName = GetPowerGridCategoryName(currentNode);
            if (lineParts.Length >= 4)
            {
               currentNode.Properties.Add($"{lineParts[1]} {CatName}", $"{lineParts[2]} {lineParts[3]}");
            }
            else if (lineParts.Length == 3)
            {
               currentNode.Properties.Add($"{lineParts[1]} {CatName}", "Setting 0");
            }
            else
            {
               Debug.WriteLine($"Unexpected number of line parts for a powergrid setting: {lineParts.Length}");
            }
         }
         else
         {
            var mode = IsWorkersOrEntities(lineParts);
            int WorkersIndex = Array.IndexOf(lineParts, mode); // todo: this assumes "Workers"/"Entities" is never the first element!
            int end = WorkersIndex > 0 ? WorkersIndex : lineParts.Length - 1;
            OrderedDictionary properties = LoadDictionary(lineParts[start..end]);
            if (WorkersIndex > 0)
            {
               string workers = string.Empty;
               for (int i = WorkersIndex + 1; i < lineParts.Length - 1; i++)
               {
                  workers += lineParts[i] + ' ';
               }
               workers = workers.TrimStart('"', '[').TrimEnd('"', ']', ' ');
               properties.Add(lineParts[WorkersIndex], workers);
            }
            Node node = new Node(subnodeId, properties, currentNode);
            currentNode.Add(node);
         }

      }
      private static string IsWorkersOrEntities(string[] data)
      {
         string mode = string.Empty;
         if (data.Contains("Workers"))
         {
            mode = "Workers";
         }
         else if (data.Contains("Entities"))
         {
            mode = "Entities";
         }

         return mode;
      }
      private static string GetPowerGridCategoryName(Node currentNode)
      {
         int n = currentNode.Properties.Count;

         if (currentNode.Properties.Contains("LayerId"))
         {
            n--;
         }

         string CatName;
         switch (n)
         {
            //case 0:
            //   {

            //      break;
            //   }
            //case 1:
            //   {

            //      break;
            //   }
            //case 2:
            //   {

            //      break;
            //   }
            case 3:
               {
                  CatName = "Engines";
                  break;
               }
            case 4:
               {
                  CatName = "FTL";
                  break;
               }
            case 5:
               {
                  CatName = "Weapons";
                  break;
               }
            //case 6:
            //   {

            //      break;
            //   }
            case 7:
               {
                  CatName = "Life Support";
                  break;
               }
            case 8:
               {
                  CatName = "Logistics";
                  break;
               }
            case 9:
               {
                  CatName = "Science";
                  break;
               }
            //case 10:
            //   {

            //      break;
            //   }
            default:
               {
                  CatName = n.ToString();
                  break;
               }
         }
         return CatName;
      }
      /// <summary>
      /// Processes a "simple" line, i.e., the beginning of a multiline node or key-value pair (e.g., "BEGIN Galaxy", "TargetSystem 11").
      /// </summary>
      /// <param name="nodeStack"></param>
      /// <param name="lineParts"></param>
      /// <param name="saveFile"></param>
      private static void ProcessSimpleLine(Stack<Node> nodeStack, string[] lineParts, SaveFilev2 saveFile)
      {
         switch (lineParts[1])
         {
            case "Galaxy":
               {
                  nodeStack.Push(saveFile.GalaxyNode);
                  break;
               }
            case "HUD":
               {
                  nodeStack.Push(saveFile.HudNode);
                  break;
               }
            default:
               {
                  ProcessSimpleLineDefault(nodeStack, lineParts);
                  break;
               }
         }
      }
      /// <summary>
      /// Handles the default case for simple lines.
      /// </summary>
      /// <param name="nodeStack"></param>
      /// <param name="lineParts"></param>
      private static void ProcessSimpleLineDefault(Stack<Node> nodeStack, string[] lineParts)
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
      }

      public static SaveFilev2 LoadFile(string filename)
      {
         SaveFilev2 saveFile = new SaveFilev2(filename);
         LoadFile(saveFile, filename);
         return saveFile;
      }

   }
}
