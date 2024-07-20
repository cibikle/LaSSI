using Eto.Forms;
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
      public TreeGridItemCollection root { get; set; } = new TreeGridItemCollection();
      public static readonly string NewLineUnix = "\n";
      public static readonly string NewLineRegexUnix = @"(?<!\r)\n";
      public static readonly string NewLineWindows = "\r\n";
      public static readonly string NewLineRegexWindows = @"\r\n";
      private static readonly string subnodeRegex = @"""\[i \d+\]""";
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
         root.Add(Root);
         Root.AddChild(HudNode);
         Root.AddChild(GalaxyNode);
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
            string key = Data[i];
            try
            {
               string value = Data[i + 1];

               if (value.StartsWith("\""))
               {
                  i++;
                  while (!value.EndsWith("\""))
                  {
                     value += " ";
                     i++;
                     value += Data[i];
                  }
                  i--;
               }
               Dictionary.Add(key, value);
            }
            catch (Exception e)
            {
               Console.WriteLine(e.Message);
            }
            //Dictionary.Add(Data[i], Data[i + 1]);
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
      internal TreeGridItemCollection FindNodes(Node node, string[] searchTerms)
      {
         TreeGridItemCollection nodes = new();
         bool match = false;
         int matches = 0;
         if (searchTerms.Length < 1 || node is null)
         {
            return nodes;
         }
         foreach (string term in searchTerms)
         {
            if (term.EndsWith(':'))//if term is property name
            {
               match = node.HasProperties(new string[] { term.TrimEnd(':') });
            }
            else if (term.Contains(':'))//if term is property name/value pair
            {
               string[] keyValue = term.Split(':');
               match = node.TryGetProperty(keyValue[0], out string value) && Regex.IsMatch(value, keyValue[1], RegexOptions.IgnoreCase);
            }
            else//if term is name ~~or property name~~
            {
               match = /*node.HasProperties(new string[] { term }) ||*/ node.NameMatches(new string[] { term });
            }
            if (match)
            {
               matches++;
            }
         }

         //if (match)
         if (matches == searchTerms.Length)
         {
            nodes.Add(node);
         }
         foreach (Node child in node.Children)
         {
            nodes.AddRange(FindNodes(child, searchTerms));
         }
         return nodes;
      }
      //internal TreeGridItemCollection FindNodes(Dictionary<string, string> searchTerms)
      //{

      //}
      public TreeGridItemCollection Search(string searchtext)
      {
         TreeGridItemCollection searchCollection;
         //Dictionary<string, string> properties = new();
         string[] searchTokens = searchtext.Split(" ");
         List<string> searchTerms = new();
         bool quote = false;
         foreach (var token in searchTokens)
         {
            if (quote)
            {
               searchTerms[^1] += ' ' + token;
            }
            else
            {
               searchTerms.Add(token);
            }

            if (token.Contains('"'))
            {
               quote = !quote;
            }
         }
         searchCollection = FindNodes(Root, searchTerms.ToArray());
         return searchCollection;
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

         Stack<Node> nodeStack = new();
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
                     if (curNode.IsResearch() || IsList(lineParts[0]))
                     {
                        if (lineParts.Length > 2)
                        {
                           value = line[line.IndexOf("\"")..];
                           if (value.StartsWith("\"["))
                           {
                              value = value.TrimStart('"', '[');
                           }
                           if (value.EndsWith("]\""))
                           {
                              value = value.TrimEnd(']', '"', ' ');
                           }
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
                        //if (lineParts[0] == "SystemId")
                        //{
                        //   string systemId = lineParts[1];
                        //   if (curNode.Parent is not null and Node parent)
                        //   {
                        //      if ((parent == saveFile.Root)
                        //         || (parent.IsSystemNode()
                        //         && parent.TryGetProperty("SystemId", out string currentSystemId)
                        //         && currentSystemId != systemId))
                        //      {
                        //         //remove curNode from parent's children
                        //         parent.RemoveChild(curNode);
                        //         //try to find correct system node in root's children
                        //         Node? systemNode = saveFile.Root.FindChild($"System {systemId}");
                        //         //create new system node if needed and add to root's children
                        //         if (systemNode is null)
                        //         {
                        //            OrderedDictionary d = new()
                        //            {
                        //               { lineParts[0], lineParts[1] }
                        //            };
                        //            systemNode = new Node($"System {systemId}", d);
                        //            parent.AddChild(systemNode);
                        //         }
                        //         //add curNode to new system node's children
                        //         systemNode.AddChild(curNode);
                        //      }
                        //   }
                        //}
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

      private static bool IsList(string key)
      {
         Regex number = new Regex(@"\d+");
         return key == "Entities" || key == "Workers"
                        || key == "UnloadRequests" || key == "Items" || key == "Completed"
                        || key == "Equipment" || number.IsMatch(key);
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
         Match m = Regex.Match(subnodeId, subnodeRegex);
         if (m.Success && lineParts.Length == 3) //we found an array line, multi-part
         {
            Node node = new Node(subnodeId);
            nodeStack.Peek().AddChild(node);
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
         else if (subnodeId.Equals("Episodes")) //todo: this could be a real problem
         {
            string value = string.Empty;
            int timerIndex = Array.IndexOf(lineParts, "CreateTimer");
            OrderedDictionary properties = LoadDictionary(lineParts[timerIndex..(timerIndex + 2)]);
            int completedIndex = Array.IndexOf(lineParts, "Completed");
            if (completedIndex > 0)
            {
               string key = lineParts[completedIndex];
               for (int i = completedIndex + 1; i < lineParts.Length - 1; i++)
               {
                  value += lineParts[i] + " ";
                  if (value.Contains(']'))
                  {
                     i = lineParts.Length;
                  }
               }
               properties.Add(key, value.TrimStart('\"', '[', ' ').TrimEnd(']', '\"', ' '));
            }
            Node node = new Node(subnodeId, properties, currentNode);
            currentNode.AddChild(node);
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
            var mode = IsWorkersOrEntitiesOrLayers(lineParts);
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
            currentNode.AddChild(node);
         }

      }
      private static string IsWorkersOrEntitiesOrLayers(string[] data) // todo: this is stupid
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
         else if (data.Contains("Layers"))
         {
            mode = "Layers";
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
            case 10:
               {
                  CatName = "Other"; //applies to the Generator; not sure what else
                  break;
               }
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
         nodeStack.Peek().AddChild(node);
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
