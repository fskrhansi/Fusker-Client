/*  This file is part of Fusker Client.
**
**  Fusker Client is free software: you can redistribute it and/or modify
**  it under the terms of the GNU Affero General Public License as published by
**  the Free Software Foundation, either version 3 of the License, or
**  (at your option) any later version.
**
**  Fusker Client is distributed in the hope that it will be useful,
**  but WITHOUT ANY WARRANTY; without even the implied warranty of
**  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
**  GNU Affero General Public License for more details.
**
**  You should have received a copy of the GNU Affero General Public License
**  along with Fusker Client.  If not, see <http://www.gnu.org/licenses/>.
*/

namespace FuskerClient
{
    using System;
    using System.ComponentModel;
    using System.Data.Common;
    using System.Data.SQLite;
    using System.Diagnostics;
    using System.Drawing;
    using System.IO;
    using System.Net;
    using System.Reflection;
    using System.Threading;
    using System.Windows.Forms;
    using System.Xml;

    public class MainForm : Form
    {
        private ToolStripMenuItem aboutToolStripMenuItem;
        private string ApplicationData;
        private System.Drawing.Color backColorSv;
        private bool blnBackupZoom = false;
        private bool blnNextFusker = false;
        private bool blnNextLid = false;
        private bool blnPaused = false;
        private IContainer components;
        private ContextMenu contextMenu1;
        private ToolStripMenuItem editToolStripMenuItem;
        private ToolStripMenuItem exitToolStripMenuItem;
        private ToolStripMenuItem fileToolStripMenuItem;
        private System.Drawing.Color foreColorSv;
        public string FullName = "Unknown";
        private bool screensaver = true;
        private ToolStripMenuItem fullScreenToolStripMenuItem;
        private ToolStripMenuItem helpToolStripMenuItem;
        private ToolStripMenuItem ignorePhotobucketToolStripMenuItem;
        private Image image = null;
        private Stream MainStream;
        private MenuItem menuItem1;
        private MenuItem menuItem2;
        private MenuStrip menuStrip1;
        private ToolStripMenuItem nextFuskerToolStripMenuItem;
        private ToolStripMenuItem nextLidToolStripMenuItem;
        private NotifyIcon notifyIcon1;
        private Panel panel1;
        private ToolStripMenuItem pauseToolStripMenuItem;
        private PictureBox mainPicture;
        public string ProxyDomain = "";
        public string ProxyPassword = "";
        public string ProxyServer = "";
        public int ProxyType = 0;
        public string ProxyUserid = "";
        private ToolStripMenuItem runToolStripMenuItem;
        private ToolStripMenuItem settingsToolStripMenuItem;
        private StatusBar statusBar1;
        private ToolStripMenuItem stopToolStripMenuItem;
        private Thread thread;
        private ToolStripSeparator toolStripSeparator2;
        private ToolStripSeparator toolStripSeparator3;
        private ToolStripSeparator toolStripSeparator4;
        private ToolStripSeparator toolStripSeparator5;
        private ToolStripMenuItem topmostToolStripMenuItem;
        private ToolStripMenuItem updateToolStripMenuItem;
        public string Version = "0.0";
        private ToolStripMenuItem viewToolStripMenuItem;
        private ToolStripMenuItem disableScreensaverToolStripMenuItem;
        private ToolStripMenuItem zoomToolStripMenuItem;
        private ToolStripMenuItem databaseToolStripMenuItem;
        private ToolStripMenuItem vacuumToolStripMenuItem;
        private ToolStripMenuItem clearToolStripMenuItem;
        private ToolStripMenuItem cacheToolStripMenuItem1;
        private ToolStripSeparator toolStripMenuItem1;
        private ToolStripMenuItem cacheShowToolStripMenuItem;
        private ToolStripMenuItem emptyToolStripMenuItem;
        private SQLiteConnection dbcon;

        //disables screensaver while fullscreen
        protected override void WndProc(ref Message m)
        {
            const int SC_SCREENSAVE = 0xF140;
            const int SC_MONITORPOWER = 0xF170;
            const int WM_SYSCOMMAND = 0x0112;

            if (!screensaver || this.disableScreensaverToolStripMenuItem.Checked)
            {
                if ((m.WParam == (IntPtr)SC_SCREENSAVE) && (m.Msg == WM_SYSCOMMAND))
                    m.Result = (IntPtr)(-1); // prevent screensaver
                else if ((m.WParam == (IntPtr)SC_MONITORPOWER) && (m.Msg == WM_SYSCOMMAND))
                    m.Result = (IntPtr)(-1); // prevent monitor power-off
                else
                    base.WndProc(ref m);
            }
            else
                base.WndProc(ref m);
        }

        public MainForm()
        {
            this.InitializeComponent();
            Assembly executingAssembly = Assembly.GetExecutingAssembly();
            this.FullName = executingAssembly.FullName.Split(new char[] { ',' })[0];
            this.Version = executingAssembly.FullName.Split(new char[] { ',' })[1].Split(new char[] { '=' })[1];
            this.ApplicationData = Application.StartupPath + System.IO.Path.DirectorySeparatorChar + @"Pictures";
            Directory.CreateDirectory(this.ApplicationData);
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new About(this).ShowDialog(this);
        }

        private void AddDoneFile(string strBase, string strUrl, int intNew)
        {
            // Insert into the database
            using (SQLiteTransaction mytransaction = dbcon.BeginTransaction())
            {
                using (SQLiteCommand dbcmd = dbcon.CreateCommand())
                {
                    // Create a parameterized insert command
                    dbcmd.CommandText = "INSERT INTO Fuskers (BASE,URL,NEW) VALUES(@base,@url,@new)";
                    dbcmd.Parameters.AddWithValue("base", strBase);
                    dbcmd.Parameters.AddWithValue("url", strUrl);
                    dbcmd.Parameters.AddWithValue("new", intNew);
                    dbcmd.ExecuteNonQuery();
                    mytransaction.Commit();
                }
            }


        }

