using System;
using System.Collections;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaSSI
{
   public class FileWriter
   {
      private readonly int IndentationAmount = 4;
      public FileWriter()
      {

      }
      public async Task<bool> WriteFile(Node root, string Filename, IProgress<int>? progressBar = null)
      {
         return await Task.Run(() =>
         {
            Stopwatch stopwatch = Stopwatch.StartNew();
            string rootdata = RenderRoot(root.Properties);
            StringBuilder sb = new(Environment.NewLine + rootdata);
            int nodeCount = 0;
            int reportBreakpoint = (int)(root.Children.Count * 0.01);
            int reportVal = 0;
            foreach (Node child in root.Children.Cast<Node>())
            {
               nodeCount++;
               if (nodeCount % reportBreakpoint == 0)
               {
                  reportVal++;
                  progressBar?.Report(reportVal);
               }
               RenderLine(child, sb);
            }

            using var sw = new StreamWriter(Filename);
            sw.Write(sb);
            sw.Flush();
            stopwatch.Stop();
            Debug.WriteLine($"File save took {stopwatch.ElapsedMilliseconds} ms.");
            return false;
         });
      }
      private static bool IsOneliner(Node item) // todo: this is a travesty
      {
         //string arrayPattern = "\\\"\\[i \\d+\\]\\\"";
         bool IsOneLiner;
         if (item.IsChildOf("Zones"))
         {
            OrderedDictionary dic = item.Properties;
            //if (item.Values[1] is OrderedDictionary dic)
            //{
            if (dic.Contains("Entities"))
            {
               string bar = dic["Entities"]!.ToString()!;
               int baz = bar.Count(c => c == ',');
               if (baz < 5) // todo: get rid of the magic number!
               {
                  IsOneLiner = true;
               }
               else
               {
                  IsOneLiner = false;
               }
            }
            else
            {
               IsOneLiner = true;
            }
            //}
            //else
            //{
            //   IsOneLiner = true;
            //}
         }
         else if (item.Name == "WorkQueue" && item.Children.Count == 0)
         {
            //if (item.Values[1] is OrderedDictionary dic)
            //{
            OrderedDictionary dic = item.Properties;
            if (dic.Contains("Workers"))
            {
               string bar = dic["Workers"]!.ToString()!;
               int baz = bar.Count(s => s == ',');
               if (baz < 5) // todo: get rid of the magic number!
               {
                  IsOneLiner = true;
               }
               else
               {
                  IsOneLiner = false;
               }
            }
            else
            {
               IsOneLiner = true;
            }
            //}
            //else
            //{
            //   IsOneLiner = true;
            //}
         }
         else if (item.Name == "Cells")
         {
            OrderedDictionary dic = item.Properties;
            if (dic.Count == 0)
            {
               IsOneLiner = true;
            }
            else
            {
               IsOneLiner = false;
            }
         }
         else if (item.Children.Count == 0
            && item.Properties.Count <= 10
            && item.Name != "Palette"
            && item.Name != "OurStock"
            && item.Name != "TheirStock"
            && item.Name != "Paint")
         {
            IsOneLiner = true;
         }
         else if ((item.Name == "Palette" || item.Name == "OurStock" || item.Name == "TheirStock")
            && item.Properties.Count == 0)
         {
            IsOneLiner = true;
         }
         else if (item.Name == "Paint")
         {
            IsOneLiner = item.IsChildOf("GridMap") && item.IsDescendantOf("SystemArchives"); // todo: this might become a problem in the future. again.
         }
         else
         {
            IsOneLiner = false;
         }

         return IsOneLiner;
      }
      private void RenderLine(Node item, StringBuilder sb, int indentationLevel = 0)
      {
         if (IsOneliner(item))
         {
            RenderOneLiner(item, sb, indentationLevel);
         }
         else
         {
            RenderMultiliner(item, sb, indentationLevel);
         }
      }
      private static string CleanName(Node item)
      {
         string name = item.Name;
         if (name.Contains('('))
         {
            name = name[..name.IndexOf("(")];
         }
         return name;
      }
      private string GetIndentPad(int indentationLevel = 0)
      {
         return new string(' ', indentationLevel * IndentationAmount);
      }
      private void RenderOneLiner(Node item, StringBuilder sb, int indentationLevel = 0)
      {
         string name = CleanName(item);
         string indent = GetIndentPad(indentationLevel);
         sb.Append($"{indent}BEGIN {name}{new string(' ', IndentationAmount)}");
         RenderProperties(item, sb);
         sb.Append($"END{Environment.NewLine}");
      }
      private void RenderMultiliner(Node item, StringBuilder sb, int indentationLevel = 0)
      {
         string name = CleanName(item);
         string indent = GetIndentPad(indentationLevel);
         indentationLevel++;
         sb.Append($"{indent}BEGIN {name}{Environment.NewLine}");
         if (name == "PowerGrid" || name == "Palette")
         {
            int index = 0;
            if (name == "PowerGrid" && item.Properties["LayerId"] is not null)
            {
               index = 1;
               RenderProperties(item, sb, indentationLevel, true, index);
            }

            PropertiesToNodes(item, sb, indentationLevel, index);
         }
         else
         {
            RenderProperties(item, sb, indentationLevel, true);
         }

         foreach (Node child in item.Children.Cast<Node>())
         {
            RenderLine(child, sb, indentationLevel);
         }
         sb.Append($"{indent}END{Environment.NewLine}");
      }
      private void RenderProperties(Node item, StringBuilder sb, int indentationLevel = 0, bool multiline = false, int truncateIndex = -1)
      {
         int counter = 0;
         string indent = GetIndentPad(indentationLevel);
         foreach (DictionaryEntry entry in item.Properties)
         {
            sb.Append($"{indent}{entry.Key} {entry.Value}  ");

            if (multiline) sb.Append(Environment.NewLine);
            counter++;
            if (truncateIndex > -1 && counter >= truncateIndex)
            {
               break;
            }
         }
      }
      private void PropertiesToNodes(Node item, StringBuilder sb, int indentationLevel = 0, int startIndex = 0)
      {
         string name = CleanName(item);
         string indent = GetIndentPad(indentationLevel);
         int counter = 0;
         foreach (DictionaryEntry entry in item.Properties)
         {
            if (counter < startIndex)
            {
               counter++;
               continue;
            }
            string key = $"{entry.Key}";
            string value = $"{entry.Value}";
            if (name == "PowerGrid")
            {
               if (key.Contains(' '))
               {
                  key = key[..key.IndexOf(' ')];
               }

               if (value == "Setting 0")
               {
                  value = string.Empty;
               }
            }
            sb.Append($"{indent}BEGIN {key}{indent}{value}  END{Environment.NewLine}");
         }
      }
      private static string RenderRoot(OrderedDictionary dictionary)
      {
         string data = string.Empty;
         if (dictionary.Count != 0)
         {
            foreach (DictionaryEntry p in dictionary)
            {
               int keylen = p.Key.ToString()!.Length;
               //string pad = new string(' ', (22 - keylen - 1)); //magic numbers screwed us again!
               string pad = " "; // less pretty, but future-proof
               data += $"{p.Key}{pad}{p.Value}  {Environment.NewLine}";
            }
         }

         return data;
      }
   }
}

