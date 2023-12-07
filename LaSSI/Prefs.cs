using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Eto.Forms;
using Newtonsoft.Json;

namespace LaSSI
{

   internal enum PrefType
   {
      freetext,
      checkbox,
      dropdown,
      alwaysneverprompt,
      yesno,
      number,
      file
   }
   internal enum alwaysneverprompt
   {
      prompt,
      never,
      always
   }
   internal enum yesno
   {
      no,
      yes
   }
   public class Prefs
   {
      internal List<Pref> defaultPrefs = new List<Pref>();
      Pref saveBeforeQuitting = new Pref("Save before quitting", "Prompt", PrefType.alwaysneverprompt);
      Pref applyBeforeSaving = new Pref("Apply before saving", "Prompt", PrefType.alwaysneverprompt);
      Pref autoSave = new Pref("Autosave", "No", PrefType.checkbox);
      Pref autoReload = new Pref("Auto-reload", "No", PrefType.checkbox);
      Pref startupFile = new Pref("Startup file", "", PrefType.file);
      Pref backup = new Pref("Backup", "Yes", PrefType.checkbox);
      Pref backupRetention = new Pref("Backup retention (days)", "30", PrefType.number);
      Pref retainPositionAndSize = new Pref("Remember window size and position", "No", PrefType.checkbox);

      //public string GameDataFile { get; set; }
      //public string CapturedFolder { get; set; }
      //public string EditFolder { get; set; }
      //public string RecodeFolder { get; set; }
      //public string UploadFolder { get; set; }
      //public string DeleteFolder { get; set; }
      //public Boolean ConfirmChanges { get; set; }
      //public Boolean Autoload { get; set; }
      //public string PrefsFile { get; set; }
      //public string DefaultMoveAction { get; set; }
      //public string PlayerName { get; set; }
      public Prefs()
      {
         defaultPrefs.Add(saveBeforeQuitting);
         defaultPrefs.Add(applyBeforeSaving);
         defaultPrefs.Add(autoSave);
         defaultPrefs.Add(autoReload);
         defaultPrefs.Add(startupFile);
         defaultPrefs.Add(backup);
         defaultPrefs.Add(backupRetention);
         defaultPrefs.Add(retainPositionAndSize);
      }
      //public void SavePrefs()
      //{
      //   if (String.IsNullOrEmpty(this.PrefsFile))
      //   {
      //      string AppDirectory = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
      //      this.PrefsFile = System.IO.Path.Combine(AppDirectory, "Preferences.xml");
      //   }
      //   if (File.Exists(PrefsFile))
      //   {
      //      try
      //      {
      //         File.Delete(PrefsFile);
      //      }
      //      catch
      //      {
      //         // TODO: ???
      //         Debug.WriteLine("Failed to delete old preferences files. I don't know if this is a problem.");
      //      }
      //   }
      //   System.IO.FileStream file = System.IO.File.Create(PrefsFile);

      //   System.Xml.Serialization.XmlSerializer xmlSerializer = new System.Xml.Serialization.XmlSerializer(this.GetType());

      //   xmlSerializer.Serialize(file, this);
      //   file.Close();
      //}

      public static Prefs LoadPrefs(string PrefsFile = "")
      {
         Prefs prefs = null!;
         if (PrefsFile != "")
         {
            TextReader reader = new StreamReader(PrefsFile);
            string text = reader.ReadToEnd();
            prefs = JsonConvert.DeserializeObject<Prefs>(text);
            reader.Dispose();
         }

         if (prefs is null)
         {
            prefs = new Prefs();
         }

         return prefs;
      }
      //public static void GenerateDefaultPrefsFile(string directory)
      //{
      //   string GameDataFilePath = System.IO.Path.Combine(directory, "GameData.txt");
      //   Prefs prefs = new Prefs
      //   {
      //      GameDataFile = GameDataFilePath,
      //      ConfirmChanges = true
      //   };
      //   prefs.SavePrefs();
      //}
   }

   internal class PrefsForm : Form
   {
      private Prefs Prefs;
      public PrefsForm(Prefs prefs)
      {
         Prefs = prefs;
         Content = InitPrefsPanel();
      }
      private DynamicLayout InitPrefsPanel()
      {
         DynamicLayout mainLayout = new DynamicLayout()
         {
            Padding = 5,
            Spacing = new Eto.Drawing.Size(5, 5)
         };
         foreach (var pref in Prefs.defaultPrefs) // todo: change this
         {
            mainLayout.AddSeparateRow(CreateRow(pref));
         }

         return mainLayout;
      }
      private static Control[] CreateRow(Pref pref)
      {
         Label prefLabel = new() { Text = pref.name, TextAlignment = TextAlignment.Center };
         Control control = null!;
         switch (pref.prefType)
         {
            case PrefType.freetext:
               {
                  control = new TextBox();
                  ((TextBox)control).Text = pref.value;
                  break;
               }
            case PrefType.checkbox:
               {
                  control = new CheckBox();
                  break;
               }
            case PrefType.dropdown:
               {
                  control = new DropDown();
                  break;
               }
            case PrefType.alwaysneverprompt:
               {
                  control = CreateDropDown(Enum.GetValues(typeof(alwaysneverprompt)), pref.value);
                  break;
               }
            case PrefType.yesno:
               {
                  break;
               }
            case PrefType.number:
               {
                  NumericStepper s = new()
                  {
                     Value = int.Parse(pref.value)
                  };
                  control = s;
                  break;
               }
            case PrefType.file:
               {
                  // file picker
                  break;
               }
         }

         return new Control[] { prefLabel, control!, null! };
      }
      internal static DropDown CreateDropDown(Array options, string defaultValue = "")
      {
         DropDown dropDownList = new();
         for (int i = 0; i <= options.Length - 1; i++)
         {
            ListItem c = new()
            {
               Key = i.ToString(),
               Text = options.GetValue(i)!.ToString()
            };
            dropDownList.Items.Add(c);
            if (c.Text!.Equals(defaultValue, StringComparison.OrdinalIgnoreCase)) dropDownList.SelectedIndex = i;
         }
         return dropDownList;
      }
   }

   internal class Pref
   {

      internal string name = string.Empty;
      internal string value = string.Empty;
      internal PrefType prefType = PrefType.freetext;
      internal string defaultValue = string.Empty;

      public Pref()
      {

      }
      public Pref(string name)
      {
         this.name = name;
      }
      public Pref(string name, string value)
      {
         this.name = name;
         this.value = value;
      }
      public Pref(string name, string value, PrefType prefType)
      {
         this.name = name;
         this.value = value;
         this.prefType = prefType;
      }
   }
}

