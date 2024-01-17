using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Eto;
using Eto.Drawing;
using Eto.Forms;
using Newtonsoft.Json;

namespace LaSSI
{

   public enum PrefType
   {
      freetext,
      checkbox,
      dropdown,
      alwaysneverprompt,
      yesno,
      number,
      file,
      startupbehavior
   }
   public enum StartupBehavior
   {
      //[Description("Nothing")]
      Nothing,
      //[Description("Show file chooser")]
      //ShowFileChooser,
      //[Description("Load last file")]
      LoadLastFile,
      LoadFile
   }
   public enum AlwaysNeverPrompt
   {
      prompt,
      never,
      always
   }
   public enum yesno
   {
      no,
      yes
   }
   public class Prefs
   {
      public List<Pref> defaultPrefs = new List<Pref>();
      public Pref saveBeforeQuitting = new Pref("Save before quitting", AlwaysNeverPrompt.prompt, PrefType.alwaysneverprompt);
      public Pref applyBeforeSaving = new Pref("Apply before saving", AlwaysNeverPrompt.always, PrefType.alwaysneverprompt);
      //Pref autoSave = new Pref("Autosave", yesno.no, PrefType.checkbox);
      //Pref autoReload = new Pref("Auto-reload", yesno.no, PrefType.checkbox);
      internal Pref startupFile = new Pref("Startup file", "", PrefType.file);
      //internal Pref autoLoad = new Pref("Auto-load last file on start", yesno.no, PrefType.checkbox);
      public Pref startupBehavior = new("Startup behavior", StartupBehavior.Nothing, PrefType.startupbehavior);
      internal Pref holidayFun = new("Holiday fun", yesno.yes, PrefType.checkbox, true);
      //Pref backup = new Pref("Backup", "Yes", PrefType.checkbox);
      //Pref backupRetention = new Pref("Backup retention (days)", "30", PrefType.number);
      //Pref retainPositionAndSize = new Pref("Remember window size and position", yesno.no, PrefType.checkbox);
      public MainForm MainForm;

      public Prefs(MainForm mainForm)
      {

         defaultPrefs.Add(applyBeforeSaving);
         defaultPrefs.Add(saveBeforeQuitting);
         //defaultPrefs.Add(autoSave);
         //defaultPrefs.Add(autoReload);
         //defaultPrefs.Add(autoLoad);
         //defaultPrefs.Add(backup);
         //defaultPrefs.Add(backupRetention);
         //defaultPrefs.Add(retainPositionAndSize);
         defaultPrefs.Add(startupBehavior);
         defaultPrefs.Add(startupFile);
         defaultPrefs.Add(holidayFun);
         if (LoadPrefs())
         {
            Debug.WriteLine("Loaded prefs");
         }
         else
         {
            Debug.WriteLine("Using default prefs");
         }
         MainForm = mainForm;
      }
      public void SavePrefs()
      {
         string PrefsFile = GetPrefsFile();
         if (File.Exists(PrefsFile))
         {
            try
            {
               File.Delete(PrefsFile);
            }
            catch
            {
               // TODO: ???
               Debug.WriteLine("Failed to delete old preferences files. I don't know if this is a problem.");
            }
         }
         string json = JsonConvert.SerializeObject(defaultPrefs);
         using StreamWriter sw = new(PrefsFile);
         sw.Write(json);
      }
      internal static string GetPrefsFile()
      {
         return Path.Combine(GetAppSupportDirectory(), "Preferences.json");
      }
      internal static string GetAppSupportDirectory()
      {
         string appSupport = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
         if (EtoEnvironment.Platform.IsMac)
         {
            appSupport = Path.Combine(appSupport, "Library", "Application Support");
         }
         else if (EtoEnvironment.Platform.IsWindows)
         {
            appSupport = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
         }
         else if (EtoEnvironment.Platform.IsLinux)
         {
            //todo
         }
         appSupport = Path.Combine(appSupport, "LaSSI");

         if (!Directory.Exists(appSupport))
         {
            _ = Directory.CreateDirectory(appSupport);
         }

         return appSupport;
      }
      internal static string GetBackupsDirectory()
      {
         return Path.Combine(GetAppSupportDirectory(), "backups");
      }
      public bool LoadPrefs()
      {
         string prefsFile = GetPrefsFile();
         if (!File.Exists(prefsFile))
         {
            return false;
         }

         using TextReader reader = new StreamReader(prefsFile);
         string text = reader.ReadToEnd();
         //List<Pref> storedPrefs = ;
         if (JsonConvert.DeserializeObject<List<Pref>>(text) is not null and List<Pref> storedPrefs)
         {
            foreach (var storedPref in storedPrefs)
            {
               if (FindPref(storedPref.name) is not null and Pref pref && storedPref.value is not null)
               {
                  pref.SetValue(storedPref.value);
               }
            }
         }
         return true;
      }
      internal Pref? FindPref(string ID)
      {
         return defaultPrefs.Find(x => x.name == ID);
      }
   }

