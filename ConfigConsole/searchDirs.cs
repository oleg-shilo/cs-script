//css_dbg /t:winexe;
////css_ref FileDialogs.dll;
//css_reference csscriptlibrary.dll;
using System;
using System.Collections;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Xml;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using Microsoft.Win32;
using System.Text;
using System.Reflection;
using System.Diagnostics;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using csscript;

namespace CSSScript
{
    public class SearchDirs : Form
    {
        private System.ComponentModel.IContainer components = null;
        private ListBox listBox1;
        private Button ok;
        private Button apply;
        private Button cancel;
        private Panel panel1;

        private LinkLabel edit;
        private LinkLabel remove;
        private LinkLabel add;
        private Button downBtn;
        private Button upBtn;
        private bool modified = false;

        public SearchDirs()
        {
            InitializeComponent();
            upBtn.Text =
            downBtn.Text = "";
            upBtn.Image = imgUp;
            downBtn.Image = imgDown;
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.listBox1 = new System.Windows.Forms.ListBox();
            this.ok = new System.Windows.Forms.Button();
            this.apply = new System.Windows.Forms.Button();
            this.cancel = new System.Windows.Forms.Button();
            this.panel1 = new System.Windows.Forms.Panel();
            this.downBtn = new System.Windows.Forms.Button();
            this.upBtn = new System.Windows.Forms.Button();
            this.edit = new System.Windows.Forms.LinkLabel();
            this.remove = new System.Windows.Forms.LinkLabel();
            this.add = new System.Windows.Forms.LinkLabel();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // listBox1
            // 
            this.listBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.listBox1.Location = new System.Drawing.Point(2, 0);
            this.listBox1.Name = "listBox1";
            this.listBox1.Size = new System.Drawing.Size(607, 225);
            this.listBox1.TabIndex = 0;
            this.listBox1.SelectedIndexChanged += new System.EventHandler(this.listBox1_SelectedIndexChanged);
            // 
            // ok
            // 
            this.ok.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.ok.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.ok.Enabled = false;
            this.ok.Location = new System.Drawing.Point(190, 281);
            this.ok.Name = "ok";
            this.ok.Size = new System.Drawing.Size(75, 23);
            this.ok.TabIndex = 1;
            this.ok.Text = "Ok";
            this.ok.Click += new System.EventHandler(this.ok_Click);
            // 
            // apply
            // 
            this.apply.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.apply.Enabled = false;
            this.apply.Location = new System.Drawing.Point(271, 281);
            this.apply.Name = "apply";
            this.apply.Size = new System.Drawing.Size(75, 23);
            this.apply.TabIndex = 1;
            this.apply.Text = "Apply";
            this.apply.Click += new System.EventHandler(this.apply_Click);
            // 
            // cancel
            // 
            this.cancel.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.cancel.Location = new System.Drawing.Point(352, 281);
            this.cancel.Name = "cancel";
            this.cancel.Size = new System.Drawing.Size(75, 23);
            this.cancel.TabIndex = 1;
            this.cancel.Text = "Cancel";
            this.cancel.Click += new System.EventHandler(this.cancel_Click);
            // 
            // panel1
            // 
            this.panel1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.panel1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panel1.Controls.Add(this.downBtn);
            this.panel1.Controls.Add(this.upBtn);
            this.panel1.Controls.Add(this.edit);
            this.panel1.Controls.Add(this.remove);
            this.panel1.Controls.Add(this.add);
            this.panel1.Controls.Add(this.listBox1);
            this.panel1.Location = new System.Drawing.Point(2, 1);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(613, 260);
            this.panel1.TabIndex = 2;
            // 
            // downBtn
            // 
            this.downBtn.Location = new System.Drawing.Point(167, 230);
            this.downBtn.Name = "downBtn";
            this.downBtn.Size = new System.Drawing.Size(28, 25);
            this.downBtn.TabIndex = 3;
            this.downBtn.Text = "Down";
            this.downBtn.Click += new System.EventHandler(this.downBtn_Click);
            // 
            // upBtn
            // 
            this.upBtn.Location = new System.Drawing.Point(136, 230);
            this.upBtn.Name = "upBtn";
            this.upBtn.Size = new System.Drawing.Size(28, 25);
            this.upBtn.TabIndex = 3;
            this.upBtn.Text = "Up";
            this.upBtn.Click += new System.EventHandler(this.upBtn_Click);
            // 
            // edit
            // 
            this.edit.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.edit.Enabled = false;
            this.edit.Location = new System.Drawing.Point(39, 232);
            this.edit.Name = "edit";
            this.edit.Size = new System.Drawing.Size(25, 13);
            this.edit.TabIndex = 2;
            this.edit.TabStop = true;
            this.edit.Text = "Edit";
            this.edit.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.edit_LinkClicked);
            // 
            // remove
            // 
            this.remove.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.remove.Enabled = false;
            this.remove.Location = new System.Drawing.Point(70, 232);
            this.remove.Name = "remove";
            this.remove.Size = new System.Drawing.Size(47, 13);
            this.remove.TabIndex = 2;
            this.remove.TabStop = true;
            this.remove.Text = "Remove";
            this.remove.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.remove_LinkClicked);
            // 
            // add
            // 
            this.add.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.add.Location = new System.Drawing.Point(9, 232);
            this.add.Name = "add";
            this.add.Size = new System.Drawing.Size(26, 13);
            this.add.TabIndex = 2;
            this.add.TabStop = true;
            this.add.Text = "Add";
            this.add.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.add_LinkClicked);
            // 
            // SearchDirs
            // 
            this.AcceptButton = this.ok;
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
            this.CancelButton = this.cancel;
            this.ClientSize = new System.Drawing.Size(616, 316);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.cancel);
            this.Controls.Add(this.apply);
            this.Controls.Add(this.ok);
            this.MaximizeBox = false;
            this.Name = "SearchDirs";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "CS-Script SearchDirs";
            this.Load += new System.EventHandler(this.SearchDirs_Load);
            this.Closing += new System.ComponentModel.CancelEventHandler(this.ConfigForm_Closing);
            this.panel1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        static public string ConfigFile
        {
            get
            {
                if (Environment.GetEnvironmentVariable("CSSCRIPT_DIR") == null)
                    throw new ApplicationException("CS-Script is not installed");

                return Environment.ExpandEnvironmentVariables("%CSSCRIPT_DIR%\\css_config.xml");
            }
        }
        [STAThread]
        static public void Main(string[] args)
        {
            if (args.Length == 1 && (args[0] == "?" || args[0] == "/?" || args[0] == "-?" || args[0].ToLower() == "help"))
                Console.WriteLine(
                    "Usage: cscscript SearchDirs [/add path]\n" +
                    "This script displays list of the CS-Script search directories for script file and assembly probing.\n\n" +
                    "</add> -  command switch to add new search directory\n" +
                    "<path> -  search directory\n" +
                    " Example: cscscript SearchDirs /add c:\\Temp\n");
            else
                try
                {
                    if (args.Length > 1 && args[0] == "/add")
                    {
                        Settings settings = Settings.Load(ConfigFile);
                        if (!settings.SearchDirs.Trim().EndsWith(";"))
                            settings.SearchDirs += ";" + args[1] + ";";
                        else
                            settings.SearchDirs += args[1] + ";";
                        settings.Save(ConfigFile);
                    }
                    else
                        Application.Run(new SearchDirs());
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "CS-Script");
                }
        }

        private void ConfigForm_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (modified)
            {
                DialogResult response = MessageBox.Show("The SearchDirs have been modified.\n Do you want to save them?", "CS-Script", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
                if (response == DialogResult.Yes)
                    apply_Click(sender, e);
                else if (response == DialogResult.Cancel)
                    e.Cancel = true;
            }
        }
        static Image DullImage(Image img)
        {
            Bitmap newBitmap = new Bitmap(img.Width, img.Height, PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(newBitmap))
            {
                //g.DrawImage(image, 0.0f, 0.0f); //draw original image

                ImageAttributes imageAttributes = new ImageAttributes();
                int width = img.Width;
                int height = img.Height;

                float[][] colorMatrixElements = { 
												new float[] {1,  0,  0,  0, 0},        // red scaling factor of 1
												new float[] {0,  1,  0,  0, 0},        // green scaling factor of 1
												new float[] {0,  0,  1,  0, 0},        // blue scaling factor of 1
												new float[] {0,  0,  0,  1, 0},        // alpha scaling factor of 1
												new float[] {.05f, .05f, .05f, 0, 1}};    // three translations of 0.2

                ColorMatrix colorMatrix = new ColorMatrix(colorMatrixElements);

                imageAttributes.SetColorMatrix(colorMatrix, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);

                g.DrawImage(
                    img,
                    new Rectangle(0, 0, width, height),  // destination rectangle 
                    0, 0,        // upper-left corner of source rectangle 
                    width,       // width of source rectangle
                    height,      // height of source rectangle
                    GraphicsUnit.Pixel,
                    imageAttributes);
            }
            return newBitmap;
        }
        static Image CreateUpImage()
        {
            Image img = new Bitmap(25, 25);
            using (Graphics g = Graphics.FromImage(img))
            {
                LinearGradientBrush lgb = new LinearGradientBrush(
                     new Point(0, 0),
                     new Point(25, 0),
                     Color.YellowGreen,
                     Color.DarkGreen);

                int xOffset = -3;
                int yOffset = -1;
                g.FillPolygon(lgb, new Point[]
                    {
                        new Point(15+xOffset,5+yOffset),
                        new Point(25+xOffset,15+yOffset),
                        new Point(18+xOffset,15+yOffset),
                        new Point(18+xOffset,20+yOffset),
                        new Point(12+xOffset,20+yOffset),
                        new Point(12+xOffset,15+yOffset),
                        new Point(5+xOffset,15+yOffset)
                    });
            }
            return img;
        }
        static Image CreateDownImage()
        {
            Image img = new Bitmap(CreateUpImage());
            img.RotateFlip(RotateFlipType.Rotate180FlipX);
            return img;
        }
        Image imgUp = CreateUpImage();
        Image imgDown = CreateDownImage();
        Image imgUpDisabled = DullImage(CreateUpImage());
        Image imgDownDisabled = DullImage(CreateDownImage());

        private void SearchDirs_Load(object sender, EventArgs e)
        {
            try
            {
                Settings settings = Settings.Load(ConfigFile);
                foreach (string dir in settings.SearchDirs.Split(';'))
                    if (dir.Trim() != "")
                        listBox1.Items.Add(dir.Trim());

                add.Enabled = true;
                ok.Enabled =
                apply.Enabled = modified;

                listBox1_SelectedIndexChanged(null, null);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            upBtn.Enabled =
            downBtn.Enabled =
            edit.Enabled =
            remove.Enabled = (listBox1.SelectedIndex != -1);

            upBtn.Image = upBtn.Enabled ? imgUp : imgUpDisabled;
            downBtn.Image = downBtn.Enabled ? imgDown : imgDownDisabled;
        }

        private void edit_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            using (EditDirForm dlg = new EditDirForm(listBox1.SelectedItem.ToString()))
            {
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    listBox1.Items[listBox1.SelectedIndex] = dlg.Dir;
                    modified = true;
                    ok.Enabled = apply.Enabled = modified;
                }
            }
        }

        private void add_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            using (EditDirForm dlg = new EditDirForm(""))
            {
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    listBox1.Items.Add(dlg.Dir);
                    modified = true;
                    ok.Enabled = apply.Enabled = modified;
                    listBox1.SelectedIndex = listBox1.Items.Count - 1;
                }
            }
        }

        private void remove_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            int index = listBox1.SelectedIndex;
            listBox1.Items.RemoveAt(listBox1.SelectedIndex);
            modified = true;
            ok.Enabled = apply.Enabled = modified;
            if (index < listBox1.Items.Count)
                listBox1.SelectedIndex = index;
            else
                listBox1.SelectedIndex = listBox1.Items.Count - 1;
        }

        private void apply_Click(object sender, EventArgs e)
        {
            try
            {
                Settings settings = Settings.Load(ConfigFile);
                settings.SearchDirs = "";
                foreach (string dir in listBox1.Items)
                    settings.SearchDirs += dir + ";";
                settings.Save(ConfigFile);
                modified = false;
                apply.Enabled = modified;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void ok_Click(object sender, EventArgs e)
        {
            apply_Click(sender, e);
            Close();
        }

        private void cancel_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void downBtn_Click(object sender, EventArgs e)
        {
            int index = listBox1.SelectedIndex;
            if (index == listBox1.Items.Count - 1 || listBox1.SelectedItem == null)
                return;

            string dir = listBox1.SelectedItem.ToString();
            listBox1.Items.RemoveAt(index);
            listBox1.Items.Insert(index + 1, dir);
            listBox1.SelectedIndex = index + 1;
            modified = true;
            ok.Enabled = apply.Enabled = modified;
        }

        private void upBtn_Click(object sender, EventArgs e)
        {
            int index = listBox1.SelectedIndex;
            if (index == 0 || listBox1.SelectedItem == null)
                return;

            string dir = listBox1.SelectedItem.ToString();
            listBox1.Items.RemoveAt(index);
            listBox1.Items.Insert(index - 1, dir);
            listBox1.SelectedIndex = index - 1;
            modified = true;
            ok.Enabled = apply.Enabled = modified;
        }
    }

    public class SelectFolderDialog
    {
        static public string SelectFolder(string initialDirectory)
        {
            using (OpenFileDialog dlg = new OpenFileDialog())
            {
                if (initialDirectory != null && initialDirectory != "")
                    dlg.InitialDirectory = initialDirectory;
                //else
                //    dlg.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.CommonProgramFiles);

                dlg.Title = "Please choose a folder";
                dlg.CheckFileExists = false;


                dlg.FileName = "[Folder]";
                dlg.Filter = "Folders|no.files";

                if (dlg.ShowDialog() == DialogResult.OK)
                    return Path.GetDirectoryName(dlg.FileName);
                else
                    return "";
            }
        }
    }
    public class EditDirForm : Form
    {
        private System.ComponentModel.IContainer components = null;
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.ok = new System.Windows.Forms.Button();
            this.cancel = new System.Windows.Forms.Button();
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.browse = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // ok
            // 
            this.ok.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.ok.Location = new System.Drawing.Point(178, 40);
            this.ok.Name = "ok";
            this.ok.Size = new System.Drawing.Size(75, 23);
            this.ok.TabIndex = 0;
            this.ok.Text = "Ok";
            this.ok.Click += new System.EventHandler(this.ok_Click);
            // 
            // cancel
            // 
            this.cancel.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.cancel.Location = new System.Drawing.Point(259, 40);
            this.cancel.Name = "cancel";
            this.cancel.Size = new System.Drawing.Size(75, 23);
            this.cancel.TabIndex = 1;
            this.cancel.Text = "Cancel";
            this.cancel.Click += new System.EventHandler(this.ok_Cancel);
            // 
            // textBox1
            // 
            this.textBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.textBox1.Location = new System.Drawing.Point(12, 11);
            this.textBox1.Name = "textBox1";
            this.textBox1.Size = new System.Drawing.Size(480, 20);
            this.textBox1.TabIndex = 0;
            // 
            // browse
            // 
            this.browse.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.browse.Location = new System.Drawing.Point(495, 9);
            this.browse.Name = "browse";
            this.browse.Size = new System.Drawing.Size(26, 23);
            this.browse.TabIndex = 0;
            this.browse.Text = "...";
            this.browse.Click += new System.EventHandler(this.browse_Click);
            // 
            // EditDirForm
            // 
            this.AcceptButton = this.ok;
            this.CancelButton = this.cancel;
            this.ClientSize = new System.Drawing.Size(530, 72);
            this.Controls.Add(this.textBox1);
            this.Controls.Add(this.browse);
            this.Controls.Add(this.cancel);
            this.Controls.Add(this.ok);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
            this.MaximumSize = new System.Drawing.Size(3710, 106);
            this.MinimumSize = new System.Drawing.Size(371, 106);
            this.Name = "EditDirForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Directory";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button ok;
        private System.Windows.Forms.Button cancel;
        private System.Windows.Forms.TextBox textBox1;
        private System.Windows.Forms.Button browse;

        public string Dir
        {
            get
            {
                return textBox1.Text;
            }
        }
        public EditDirForm(string dir)
        {
            InitializeComponent();
            textBox1.Text = dir;
        }

        static internal bool IsVistaOrHigher()
        {
            return Environment.OSVersion.Version.Major >= 6;
        }

        private void browse_Click(object sender, EventArgs e)
        {
            //FileDialogs do not work well on Vista
            // bool useClassicFolderBrowser = IsVistaOrHigher();
            // if (useClassicFolderBrowser)
            {
                using (FolderBrowserDialog browse = new FolderBrowserDialog())
                {
                    browse.Description = "Select CS-Script search directorty to add.";
                    browse.SelectedPath = textBox1.Text;

                    if (browse.ShowDialog() == DialogResult.OK)
                        textBox1.Text = browse.SelectedPath;
                }
            }
            // else
            // {
                // using (FileDialogs.SelectFolderDialog dialog = new FileDialogs.SelectFolderDialog())
                // {
                    // if (DialogResult.OK == dialog.ShowDialog())
                    // {
                        // if (dialog.SelectedPath != "")
                            // textBox1.Text = dialog.SelectedPath;
                    // }
                // }
            // }
        }
        private void ok_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.OK;
            Close();
        }
        private void ok_Cancel(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            Close();
        }
    }
}
