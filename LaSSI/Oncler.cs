using System;
using System.Collections;

namespace LaSSI
{
   public class Oncler
   {
      public string Key { get; set; }
      public string? Value { get; set; }

      public Oncler()
      {
         Key = string.Empty;
         Value = string.Empty;
      }
      public Oncler(string k)
      {
         Key = k;
         Value = string.Empty;
      }
      public Oncler(string k, string v)
      {
         Key = k;
         Value = v;
      }
      public Oncler(DictionaryEntry entry)
      {
         Key = entry.Key.ToString()!;
         if (entry.Value is not null)
         {
            Value = entry.Value.ToString()!;
         }
         else
         {
            Value = string.Empty;
         }
      }
      public DictionaryEntry ToDictionaryEntry()
      {
         return new DictionaryEntry(Key, Value);
      }
   }
}