   internal class PrefsDialog : Dialog
   {
      public event EventHandler? UiRefreshRequired;

      private Prefs Prefs;
      private Button? OKButton;
      private Button? CancelButton;
      public PrefsDialog(Prefs prefs)
      {
         Title = "Preferences";
         Prefs = prefs;
         Content = InitPrefsPanel();
         DefaultButton = OKButton;
         AbortButton = CancelButton;
         //KeyUp += (sender, e) => { if (e.Key == Keys.Escape && CancelButton is not null) { CancelButton.PerformClick(); } };
         ID = "PrefsDialog";
      }
      private DynamicLayout InitPrefsPanel()
      {
         Size space = new Size(5, 5);
         DynamicLayout mainLayout = new DynamicLayout()
         {
            Padding = 5,
            Spacing = space
         };
         foreach (var pref in Prefs.defaultPrefs) // todo: change this
         {
            mainLayout.AddSeparateRow(null, space, false, false, CreateRow(pref));
         }
         OKButton = new(OK_clicked)
         {
            Text = "OK",
         };
         CancelButton = new(Cancel_clicked)
         {
            Text = "Cancel"
         };
         StackLayout buttons = new StackLayout()
         {
            Orientation = Orientation.Horizontal
         };
         buttons.Items.Add(OKButton);
         buttons.Items.Add(CancelButton);
         mainLayout.BeginVertical(5, space);
         mainLayout.BeginHorizontal();
         mainLayout.Add(OKButton, true);
         mainLayout.Add(CancelButton, true);
         mainLayout.EndHorizontal();
         mainLayout.EndVertical();

         return mainLayout;
      }
      private Control[] CreateRow(Pref pref)
      {
         Label prefLabel = new() { Text = pref.name, /*TextAlignment = TextAlignment.Center */ VerticalAlignment = VerticalAlignment.Center };
         Control control = null!;
         switch (pref.prefType)
         {
            case PrefType.freetext:
               {
                  control = new TextBox();
                  ((TextBox)control).Text = pref.value!.ToString();
                  break;
               }
            case PrefType.checkbox:
               {
                  CheckBox checkBox = new()
                  {
                     ID = pref.name,
                  };
                  if (pref.value is not null and yesno value)
                  {
                     checkBox.Checked = value == yesno.yes;
                  }

                  checkBox.CheckedChanged += CheckBox_CheckedChanged;
                  control = checkBox;
                  break;
               }
            case PrefType.dropdown:
               {
                  control = new DropDown();
                  break;
               }
            case PrefType.alwaysneverprompt:
               {
                  if (pref.value is not null and AlwaysNeverPrompt value)
                  {
                     control = CreateDropDown(pref.name, Enum.GetValues(typeof(AlwaysNeverPrompt)), value.ToString());
                  }

                  break;
               }
            case PrefType.startupbehavior:
               {
                  if (pref.value is not null and StartupBehavior value)
                  {
                     control = CreateDropDown(pref.name, Enum.GetValues(typeof(StartupBehavior)), value.ToString());
                  }
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
                     Value = int.Parse(pref.value!.ToString()!)
                  };
                  control = s;
                  break;
               }
            case PrefType.file:
               {
                  TextBox startingSaveFileBox = new()
                  {
                     Enabled = false,
                     ID = $"{pref.name}-textbox",
                     Width = 200
                  };
                  if (pref.value is not null and string filename)
                  {
                     startingSaveFileBox.Text = Path.GetFileName(filename);
                  }
                  Button pickStartingSave = new()
                  {
                     Text = "Choose file",
                     ID = pref.name
                  };
                  pickStartingSave.Click += PickStartingSave_Click;
                  StackLayout stack = new StackLayout()
                  {
                     Orientation = Orientation.Horizontal,
                     Spacing = 5
                  };
                  stack.Items.Add(startingSaveFileBox);
                  stack.Items.Add(pickStartingSave);
                  control = stack;
                  break;
               }
         }

         return new Control[] { prefLabel, control!, null! };
      }

      private void CheckBox_CheckedChanged(object? sender, EventArgs e)
      {
         if (sender is not null and Control c)
         {
            PrefsDialog prefsDialog = (PrefsDialog)c.FindParent("PrefsDialog");
            if (Prefs.FindPref(c.ID) is not null and Pref pref && pref.prefType == PrefType.checkbox)
            {
               if (pref.uiRefresh)
               {
                  //UiRefreshRequired?.Invoke(this, null);

               }
            }

         }
      }