        private bool AllReadyDone(string strBase, string strUrl)
        {
            using (SQLiteCommand dbcmd = dbcon.CreateCommand())
            {
                dbcmd.CommandText = "SELECT BASE, URL FROM Fuskers WHERE BASE LIKE @base AND URL like @url";
                dbcmd.Parameters.AddWithValue("base", strBase);
                dbcmd.Parameters.AddWithValue("url", strUrl);
                using (DbDataReader reader = dbcmd.ExecuteReader())
                {
                    return reader.HasRows;
                }
            }
        }

        private void cacheToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && (this.components != null))
            {
                this.components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void DoRun()
        {
            this.backColorSv = this.BackColor;
            this.foreColorSv = this.ForeColor;
            this.BackColor = System.Drawing.Color.Black;
            this.ForeColor = System.Drawing.Color.White;
            this.mainPicture.Visible = true;
            this.thread = new Thread(new ThreadStart(this.RunIt));
            this.thread.Name = "RunIt";
            this.thread.Start();
            this.ShowStatus("running...");
        }

        private void DoStop()
        {
            if ((this.thread != null) && this.thread.IsAlive)
            {
                this.thread.Abort();
            }
            base.WindowState = FormWindowState.Normal;
            this.BackColor = this.backColorSv;
            this.ForeColor = this.foreColorSv;
            this.mainPicture.Visible = false;
            this.ShowStatus("stopped");
        }

        private void DrawPicture()
        {
            if (this.mainPicture.InvokeRequired)
            {
                this.mainPicture.Invoke(new DrawPictureDelegate(this.DrawPicture));
            }
            else
            {
                try
                {
                    int num;
                    int num2;
                    int num3;
                    int num4;
                    if (this.image == null)
                    {
                        return;
                    }
                    int width = this.panel1.Width;
                    int height = this.panel1.Height;
                    if (!(((this.image.Width >= width) || (this.image.Height >= height)) || this.zoomToolStripMenuItem.Checked))
                    {
                        num3 = this.image.Width;
                        num4 = this.image.Height;
                        num = Math.Max(0, (width - num3) / 2);
                        num2 = Math.Max(0, (height - num4) / 2);
                    }
                    else
                    {
                        double num7 = ((double)width) / ((double)this.image.Width);
                        double num8 = ((double)height) / ((double)this.image.Height);
                        num3 = (int)Math.Min((double)(this.image.Width * num7), (double)(this.image.Width * num8));
                        num4 = (int)Math.Min((double)(this.image.Height * num7), (double)(this.image.Height * num8));
                    }
                    num3 = Math.Max(1, num3);
                    num4 = Math.Max(1, num4);
                    num = Math.Max(0, (width - num3) / 2);
                    num2 = Math.Max(0, (height - num4) / 2);
                    this.mainPicture.Image = new Bitmap(this.image, num3, num4);
                    this.mainPicture.Width = num3;
                    this.mainPicture.Height = num4;
                    this.mainPicture.Left = num;
                    this.mainPicture.Top = num2;
                }
                catch (Exception exception)
                {
                    Debug.WriteLine("DrawPicture: " + exception.Message);
                }
                //GC.Collect();
            }
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            base.Close();
        }

        private void fullScreenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.ShowFullScreen();
        }
		private void storeURL(string strUrl, string strBase, string strLinks)
		{
			// Insert into the database
            using (SQLiteTransaction mytransaction = dbcon.BeginTransaction())
            {
                using (SQLiteCommand dbcmd = dbcon.CreateCommand())
                {
                    // Create a parameterized insert command
                    dbcmd.CommandText = "INSERT OR REPLACE INTO Sites (BASE,URL,LINKS) VALUES(@base,@url,@links)";
                    dbcmd.Parameters.AddWithValue("base", strBase);
                    dbcmd.Parameters.AddWithValue("url", strUrl);
                    dbcmd.Parameters.AddWithValue("links", strLinks);
                    dbcmd.ExecuteNonQuery();
                    mytransaction.Commit();
                }
            }
		}
        private void Fusker2Lids(string strUrl, string strBase, string strLinks)
        {
            int num = 0;
            int intNew = 0;
            Stream stream = this.Url(strUrl);
            if (stream != null)
            {
                StreamReader reader = new StreamReader(stream);
                string str2 = reader.ReadToEnd().ToLower();
                reader.Close();
                while (true)
                {
                    if (this.blnNextLid)
                    {
                        this.blnNextLid = false;
                    }
                    if (this.blnNextFusker)
                    {
                        this.blnNextFusker = false;
                        return;
                    }
                    int index = str2.IndexOf(strLinks);
                    if (index < 0)
                    {
                        return;
                    }
                    str2 = str2.Substring(index);
                    int length = str2.IndexOf("\"");
                    if (length < 0)
                    {
                        return;
                    }
                    string str = str2.Substring(0, length);
                    int num5 = str.IndexOf("&");
                    if (num5 > 0)
                    {
                        str = str.Substring(0, num5);
                    }
                    if (str != "")
                    {
                        intNew = 0;
                        Debug.WriteLine("Lid: " + strBase + str);
                        this.ShowStatus("Lid: " + strBase + str);
                        if (this.AllReadyDone(strBase, str))
                        {
                            num++;
                            if (num > 5)
                            {
                                return;
                            }
                        }
                        else
                        {
                            num = 0;
                            intNew = this.GetImages(strBase, str);
                            this.AddDoneFile(strBase, str, intNew);
                        }
                        Debug.WriteLine("Lid: " + intNew + " pictures");
                    }
                    str2 = str2.Substring(length + 1);
                }
            }
        }

