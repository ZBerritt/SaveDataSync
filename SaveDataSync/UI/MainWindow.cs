﻿using SaveDataSync.UI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;



namespace SaveDataSync
{
    /// <summary>
    /// Represents the main GUI window used in the app
    /// </summary>
    public partial class MainWindow : Form
    {

        private SaveDataSyncEngine engine;
        public MainWindow()
        {
            InitializeComponent();
        }

        // Loading Events
        private void OnLoad(object sender, EventArgs e)
        {
            // Grabs the engine which allows communication with the backend 
            engine = SaveDataSyncEngine.CreateInstance();

            // Auto sizes the last column of the save list
            saveFileList.Columns[saveFileList.Columns.Count - 1].Width = -2;

            // Loads the save list with the imported data from the engine
            ReloadUI();
        }

        //Used to reload all UI data
        public void ReloadUI()
        {
            /* Reload the save file list */
            saveFileList.Items.Clear();
            var saves = engine.GetLocalSaveList().GetSaves();
            foreach (var save in saves)
            {
                ListViewItem saveItem = new ListViewItem(save.Key);
                saveItem.SubItems.Add(save.Value);

                // Get file size
                try
                {
                    FileAttributes attr = File.GetAttributes(save.Value);
                    long saveSize = attr.HasFlag(FileAttributes.Directory)
                        ? new DirectoryInfo(save.Value).EnumerateFiles("*.*", SearchOption.AllDirectories).Sum(fi => fi.Length)
                        : new FileInfo(save.Value).Length; // The size of the file/folder in bytes
                    string[] sizes = { "Bytes", "kB", "MB", "GB", "TB" };
                    int order = 0;
                    while (saveSize >= 1024 && order < sizes.Length - 1)
                    {
                        order++;
                        saveSize = saveSize / 1024;
                    }
                    saveItem.SubItems.Add(string.Format("{0:0.##} {1}", saveSize, sizes[order]));
                } catch (Exception)
                {
                    saveItem.SubItems.Add("N/A");
                }

                // Get file sync status
                saveItem.SubItems.Add("Not implemented");

                // Add to the table
                saveFileList.Items.Add(saveItem);
            }

            /* Check server status */
            var server = engine.GetServer();
            string serverType = "None";
            string status = "N/A";
            Color statusColor = Color.Black;
            string serverHost = "N/A";
            if (server != null)
            {
                serverType = server.Name();
                serverHost = server.Host();
                try
                {   
                    var serverOnline = server.ServerOnline();
                    status = serverOnline ? "Online" : "Offline";
                    statusColor = serverOnline ? Color.Green : Color.Gold;
                }
                catch (Exception)
                {
                    status = "Error";
                    statusColor = Color.Red;
                }
            }

            // Set the text of the server information
            type.Text = serverType;
            host.Text = serverHost;
            serverStatus.Text = status;
            serverStatus.ForeColor = statusColor;
        }

        // Click Events
        private void NewSaveFile_Click(object sender, EventArgs e)
        {
            SaveFileWindow sfw = new SaveFileWindow(engine)
            {
                Owner = this,
                ShowInTaskbar = false
            };
            sfw.ShowDialog();
            ReloadUI();
        }

        private void Settings_Click(object sender, EventArgs e)
        {
            SettingsWindow sw = new SettingsWindow(engine)
            {
                Owner = this,
                ShowInTaskbar = true
            };
            sw.ShowDialog();
            ReloadUI();
        }


        private void serverSettingsBtn_Click(object sender, EventArgs e)
        {
            ServerSettings ss = new ServerSettings(engine)
            {
                Owner = this,
                ShowInTaskbar = true
            };
            ss.ShowDialog();
            ReloadUI();
        }

        private void Export_Click(object sender, EventArgs e)
        {
            Console.WriteLine("Export");
            engine.ExportSaveData();
        }

        private void Import_Click(object sender, EventArgs e)
        {
            Console.WriteLine("Import");
            engine.ImportSaveData();
        }

        private void SaveFileList_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                var selectedItem = saveFileList.FocusedItem;
                if (selectedItem == null) return;
                SaveFileContextMenu(selectedItem.Text).Show(saveFileList, new Point(e.X, e.Y));

            }
        }

        private ContextMenuStrip SaveFileContextMenu(string name)
        {
            var menu = new ContextMenuStrip();
            var goToLocation = menu.Items.Add("Open File Location");
            goToLocation.Click += (object sender2, EventArgs e2) =>
            {
                string savePath = engine.GetLocalSaveList().GetSavePath(name);
                Process.Start("explorer.exe", string.Format("/select, \"{0}\"", savePath));
            };

            var quickExport = menu.Items.Add("Quick Export");
            quickExport.Click += (object sender3, EventArgs e3) =>
            {
                var savesToExport = GetSelectedSaves();

                savesToExport.ForEach(i => Console.WriteLine("Export: {0}", i));
            };

            var quickImport = menu.Items.Add("Quick Import");
            quickImport.Click += (object sender4, EventArgs e4) =>
            {
                var savesToImport = GetSelectedSaves();

                savesToImport.ForEach(i => Console.WriteLine("Import: {0}", i));
            };

            var removeSave = menu.Items.Add("Remove Save");
            removeSave.Click += (object sender5, EventArgs e5) =>
            {
                var confirm = MessageBox.Show("Are you sure you want to remove this save file?",
                    "Confirm",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);
                if (confirm == DialogResult.Yes)
                {
                    engine.GetLocalSaveList().RemoveSave(name);
                    engine.Save();
                    ReloadUI();

                }
            };
            return menu;
        }

        private void label2_Click(object sender, EventArgs e)
        {
            try
            {
                var url = "http://" + host.Text;
                Process.Start(url);
            } catch (Exception) { }
        }


        private List<string> GetSelectedSaves()
        {
            var selected = saveFileList.SelectedItems;
            List<string> saves = new List<string>();
            foreach (ListViewItem item in selected)
            {
                saves.Add(item.SubItems[0].Text); // The first sub item will always be the name
            }

            return saves;
        }
    }
}