      private void PickStartingSave_Click(object? sender, EventArgs e)
      {

         if (sender is not null and Button d)
         {
            //var pref = d;
            PrefsDialog prefsDialog = (PrefsDialog)d.FindParent("PrefsDialog");
            if (Prefs.FindPref(d.ID) is not null and Pref pref && pref.prefType == PrefType.file)
            {
               OpenFileDialog fileDialog = new()
               {
                  Directory = Prefs.MainForm.savesFolder
               };
               fileDialog.Filters.Add(Prefs.MainForm.FileFormat);
               fileDialog.FileName = $"{pref.value}";
               if (fileDialog.ShowDialog(this) == DialogResult.Ok)
               {
                  var textbox = (TextBox)prefsDialog.FindChild($"{d.ID}-textbox");
                  pref.value = fileDialog.FileName;
                  textbox.Text = Path.GetFileName(fileDialog.FileName);
               }

               //pref.value = Enum.GetValues(typeof(AlwaysNeverPrompt)).GetValue(d.SelectedIndex);
            }
         }
      }

      internal static DropDown CreateDropDown(string id, Array options, string defaultValue)
      {
         DropDown dropDownList = new()
         {
            ID = id
         };
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

      //private static void DropDownList_SelectedKeyChanged(object? sender, EventArgs e)
      //{
      //   if (sender is not null and DropDown d)
      //   {
      //      //var pref = d;
      //      PrefsDialog prefsDialog = (PrefsDialog)d.FindParent("PrefsDialog");
      //      if (prefsDialog.FindPref(d.ID) is not null and Pref pref)
      //      {
      //         switch (pref.prefType)
      //         {
      //            case PrefType.alwaysneverprompt:
      //               {
      //                  //AlwaysNeverPrompt
      //                  pref.value = Enum.GetValues(typeof(AlwaysNeverPrompt)).GetValue(d.SelectedIndex);
      //                  break;
      //               }
      //            case PrefType.startupbehavior:
      //               {
      //                  pref.value = Enum.GetValues(typeof(StartupBehavior)).GetValue(d.SelectedIndex);
      //                  break;
      //               }
      //         }
      //      }
      //   }
      //}

      //private static void CheckBox_CheckedChanged(object? sender, EventArgs e)
      //{
      //   if (sender is not null and CheckBox c)
      //   {
      //      PrefsDialog prefsDialog = (PrefsDialog)c.FindParent("PrefsDialog");
      //      if (prefsDialog.FindPref(c.ID) is not null and Pref pref)
      //      {
      //         if (c.Checked == true)
      //         {
      //            pref.value = yesno.yes;
      //         }
      //         else
      //         {
      //            pref.value = yesno.no;
      //         }

      //      }
      //   }
      //}

      private void OK_clicked(object? sender, EventArgs e)
      {
         if (sender is not null and Button b)
         {
            PrefsDialog prefsDialog = (PrefsDialog)b.FindParent("PrefsDialog");
            foreach (var control in prefsDialog.Children)
            {
               if (Prefs.FindPref(control.ID) is not null and Pref pref)
               {
                  object? value = null;
                  switch (pref.prefType)
                  {
                     case PrefType.alwaysneverprompt:
                     case PrefType.startupbehavior:
                        {
                           //value = Enum.GetValues(typeof(StartupBehavior)).GetValue(((DropDown)control).SelectedIndex);
                           value = ((DropDown)control).SelectedIndex;
                           break;
                        }
                     case PrefType.checkbox:
                        {
                           if (control is not null and CheckBox c)
                           {
                              value = (bool)c.Checked! ? 1 : 0;
                           }
                           break;
                        }
                  }
                  if (value is not null)
                  {
                     pref.SetValue(value);
                  }

               }
            }
         }

         Close();
      }
      private void Cancel_clicked(object? sender, EventArgs e)
      {
         Close();
      }
   }

   public class Pref
   {
      public string name = string.Empty;
      public object? value = null;
      public PrefType prefType = PrefType.freetext;
      public string defaultValue = string.Empty;
      public bool uiRefresh = false;

      public Pref()
      {

      }
      public Pref(string name)
      {
         this.name = name;
      }
      public Pref(string name, object value) : this(name)
      {
         this.value = value;
      }
      public Pref(string name, object value, PrefType prefType) : this(name, value)
      {
         this.prefType = prefType;
      }
      public Pref(string name, object value, PrefType prefType, bool uiRefresh) : this(name, value, prefType)
      {
         this.uiRefresh = uiRefresh;
      }
      internal void SetValue(object value)
      {
         switch (prefType)
         {
            case PrefType.alwaysneverprompt:
               {
                  this.value = (AlwaysNeverPrompt)Convert.ToInt32(value);
                  break;
               }
            case PrefType.startupbehavior:
               {
                  this.value = (StartupBehavior)Convert.ToInt32(value);
                  break;
               }
            case PrefType.file:
               {
                  this.value = (string)value;
                  break;
               }
            case PrefType.checkbox:
            case PrefType.yesno:
               {
                  this.value = (yesno)Convert.ToInt32(value);
                  break;
               }
         }
      }
   }
}