        private int GetImages(string strBase, string strUrl)
        {
            long num = 0L;
            int num3 = 0;
            int num4 = 0;
            Stream stream = this.Url(strBase + strUrl);
            if (stream == null)
            {
                return 0;
            }
            StreamReader reader = new StreamReader(stream);
            string str2 = reader.ReadToEnd().ToLower();
            reader.Close();
            while (num4 < 5)
            {
                if (this.blnNextLid || this.blnNextFusker)
                {
                    return num3;
                }
                if (this.blnPaused)
                {
                    Thread.Sleep(0x3e8);
                }
                else
                {
                    int index = str2.IndexOf("<img src=");
                    if (index < 0)
                    {
                        return num3;
                    }
                    str2 = str2.Substring(index + 10);
                    int length = str2.IndexOf("\"");
                    if (length < 0)
                    {
                        return num3;
                    }
                    string strSrc = str2.Substring(0, length);
                    str2 = str2.Substring(length + 1);
                    if ((!this.ignorePhotobucketToolStripMenuItem.Checked || (strSrc.ToLower().IndexOf("photobucket") < 0)) && ((strSrc.IndexOf("http://") >= 0) && (strSrc.IndexOf(strBase) < 0)))
                    {
                        long thePicture = this.GetThePicture(strSrc);
                        if ((thePicture == 0L) || (thePicture == num))
                        {
                            num4++;
                        }
                        else
                        {
                            num4 = 0;
                            if (thePicture != num)
                            {
                                num3++;
                            }
                        }
                        num = thePicture;
                    }
                }
            }
            return num3;
        }

        private void GetProxySettingsFromBrowser()
        {
            try
            {
                Microsoft.Win32.RegistryKey key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Internet Settings");
                int num = (int)key.GetValue("ProxyEnable");
                if (num > 0)
                {
                    this.ProxyType = 1;
                    this.ProxyServer = "http://" + key.GetValue("ProxyServer");
                }
                key.Close();
            }
            catch
            {
                this.ProxyType = 0;
            }
        }

        private void GetTheImage()
        {
            try
            {
                this.image = Image.FromStream(this.MainStream);
            }
            catch
            {
            }
        }

        private long GetThePicture(string strSrc)
        {
            int startIndex = strSrc.LastIndexOf("http:/");
            string path = strSrc.Substring(startIndex);
            path = strSrc.Replace("http:/", this.ApplicationData).Replace("../", "").Replace("/", @"\");
            if (System.IO.File.Exists(path))
            {
                Debug.WriteLine("Old: " + strSrc);
            }
            else
            {
                long ticks = DateTime.Now.Ticks;
                Stream stream = this.Url(strSrc);
                if (stream == null)
                {
                    return 0L;
                }
                long num4 = (DateTime.Now.Ticks - ticks) / 0x2710L;
                try
                {
                    this.MainStream = stream;
                    Thread thread = new Thread(new ThreadStart(this.GetTheImage));
                    thread.Name = "GetTheImage thread";
                    thread.Start();
                    if (thread.Join(new TimeSpan(0, 0, 10)))
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(path));
                        this.image.Save(path);
                        this.DrawPicture();
                        if (num4 < 0x3e8L)
                        {
                            Thread.Sleep((int)(0x3e8 - ((int)num4)));
                        }
                    }
                    else
                    {
                        thread.Abort();
                        Debug.WriteLine("GetTheImage aborted on " + strSrc);
                        return 0L;
                    }
                }
                catch (Exception exception)
                {
                    Debug.WriteLine("GetThePicture: " + exception.Message);
                    return 0L;
                }
            }
            FileInfo info = new FileInfo(path);
            return info.Length;
        }



        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.notifyIcon1 = new System.Windows.Forms.NotifyIcon(this.components);
            this.contextMenu1 = new System.Windows.Forms.ContextMenu();
            this.menuItem1 = new System.Windows.Forms.MenuItem();
            this.menuItem2 = new System.Windows.Forms.MenuItem();
            this.statusBar1 = new System.Windows.Forms.StatusBar();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.runToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator3 = new System.Windows.Forms.ToolStripSeparator();
            this.pauseToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator4 = new System.Windows.Forms.ToolStripSeparator();
            this.stopToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator5 = new System.Windows.Forms.ToolStripSeparator();
            this.databaseToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.vacuumToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.clearToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.cacheToolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.cacheShowToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.emptyToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem1 = new System.Windows.Forms.ToolStripSeparator();
            this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.editToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.nextLidToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.nextFuskerToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.ignorePhotobucketToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.disableScreensaverToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.settingsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.viewToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.zoomToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.topmostToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.fullScreenToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.helpToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.updateToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.aboutToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.panel1 = new System.Windows.Forms.Panel();
            this.mainPicture = new System.Windows.Forms.PictureBox();
            this.menuStrip1.SuspendLayout();
            this.panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.mainPicture)).BeginInit();
            this.SuspendLayout();
            // 
            // notifyIcon1
            // 
            this.notifyIcon1.ContextMenu = this.contextMenu1;
            this.notifyIcon1.Text = "FuskerClient";
            this.notifyIcon1.DoubleClick += new System.EventHandler(this.notifyIcon1_DoubleClick);
            // 
            // contextMenu1
            // 
            this.contextMenu1.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
            this.menuItem1,
            this.menuItem2});
            // 
            // menuItem1
            // 
            this.menuItem1.Index = 0;
            this.menuItem1.Text = "Restore";
            this.menuItem1.Click += new System.EventHandler(this.menuItem1_Click);
            // 
            // menuItem2
            // 
            this.menuItem2.Index = 1;
            this.menuItem2.Text = "Close";
            this.menuItem2.Click += new System.EventHandler(this.menuItem2_Click);
            // 
            // statusBar1
            // 
            this.statusBar1.Location = new System.Drawing.Point(0, 192);
            this.statusBar1.Name = "statusBar1";
            this.statusBar1.Size = new System.Drawing.Size(288, 22);
            this.statusBar1.TabIndex = 0;
            this.statusBar1.Text = "ok";
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.editToolStripMenuItem,
            this.viewToolStripMenuItem,
            this.helpToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(288, 24);
            this.menuStrip1.TabIndex = 1;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.runToolStripMenuItem,
            this.toolStripSeparator3,
            this.pauseToolStripMenuItem,
            this.toolStripSeparator4,
            this.stopToolStripMenuItem,
            this.toolStripSeparator5,
            this.databaseToolStripMenuItem,
            this.cacheToolStripMenuItem1,
            this.toolStripMenuItem1,
            this.exitToolStripMenuItem});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
            this.fileToolStripMenuItem.Text = "File";
            // 
            // runToolStripMenuItem
            // 
            this.runToolStripMenuItem.Name = "runToolStripMenuItem";
            this.runToolStripMenuItem.Size = new System.Drawing.Size(122, 22);
            this.runToolStripMenuItem.Text = "Run";
            this.runToolStripMenuItem.Click += new System.EventHandler(this.runToolStripMenuItem_Click);
            // 
            // toolStripSeparator3
            // 
            this.toolStripSeparator3.Name = "toolStripSeparator3";
            this.toolStripSeparator3.Size = new System.Drawing.Size(119, 6);
            // 
            // pauseToolStripMenuItem
            // 
            this.pauseToolStripMenuItem.Name = "pauseToolStripMenuItem";
            this.pauseToolStripMenuItem.Size = new System.Drawing.Size(122, 22);
            this.pauseToolStripMenuItem.Text = "Pause";
            this.pauseToolStripMenuItem.Click += new System.EventHandler(this.pauseToolStripMenuItem_Click);
            // 
            // toolStripSeparator4
            // 
            this.toolStripSeparator4.Name = "toolStripSeparator4";
            this.toolStripSeparator4.Size = new System.Drawing.Size(119, 6);
            // 
            // stopToolStripMenuItem
            // 
            this.stopToolStripMenuItem.Name = "stopToolStripMenuItem";
            this.stopToolStripMenuItem.Size = new System.Drawing.Size(122, 22);
            this.stopToolStripMenuItem.Text = "Stop";
            this.stopToolStripMenuItem.Click += new System.EventHandler(this.stopToolStripMenuItem_Click);
            // 
            // toolStripSeparator5
            // 
            this.toolStripSeparator5.Name = "toolStripSeparator5";
            this.toolStripSeparator5.Size = new System.Drawing.Size(119, 6);
            // 
            // databaseToolStripMenuItem
            // 
            this.databaseToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.vacuumToolStripMenuItem,
            this.clearToolStripMenuItem});
            this.databaseToolStripMenuItem.Name = "databaseToolStripMenuItem";
            this.databaseToolStripMenuItem.Size = new System.Drawing.Size(122, 22);
            this.databaseToolStripMenuItem.Text = "Database";
            // 
            // vacuumToolStripMenuItem
            // 
            this.vacuumToolStripMenuItem.Name = "vacuumToolStripMenuItem";
            this.vacuumToolStripMenuItem.Size = new System.Drawing.Size(118, 22);
            this.vacuumToolStripMenuItem.Text = "Vacuum";
            this.vacuumToolStripMenuItem.Click += new System.EventHandler(this.vacuumToolStripMenuItem_Click);
            // 
            // clearToolStripMenuItem
            // 
            this.clearToolStripMenuItem.Name = "clearToolStripMenuItem";
            this.clearToolStripMenuItem.Size = new System.Drawing.Size(118, 22);
            this.clearToolStripMenuItem.Text = "Clear";
            this.clearToolStripMenuItem.Click += new System.EventHandler(this.clearToolStripMenuItem_Click);
            // 
            // cacheToolStripMenuItem1
            // 
            this.cacheToolStripMenuItem1.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.cacheShowToolStripMenuItem,
            this.emptyToolStripMenuItem});
            this.cacheToolStripMenuItem1.Name = "cacheToolStripMenuItem1";
            this.cacheToolStripMenuItem1.Size = new System.Drawing.Size(122, 22);
            this.cacheToolStripMenuItem1.Text = "Cache";
            // 
            // cacheShowToolStripMenuItem
            // 
            this.cacheShowToolStripMenuItem.Name = "cacheShowToolStripMenuItem";
            this.cacheShowToolStripMenuItem.Size = new System.Drawing.Size(108, 22);
            this.cacheShowToolStripMenuItem.Text = "Show";
            this.cacheShowToolStripMenuItem.Click += new System.EventHandler(this.cacheShowToolStripMenuItem_Click);
            // 
            // emptyToolStripMenuItem
            // 
            this.emptyToolStripMenuItem.Name = "emptyToolStripMenuItem";
            this.emptyToolStripMenuItem.Size = new System.Drawing.Size(108, 22);
            this.emptyToolStripMenuItem.Text = "Empty";
            this.emptyToolStripMenuItem.Click += new System.EventHandler(this.emptyToolStripMenuItem_Click);
            // 
            // toolStripMenuItem1
            // 
            this.toolStripMenuItem1.Name = "toolStripMenuItem1";
            this.toolStripMenuItem1.Size = new System.Drawing.Size(119, 6);
            // 
            // exitToolStripMenuItem
            // 
            this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            this.exitToolStripMenuItem.Size = new System.Drawing.Size(122, 22);
            this.exitToolStripMenuItem.Text = "Exit";
            this.exitToolStripMenuItem.Click += new System.EventHandler(this.exitToolStripMenuItem_Click);
            // 
            // editToolStripMenuItem
            // 
            this.editToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.nextLidToolStripMenuItem,
            this.nextFuskerToolStripMenuItem,
            this.ignorePhotobucketToolStripMenuItem,
            this.disableScreensaverToolStripMenuItem,
            this.toolStripSeparator2,
            this.settingsToolStripMenuItem});
            this.editToolStripMenuItem.Name = "editToolStripMenuItem";
            this.editToolStripMenuItem.Size = new System.Drawing.Size(39, 20);
            this.editToolStripMenuItem.Text = "Edit";
            // 
            // nextLidToolStripMenuItem
            // 
            this.nextLidToolStripMenuItem.Name = "nextLidToolStripMenuItem";
            this.nextLidToolStripMenuItem.Size = new System.Drawing.Size(188, 22);
            this.nextLidToolStripMenuItem.Text = "Next lid            (right)";
            this.nextLidToolStripMenuItem.Click += new System.EventHandler(this.nextLidToolStripMenuItem_Click);
            // 
            // nextFuskerToolStripMenuItem
            // 
            this.nextFuskerToolStripMenuItem.Name = "nextFuskerToolStripMenuItem";
            this.nextFuskerToolStripMenuItem.Size = new System.Drawing.Size(188, 22);
            this.nextFuskerToolStripMenuItem.Text = "Next Fusker     (down)";
            this.nextFuskerToolStripMenuItem.Click += new System.EventHandler(this.nextFuskerToolStripMenuItem_Click);
            // 
            // ignorePhotobucketToolStripMenuItem
            // 
            this.ignorePhotobucketToolStripMenuItem.Checked = true;
            this.ignorePhotobucketToolStripMenuItem.CheckOnClick = true;
            this.ignorePhotobucketToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            this.ignorePhotobucketToolStripMenuItem.Name = "ignorePhotobucketToolStripMenuItem";
            this.ignorePhotobucketToolStripMenuItem.Size = new System.Drawing.Size(188, 22);
            this.ignorePhotobucketToolStripMenuItem.Text = "Ignore photobucket";
            this.ignorePhotobucketToolStripMenuItem.Click += new System.EventHandler(this.ignorePhotobucketToolStripMenuItem_Click);
            // 
            // disableScreensaverToolStripMenuItem
            // 
            this.disableScreensaverToolStripMenuItem.CheckOnClick = true;
            this.disableScreensaverToolStripMenuItem.Name = "disableScreensaverToolStripMenuItem";
            this.disableScreensaverToolStripMenuItem.Size = new System.Drawing.Size(188, 22);
            this.disableScreensaverToolStripMenuItem.Text = "Disable Screensaver";
            // 
            // toolStripSeparator2
            // 
            this.toolStripSeparator2.Name = "toolStripSeparator2";
            this.toolStripSeparator2.Size = new System.Drawing.Size(185, 6);
            // 
            // settingsToolStripMenuItem
            // 
            this.settingsToolStripMenuItem.Name = "settingsToolStripMenuItem";
            this.settingsToolStripMenuItem.Size = new System.Drawing.Size(188, 22);
            this.settingsToolStripMenuItem.Text = "Settings...";
            this.settingsToolStripMenuItem.Click += new System.EventHandler(this.settingsToolStripMenuItem_Click);
            // 
            // viewToolStripMenuItem
            // 
            this.viewToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.zoomToolStripMenuItem,
            this.topmostToolStripMenuItem,
            this.fullScreenToolStripMenuItem});
            this.viewToolStripMenuItem.Name = "viewToolStripMenuItem";
            this.viewToolStripMenuItem.Size = new System.Drawing.Size(44, 20);
            this.viewToolStripMenuItem.Text = "View";
            // 
            // zoomToolStripMenuItem
            // 
            this.zoomToolStripMenuItem.Name = "zoomToolStripMenuItem";
            this.zoomToolStripMenuItem.Size = new System.Drawing.Size(131, 22);
            this.zoomToolStripMenuItem.Text = "Zoom";
            this.zoomToolStripMenuItem.Click += new System.EventHandler(this.zoomToolStripMenuItem_Click);
            // 
            // topmostToolStripMenuItem
            // 
            this.topmostToolStripMenuItem.Name = "topmostToolStripMenuItem";
            this.topmostToolStripMenuItem.Size = new System.Drawing.Size(131, 22);
            this.topmostToolStripMenuItem.Text = "Topmost";
            this.topmostToolStripMenuItem.Click += new System.EventHandler(this.topmostToolStripMenuItem_Click);
            // 
            // fullScreenToolStripMenuItem
            // 
            this.fullScreenToolStripMenuItem.Name = "fullScreenToolStripMenuItem";
            this.fullScreenToolStripMenuItem.Size = new System.Drawing.Size(131, 22);
            this.fullScreenToolStripMenuItem.Text = "Full Screen";
            this.fullScreenToolStripMenuItem.Click += new System.EventHandler(this.fullScreenToolStripMenuItem_Click);
            // 
            // helpToolStripMenuItem
            // 
            this.helpToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.updateToolStripMenuItem,
            this.aboutToolStripMenuItem});
            this.helpToolStripMenuItem.Name = "helpToolStripMenuItem";
            this.helpToolStripMenuItem.Size = new System.Drawing.Size(44, 20);
            this.helpToolStripMenuItem.Text = "Help";
            // 
            // updateToolStripMenuItem
            // 
            this.updateToolStripMenuItem.Name = "updateToolStripMenuItem";
            this.updateToolStripMenuItem.Size = new System.Drawing.Size(121, 22);
            this.updateToolStripMenuItem.Text = "Update...";
            this.updateToolStripMenuItem.Click += new System.EventHandler(this.updateToolStripMenuItem_Click);
            // 
            // aboutToolStripMenuItem
            // 
            this.aboutToolStripMenuItem.Name = "aboutToolStripMenuItem";
            this.aboutToolStripMenuItem.Size = new System.Drawing.Size(121, 22);
            this.aboutToolStripMenuItem.Text = "About...";
            this.aboutToolStripMenuItem.Click += new System.EventHandler(this.aboutToolStripMenuItem_Click);
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.mainPicture);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel1.Location = new System.Drawing.Point(0, 24);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(288, 168);
            this.panel1.TabIndex = 2;
            this.panel1.Paint += new System.Windows.Forms.PaintEventHandler(this.panel1_Paint);
            // 
            // mainPicture
            // 
            this.mainPicture.Location = new System.Drawing.Point(80, 48);
            this.mainPicture.Name = "pictureBox1";
            this.mainPicture.Size = new System.Drawing.Size(100, 50);
            this.mainPicture.TabIndex = 0;
            this.mainPicture.TabStop = false;
            this.mainPicture.DoubleClick += new System.EventHandler(this.pictureBox1_DoubleClick);
            // 
            // MainForm
            // 
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
            this.ClientSize = new System.Drawing.Size(288, 214);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.menuStrip1);
            this.Controls.Add(this.statusBar1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.KeyPreview = true;
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "MainForm";
            this.Text = "FuskerClient 2.0";
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.Closing += new System.ComponentModel.CancelEventHandler(this.MainForm_Closing);
            this.Resize += new System.EventHandler(this.MainForm_Resize);
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.MainForm_KeyDown);
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.panel1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.mainPicture)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        private void OpenDoneDB()
        {

            // Create a connection and a command
            dbcon = new SQLiteConnection("Data Source=" + Application.StartupPath + System.IO.Path.DirectorySeparatorChar + @"config.db3");
            using (SQLiteCommand dbcmd = dbcon.CreateCommand())
            {

                // Open the connection. If the database doesn't exist,
                // it will be created automatically
                dbcon.Open();
                // Create a table in the database
                using (SQLiteTransaction mytransaction = dbcon.BeginTransaction())
                {
                    dbcmd.CommandText = "CREATE TABLE IF NOT EXISTS Settings (Name VARCHAR PRIMARY KEY NOT NULL, Value VARCHAR)";
                    dbcmd.ExecuteNonQuery();
                    dbcmd.CommandText = "CREATE TABLE IF NOT EXISTS Fuskers (ID INTEGER PRIMARY KEY AUTOINCREMENT, BASE VARCHAR, URL VARCHAR, NEW INTEGER, DATE DATETIME)";
                    dbcmd.ExecuteNonQuery();
                    dbcmd.CommandText = "CREATE TRIGGER IF NOT EXISTS \"date\" AFTER INSERT ON Fuskers BEGIN UPDATE Fuskers SET DATE=datetime('now') WHERE ID=new.ID; END;";
                    dbcmd.ExecuteNonQuery();
                    dbcmd.CommandText = "CREATE TABLE IF NOT EXISTS Sites (BASE VARCHAR PRIMARY KEY NOT NULL, URL VARCHAR, LINKS VARCHAR, RATING INTEGER DEFAULT 0 NOT NULL)";
                    dbcmd.ExecuteNonQuery();
                    mytransaction.Commit();
                }
            }

        }



        private void LoadWindows()
        {
            this.Text = this.FullName + " " + this.Version;
            System.Collections.Generic.Dictionary<string, string> settings = new System.Collections.Generic.Dictionary<string, string>()
                    {
                        {"X",""},
                        {"Y",""},
                        {"W",""},
                        {"H",""},
                        {"Zoom",""},
                        {"ProxyType",""},
                        {"ProxyServer",""},
                        {"ProxyUserID",""},
                        {"ProxyPassword",""},
                        {"ProxyDomain",""},
                        {"IgnorePhotobucket",""},
                        {"ScreensaverOff",""}
                    };


            using (SQLiteCommand dbcmd = dbcon.CreateCommand())
            {
                dbcmd.CommandText = "SELECT Value FROM Settings WHERE Name LIKE @name";

                foreach (string setting in new System.Collections.Generic.Dictionary<string,string>(settings).Keys)
                {
                    dbcmd.Parameters.Clear();
                    dbcmd.Parameters.AddWithValue("name", setting);
                    using (DbDataReader reader = dbcmd.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            settings[setting] = reader.GetValue(0).ToString();
                        }
                    }
                }

            }
            try
            {
                if (Screen.PrimaryScreen.Bounds.X < int.Parse(settings["X"]) && Screen.PrimaryScreen.Bounds.Y < int.Parse(settings["Y"]))
                    base.Location = new Point(int.Parse(settings["X"]), int.Parse(settings["Y"]));
                base.Width = int.Parse(settings["W"]);
                base.Height = int.Parse(settings["H"]);
                this.zoomToolStripMenuItem.Checked = settings["Zoom"] == "True";
                this.ProxyType = int.Parse(settings["ProxyType"].ToString());
                this.ProxyServer = settings["ProxyServer"];
                this.ProxyUserid = settings["ProxyUserID"];
                this.ProxyPassword = settings["ProxyPassword"];
                this.ProxyDomain = settings["ProxyDomain"];
                this.ignorePhotobucketToolStripMenuItem.Checked = settings["IgnorePhotobucket"] == "True";
                this.disableScreensaverToolStripMenuItem.Checked = settings["ScreensaverOff"] == "True";
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error while reading startup parameters:");
                Debug.WriteLine(ex.StackTrace);
            }
        }

        //[STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }

        private void MainForm_Closing(object sender, CancelEventArgs e)
        {
            if (this.stopToolStripMenuItem.Enabled)
            {
                stopToolStripMenuItem_Click(this, new EventArgs());
            }
            this.SaveWindows();
            if ((this.thread != null) && this.thread.IsAlive)
            {
                this.thread.Abort();
            }
            this.CloseDB();
        }

        private void MainForm_KeyDown(object sender, KeyEventArgs e)
        {

            if (e.KeyCode.Equals(Keys.Right))
            {
                this.nextLidToolStripMenuItem_Click(sender, new EventArgs());
            }
            else if (e.KeyCode.Equals(Keys.Down))
            {
                this.nextFuskerToolStripMenuItem_Click(sender, new EventArgs());
            }
            else if (base.WindowState != FormWindowState.Normal && e.KeyCode.Equals(Keys.Escape))
            {

                this.ShowNormalState();



            }
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            this.OpenDoneDB();
            this.LoadWindows();
        }

        private void MainForm_Resize(object sender, EventArgs e)
        {
            if (!((base.WindowState != FormWindowState.Minimized) || this.notifyIcon1.Visible))
            {
                base.Hide();
                this.notifyIcon1.Icon = base.Icon;
                this.notifyIcon1.Visible = true;
            }
            else
            {
                this.DrawPicture();
            }
        }

        private void menuItem1_Click(object sender, EventArgs e)
        {
            if (base.WindowState == FormWindowState.Minimized)
            {
                base.Show();
                base.WindowState = FormWindowState.Normal;
                base.Activate();
                this.notifyIcon1.Visible = false;
            }
        }

        private void menuItem12_Click(object sender, EventArgs e)
        {
            new About(this).ShowDialog(this);
        }

        private void menuItem16_Click(object sender, EventArgs e)
        {
            this.blnNextLid = true;
        }

        private void menuItem17_Click(object sender, EventArgs e)
        {
            this.blnNextFusker = true;
        }

        private void menuItem2_Click(object sender, EventArgs e)
        {
            base.Close();
        }

        private void menuItem21_Click(object sender, EventArgs e)
        {
            new Settings(this).ShowDialog(this);
            if (this.ProxyType == 1)
            {
                this.GetProxySettingsFromBrowser();
            }
        }

        private void menuItem24_Click(object sender, EventArgs e)
        {
            try
            {
                Process.Start(this.ApplicationData);
            }
            catch
            {
            }
        }

        private void menuItem25_Click(object sender, EventArgs e)
        {
            Process.Start("http://fuskerclient.berlios.de/update/Default.php?" + Assembly.GetExecutingAssembly().GetName().Version.ToString());
        }

        private void menuItem26_Click(object sender, EventArgs e)
        {
            this.statusBar1.Visible = false;
            this.menuStrip1.Visible = false;
            base.FormBorderStyle = FormBorderStyle.None;
            base.WindowState = FormWindowState.Maximized;
        }

        private void menuItem7_Click(object sender, EventArgs e)
        {
            base.Close();
        }

        private void menuItem9_Click(object sender, EventArgs e)
        {
            MenuItem item = (MenuItem)sender;
            if (item.Checked)
            {
                item.Checked = false;
            }
            else
            {
                item.Checked = true;
            }
            this.DrawPicture();
        }

        private void nextFuskerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.blnNextFusker = true;
        }

        private void nextLidToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.blnNextLid = true;
        }

        private void notifyIcon1_DoubleClick(object sender, EventArgs e)
        {
            if (base.WindowState == FormWindowState.Minimized)
            {
                base.Show();
                base.WindowState = FormWindowState.Normal;
                base.Activate();
                this.notifyIcon1.Visible = false;
            }
        }

        private void pauseToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (this.blnPaused)
            {
                this.blnPaused = false;
                this.ShowStatus("...");
            }
            else
            {
                this.blnPaused = true;
                this.ShowStatus("paused...");
            }
            this.pauseToolStripMenuItem.Checked = this.blnPaused;
        }

        private void pictureBox1_DoubleClick(object sender, EventArgs e)
        {
            if (base.WindowState != FormWindowState.Normal)
            {
                this.ShowNormalState();
            }
            else
            {
                this.ShowFullScreen();
            }
        }

        private void RunIt()
        {
            int index = 0;
            Stream stream = this.Url("http://fuskerclient.berlios.de/update/conf.php?version=" + this.Version);
            if (stream != null)
            {
                StreamReader reader = new StreamReader(stream);
                string xml = reader.ReadToEnd();
                reader.Close();
                XmlDocument document = new XmlDocument();
                document.LoadXml(xml);
                XmlNodeList list = document.SelectNodes("//fusker");
                    for (index = 0; index < list.Count; index++)
                    {
                        XmlNode node = list.Item(index);
                        if (node.SelectSingleNode("@checked").Value == "true")
                        {
                            string str2;
                            string strUrl = node.SelectSingleNode("@url").Value;
                            Debug.WriteLine("Fusker: " + strUrl);
                            if (node.SelectSingleNode("@base") != null)
                            {
                                str2 = node.SelectSingleNode("@base").Value;
                            }
                            else
                            {
                                str2 = strUrl;
                            }
                            string strLinks = node.SelectSingleNode("@links").Value;
                            this.storeURL(strUrl, str2, strLinks);
                        }
                    }
            }
            while(true){
            	using (SQLiteCommand dbcmd = dbcon.CreateCommand())
	            {
	                dbcmd.CommandText = "SELECT URL, BASE, LINKS FROM Sites";
	                using (DbDataReader reader = dbcmd.ExecuteReader())
	                {
	                	while(reader.Read()){
	                		this.Fusker2Lids(reader.GetString(0), reader.GetString(1), reader.GetString(2));
	                	}
	                }
	            }
	        }
        }

        private void runToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.runToolStripMenuItem.Enabled = false;
            this.stopToolStripMenuItem.Enabled = true;
            this.pauseToolStripMenuItem.Enabled = true;
            this.pauseToolStripMenuItem.Checked = false;
            this.blnPaused = false;
            this.DoRun();
        }

        private void CloseDB()
        {
            dbcon.Close();
        }

        private void SaveWindows()
        {
            try
            {
                this.Text = this.FullName + " " + this.Version;
                System.Collections.Generic.Dictionary<string, string> settings = new System.Collections.Generic.Dictionary<string, string>()
                    {
                        {"X",base.Location.X.ToString()},
                        {"Y",base.Location.Y.ToString()},
                        {"W",base.Width.ToString()},
                        {"H",base.Height.ToString()},
                        {"Zoom",this.zoomToolStripMenuItem.Checked.ToString()},
                        {"ProxyType",this.ProxyType.ToString()},
                        {"ProxyServer",this.ProxyServer.ToString()},
                        {"ProxyUserID",this.ProxyUserid.ToString()},
                        {"ProxyPassword",this.ProxyPassword.ToString()},
                        {"ProxyDomain",this.ProxyDomain.ToString()},
                        {"IgnorePhotobucket",this.ignorePhotobucketToolStripMenuItem.Checked.ToString()},
                        {"ScreensaverOff",this.disableScreensaverToolStripMenuItem.Checked.ToString()}
                    };

                using (SQLiteTransaction transaction = dbcon.BeginTransaction())
                {
                    using (SQLiteCommand dbcmd = dbcon.CreateCommand())
                    {
                        dbcmd.CommandText = "INSERT OR REPLACE INTO Settings (Name,Value) VALUES (@name,@value)";
                        foreach (System.Collections.Generic.KeyValuePair<string, string> setting in settings)
                        {
                            dbcmd.Parameters.Clear();
                            dbcmd.Parameters.AddWithValue("value", setting.Value);
                            dbcmd.Parameters.AddWithValue("name", setting.Key);
                            dbcmd.ExecuteNonQuery();
                        }
                    }
                    transaction.Commit();
                }
            }

            catch
            {
            }
        }

        private void settingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new Settings(this).ShowDialog(this);
            if (this.ProxyType == 1)
            {
                this.GetProxySettingsFromBrowser();
            }
        }

        private void ShowFullScreen()
        {
            Cursor.Hide();
            this.screensaver = false;
            base.TopMost = true;
            this.blnBackupZoom = this.zoomToolStripMenuItem.Checked;
            this.zoomToolStripMenuItem.Checked = true;
            this.statusBar1.Visible = false;
            this.menuStrip1.Visible = false;
            base.FormBorderStyle = FormBorderStyle.None;
            base.WindowState = FormWindowState.Maximized;

        }

        private void ShowNormalState()
        {
            Cursor.Show();
            this.screensaver = true;
            this.zoomToolStripMenuItem.Checked = this.blnBackupZoom;
            base.TopMost = this.topmostToolStripMenuItem.Checked;
            base.WindowState = FormWindowState.Normal;
            this.statusBar1.Visible = true;
            this.menuStrip1.Visible = true;
            base.FormBorderStyle = FormBorderStyle.Sizable;
        }

        private void ShowStatus(string strIn)
        {
            if (this.statusBar1.InvokeRequired)
            {
                this.statusBar1.Invoke(new StatusDelegate(this.ShowStatus), new object[] { strIn });
            }
            else
            {
                int startIndex = strIn.LastIndexOf("http:");
                if (startIndex >= 0)
                {
                    startIndex = strIn.IndexOf("/", (int)(startIndex + 7));
                    if (startIndex > 0)
                    {
                        strIn = strIn.Substring(startIndex);
                    }
                }
                this.statusBar1.Text = strIn;
            }
        }

        private void stopToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.runToolStripMenuItem.Enabled = true;
            this.stopToolStripMenuItem.Enabled = false;
            this.pauseToolStripMenuItem.Enabled = false;
            this.pauseToolStripMenuItem.Checked = false;
            this.blnPaused = false;
            this.DoStop();
        }

        private void topmostToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.topmostToolStripMenuItem.Checked = !this.topmostToolStripMenuItem.Checked;
            base.TopMost = this.topmostToolStripMenuItem.Checked;
        }

        private void updateToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Process.Start("http://fuskerclient.berlios.de/update/Default.php?" + Assembly.GetExecutingAssembly().GetName().Version.ToString());
        }

        private void UpdateWindowBorder()
        {
            Point mousePosition = Control.MousePosition;
            if (!base.Bounds.Contains(mousePosition))
            {
                base.FormBorderStyle = FormBorderStyle.None;
                this.statusBar1.Visible = false;
            }
            else
            {
                base.FormBorderStyle = FormBorderStyle.Sizable;
                this.statusBar1.Visible = true;
            }
        }

        private Stream Url(string strUrl)
        {
            Debug.WriteLine("Url: " + strUrl);
            this.ShowStatus(strUrl);
            Stream responseStream = null;
            if (strUrl.IndexOf("res:") >= 0)
            {
                return Assembly.GetExecutingAssembly().GetManifestResourceStream(this.FullName + "." + strUrl.Substring(4));
            }
            try
            {
                WebRequest request = WebRequest.Create(strUrl);
                if (this.ProxyType != 0)
                {
                    request.Method = "GET";
                    WebProxy defaultProxy = new WebProxy();//WebProxy.GetDefaultProxy();
                    defaultProxy.Address = new Uri(this.ProxyServer);
                    request.Proxy = defaultProxy;
                    request.Proxy.Credentials = new NetworkCredential(this.ProxyUserid, this.ProxyPassword, this.ProxyDomain);
                }
                request.Timeout = 0x1388;
                responseStream = request.GetResponse().GetResponseStream();
            }
            catch (Exception exception)
            {
                Debug.WriteLine(exception.Message);
            }
            return responseStream;
        }

        private void zoomToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.zoomToolStripMenuItem.Checked = !this.zoomToolStripMenuItem.Checked;
            this.DrawPicture();
        }

        private delegate void DrawPictureDelegate();

        private delegate void StatusDelegate(string strIn);

        private void panel1_Paint(object sender, PaintEventArgs e)
        {

        }

        private void ignorePhotobucketToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void cacheToolStripMenuItem1_Click(object sender, EventArgs e)
        {

        }

        private void cacheShowToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                Process.Start(this.ApplicationData);
            }
            catch
            {
            }
        }

        private void emptyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (System.IO.Directory.Exists(this.ApplicationData))
            {
                try
                {
                    System.IO.Directory.Delete(this.ApplicationData, true);
                }

                catch (System.IO.IOException ex)
                {
                    Debug.WriteLine(ex.Message);
                }
            }

        }

        private void vacuumToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SQLiteCommand cmd = dbcon.CreateCommand();
            cmd.CommandText = "VACUUM";
            cmd.ExecuteNonQuery();
        }

        private void clearToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (SQLiteTransaction transaction = dbcon.BeginTransaction())
            {
                SQLiteCommand cmd = dbcon.CreateCommand();
                cmd.CommandText = "DELETE FROM Fuskers";
                cmd.ExecuteNonQuery();
                transaction.Commit();
            }
        }


    }
}

