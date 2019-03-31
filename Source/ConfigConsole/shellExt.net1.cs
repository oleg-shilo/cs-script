//css_dbg /t:winexe;
using System;
using System.Collections;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;

namespace CSSScript
{
    public class ShellExForm : System.Windows.Forms.Form
    {
        int spaceBetweenItems = 2;
        private TreeView treeView1;
        private Button upBtn;
        private Button downBtn;
        private Button cancelBtn;
        private Button okBtn;
        private Button browseBtn;
        private Button helpBtn;
        private System.ComponentModel.Container components = null;

        public bool readOnlyMode = false;
        private Button refreshBtn;
        private Button editBtn;
        public TreeViewEventHandler additionalOnCheckHandler;

        public ShellExForm(bool readOnlyMode)
        {
            this.readOnlyMode = readOnlyMode;
            InitializeComponent();
            upBtn.Text =
            downBtn.Text = "";
#if NET2
            treeView1.DrawMode = TreeViewDrawMode.OwnerDrawText;
#endif
        }

        public ShellExForm()
        {
            InitializeComponent();
            upBtn.Text =
            downBtn.Text = "";
#if NET2
            treeView1.DrawMode = TreeViewDrawMode.OwnerDrawText;
#endif
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (components != null)
                {
                    components.Dispose();
                }
            }

            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.treeView1 = new System.Windows.Forms.TreeView();
            this.upBtn = new System.Windows.Forms.Button();
            this.downBtn = new System.Windows.Forms.Button();
            this.cancelBtn = new System.Windows.Forms.Button();
            this.okBtn = new System.Windows.Forms.Button();
            this.browseBtn = new System.Windows.Forms.Button();
            this.helpBtn = new System.Windows.Forms.Button();
            this.refreshBtn = new System.Windows.Forms.Button();
            this.editBtn = new System.Windows.Forms.Button();
            this.SuspendLayout();
            //
            // treeView1
            //
            this.treeView1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                           | System.Windows.Forms.AnchorStyles.Left)
                           | System.Windows.Forms.AnchorStyles.Right)));
            this.treeView1.HideSelection = false;
            this.treeView1.Location = new System.Drawing.Point(12, 12);
            this.treeView1.Name = "treeView1";
            this.treeView1.Size = new System.Drawing.Size(383, 375);
            this.treeView1.TabIndex = 0;
            this.treeView1.AfterCheck += new System.Windows.Forms.TreeViewEventHandler(this.treeView1_AfterCheck);
            this.treeView1.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.treeView1_AfterSelect);
            //
            // upBtn
            //
            this.upBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.upBtn.Location = new System.Drawing.Point(413, 23);
            this.upBtn.Name = "upBtn";
            this.upBtn.Size = new System.Drawing.Size(75, 24);
            this.upBtn.TabIndex = 1;
            this.upBtn.Text = "Up";
            this.upBtn.Click += new System.EventHandler(this.upBtn_Click);
            //
            // downBtn
            //
            this.downBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.downBtn.Location = new System.Drawing.Point(413, 52);
            this.downBtn.Name = "downBtn";
            this.downBtn.Size = new System.Drawing.Size(75, 24);
            this.downBtn.TabIndex = 1;
            this.downBtn.Text = "Down";
            this.downBtn.Click += new System.EventHandler(this.downBtn_Click);
            //
            // cancelBtn
            //
            this.cancelBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.cancelBtn.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.cancelBtn.Location = new System.Drawing.Point(413, 363);
            this.cancelBtn.Name = "cancelBtn";
            this.cancelBtn.Size = new System.Drawing.Size(75, 24);
            this.cancelBtn.TabIndex = 2;
            this.cancelBtn.Text = "Cancel";
            this.cancelBtn.Click += new System.EventHandler(this.cancelBtn_Click);
            //
            // okBtn
            //
            this.okBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.okBtn.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.okBtn.Location = new System.Drawing.Point(413, 333);
            this.okBtn.Name = "okBtn";
            this.okBtn.Size = new System.Drawing.Size(75, 24);
            this.okBtn.TabIndex = 2;
            this.okBtn.Text = "Ok";
            this.okBtn.Click += new System.EventHandler(this.okBtn_Click);
            //
            // browseBtn
            //
            this.browseBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.browseBtn.Location = new System.Drawing.Point(413, 114);
            this.browseBtn.Name = "browseBtn";
            this.browseBtn.Size = new System.Drawing.Size(75, 24);
            this.browseBtn.TabIndex = 2;
            this.browseBtn.Text = "Browse";
            this.browseBtn.Click += new System.EventHandler(this.browseBtn_Click);
            //
            // helpBtn
            //
            this.helpBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.helpBtn.Location = new System.Drawing.Point(413, 174);
            this.helpBtn.Name = "helpBtn";
            this.helpBtn.Size = new System.Drawing.Size(75, 24);
            this.helpBtn.TabIndex = 2;
            this.helpBtn.Text = "Help";
            this.helpBtn.Click += new System.EventHandler(this.helpBtn_Click);
            //
            // refreshBtn
            //
            this.refreshBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.refreshBtn.Location = new System.Drawing.Point(413, 82);
            this.refreshBtn.Name = "refreshBtn";
            this.refreshBtn.Size = new System.Drawing.Size(75, 23);
            this.refreshBtn.TabIndex = 3;
            this.refreshBtn.Text = "Refresh";
            this.refreshBtn.Click += new System.EventHandler(this.refreshBtn_Click);
            //
            // editBtn
            //
            this.editBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.editBtn.Location = new System.Drawing.Point(413, 144);
            this.editBtn.Name = "editBtn";
            this.editBtn.Size = new System.Drawing.Size(75, 24);
            this.editBtn.TabIndex = 2;
            this.editBtn.Text = "Edit";
            this.editBtn.Click += new System.EventHandler(this.editBtn_Click);
            //
            // ShellExForm
            //
            this.AcceptButton = this.okBtn;
            this.CancelButton = this.cancelBtn;
            this.ClientSize = new System.Drawing.Size(503, 399);
            this.Controls.Add(this.refreshBtn);
            this.Controls.Add(this.helpBtn);
            this.Controls.Add(this.editBtn);
            this.Controls.Add(this.browseBtn);
            this.Controls.Add(this.okBtn);
            this.Controls.Add(this.cancelBtn);
            this.Controls.Add(this.downBtn);
            this.Controls.Add(this.upBtn);
            this.Controls.Add(this.treeView1);
            this.KeyPreview = true;
            this.Name = "ShellExForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "CS-Script Advanced Shell Extension";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.ShellExForm_KeyDown);
            this.ResumeLayout(false);
        }

        #endregion Windows Form Designer generated code

        private void button1_Click(object sender, System.EventArgs e)
        {
            this.Close();
        }

        public string baseDirectory = Environment.ExpandEnvironmentVariables(@"%CSSCRIPT_DIR%\Lib\ShellExtensions\CS-Script");

        private void Form1_Load(object sender, EventArgs e)
        {
            treeView1.CheckBoxes = true;
            RefreshTreeView();
        }

        public void RefreshTreeView()
        {
            treeView1.Nodes.Clear();
            ReadShellExtensions(baseDirectory);
            treeView1.SelectedNode = treeView1.Nodes[0];

            if (readOnlyMode)
            {
                treeView1.Dock = DockStyle.Fill;
                treeView1.BackColor = Color.WhiteSmoke;
                foreach (Control c in this.Controls)
                    if (c is Button)
                        c.Visible = false;
            }
        }

        class SEItem
        {
            public SEItem(string path)
            {
                this.path = path;
                this.location = path;
                Refresh();
            }

            public void Refresh()
            {
                string logicalPath = path;
                isDir = !File.Exists(logicalPath);
                if (path.EndsWith(".disabled"))
                {
                    enabled = false;
                    logicalPath = logicalPath.Replace(".disabled", "");
                }
                else
                {
                    enabled = true;
                }
                isSeparator = !isDir && (path.EndsWith("separator") || path.EndsWith("separator.disabled"));
                isConsole = !isDir && !isSeparator && path.EndsWith(".c.cmd");
                level = path.Split(Path.DirectorySeparatorChar).Length - 1;
            }

            public override string ToString()
            {
                if (isSeparator)
                    return "------------------------";
                else
                {
                    string[] parts = path.Split(Path.DirectorySeparatorChar);
                    return parts[parts.Length - 1].Substring(3).Replace(".c.cmd", "").Replace(".cmd", "").Replace(".disabled", "");
                }
            }

            public string GetLogicalFileName()
            {
                string retval = this.ToString();
                if (!isSeparator)
                {
                    if (!isDir)
                    {
                        if (isConsole)
                            retval += ".c";
                        retval += ".cmd";
                    }
                }
                else
                {
                    retval = "separator";
                }
                if (!enabled)
                    retval += ".disabled";
                return retval;
            }

            public bool isDir;
            public bool isConsole;
            public bool isSeparator;
            bool enabled = false;

            public bool Enabled
            {
                get { return enabled; }
                set
                {
                    if (enabled != value)
                    {
                        if (value)
                            path = path.Substring(0, path.Length - ".disabled".Length);
                        else
                            path += ".disabled";
                    }
                    enabled = value;
                }
            }

            public string path;     //note this.path can be changed during the user operations
            public string location; //original value of this.path
            public int level;
        }

        public void ReadShellExtensions(string path)
        {
            int iterator = 0;
            ArrayList dirList = new ArrayList();
            ArrayList itemsList = new ArrayList();

            dirList.Add(path);

            while (iterator < dirList.Count)
            {
                foreach (string dir in Directory.GetDirectories(dirList[iterator].ToString()))
                {
                    dirList.Add(dir);
                    itemsList.Add(dir);
                }
                foreach (string file in Directory.GetFiles(dirList[iterator].ToString(), "*.cmd*"))
                    itemsList.Add(file);
                foreach (string file in Directory.GetFiles(dirList[iterator].ToString(), "*.separator"))
                    itemsList.Add(file);

                iterator++;
            }

            //sort according the ShellExtension sorting algorithm
            itemsList.Sort(Sorter.instance);

            //foreach (string item in itemsList)
            //    Trace.WriteLine(item);

            TreeNode dirNode = null;
            foreach (string item in itemsList)
            {
                SEItem shellEx = new SEItem(item);
                TreeNode node = new TreeNode(shellEx.ToString());
                node.Checked = shellEx.Enabled;
                node.Tag = shellEx;

                if (dirNode == null)
                {
                    treeView1.Nodes.Add(node);
                }
                else
                {
                    TreeNode parentNode = dirNode;
                    SEItem parentShellEx = null;
                    do
                    {
                        parentShellEx = (parentNode.Tag as SEItem);

                        if (parentShellEx == null)
                        {
                            treeView1.Nodes.Add(node);
                        }
                        else if (parentShellEx.level == shellEx.level - 1)
                        {
                            parentNode.Nodes.Add(node);
                            break;
                        }

                        parentNode = parentNode.Parent;
                    } while (parentNode != null);

                    if (parentNode == null)
                        treeView1.Nodes.Add(node);
                }

                if (shellEx.isDir)
                    dirNode = node;
            }
            treeView1.ExpandAll();
        }

        class Sorter : IComparer
        {
            static public Sorter instance = new Sorter();

            public int Compare(object x, object y)
            {
                return SortMethod(x.ToString(), y.ToString());
            }

            int SortMethod(string x, string y)
            {
                string[] partsX = x.Split(Path.DirectorySeparatorChar);
                string[] partsY = y.Split(Path.DirectorySeparatorChar);
                for (int i = 0; i < Math.Min(partsX.Length, partsY.Length); i++)
                {
                    string indexX = partsX[i].Substring(0, Math.Min(2, partsX[i].Length));
                    string indexY = partsY[i].Substring(0, Math.Min(2, partsX[i].Length));
                    if (indexX != indexY)
                        return string.Compare(indexX, indexY);
                }
                if (partsX.Length < partsY.Length)
                    return -1;
                else if (partsX.Length == partsY.Length)
                    return 0;
                else
                    return 1;
            }
        }

        Brush treeViewBackBrush = null;

        Brush TreeViewBackBrush
        {
            get
            {
                if (treeViewBackBrush == null)
                    treeViewBackBrush = new SolidBrush(treeView1.BackColor);
                return treeViewBackBrush;
            }
        }

#if NET2
        private void treeView1_DrawNode(object sender, DrawTreeNodeEventArgs e)
        {
            SEItem item = (SEItem)e.Node.Tag;

            Brush b = Brushes.Black;
            if (item.isConsole)
                b = Brushes.Blue;

            if (!item.Enabled)
            {
                b = Brushes.Gray;
            }
            else
            {
                TreeNode parent = e.Node.Parent;
                while (parent != null)
                {
                    if (!parent.Checked)
                    {
                        b = Brushes.Gray;
                        break;
                    }
                    parent = parent.Parent;
                }
            }

            Rectangle frame = e.Bounds;
            if ((e.State & TreeNodeStates.Selected) != 0)
            {
                frame.Width = (int)e.Graphics.MeasureString(item.ToString(), treeView1.Font).Width;
                e.Graphics.FillRectangle(TreeViewBackBrush, frame);
                frame.Inflate(-1, -1);
                e.Graphics.DrawRectangle(Pens.Red, frame);
            }

            if (item.isSeparator)
                e.Graphics.DrawLine(Pens.Black, frame.Left + 4, frame.Top + frame.Height / 2, frame.Right - 4, frame.Top + frame.Height / 2);
            else
                e.Graphics.DrawString(item.ToString(), treeView1.Font, b, e.Bounds.Left, e.Bounds.Top + 2);
        }
#endif
        bool ignoreChecking = false;

        private void treeView1_AfterCheck(object sender, TreeViewEventArgs e)
        {
            if (!ignoreChecking)
            {
                SEItem item = (SEItem)e.Node.Tag;
                if (readOnlyMode)
                {
                    ignoreChecking = true;
                    e.Node.Checked = !e.Node.Checked;
                    ignoreChecking = false;
                }
                else
                {
                    if (item.isDir) //directories cannot be disabled
                    {
                        ignoreChecking = true;
                        e.Node.Checked = !e.Node.Checked;
                        ignoreChecking = false;
                        MessageBox.Show("Disabling 'parent' menus is not supported.\nIf you want to control such menu items you need to press 'Browse' button and adjust the corresponding directories.");
                    }
                    else
                    {
                        item.Enabled = !item.Enabled;
                        //if (item.Enabled)
                        //    e.Node.Expand();
                        //else
                        //    e.Node.Collapse();
                        treeView1.Invalidate();
                    }
                }

                if (additionalOnCheckHandler != null)
                    additionalOnCheckHandler(sender, e);
            }
        }

        private void upBtn_Click(object sender, EventArgs e)
        {
            TreeNode node = treeView1.SelectedNode;
            if (node != null)
            {
                if (node.Index == 0) //first
                {
                    if (node.Parent != null)
                    {
                        int nodeIndex = node.Parent.Index;
                        TreeNodeCollection coll = (node.Parent.Parent == null ? treeView1.Nodes : node.Parent.Parent.Nodes);

                        node.Remove();
                        coll.Insert(nodeIndex, node);
                    }
                }
                else
                {
                    int nodeIndex = node.Index;
                    TreeNodeCollection coll = (node.Parent == null ? treeView1.Nodes : node.Parent.Nodes);

                    if (coll[nodeIndex - 1].Nodes.Count != 0) //has child nodes
                    {
                        node.Remove();
                        coll[nodeIndex - 1].Nodes.Add(node);
                    }
                    else
                    {
                        node.Remove();
                        coll.Insert(nodeIndex - 1, node);
                    }
                }
                treeView1.SelectedNode = node;
            }
            treeView1.Select();
        }

        private void downBtn_Click(object sender, EventArgs e)
        {
            TreeNode node = treeView1.SelectedNode;
            if (node != null)
            {
                TreeNodeCollection coll = (node.Parent == null ? treeView1.Nodes : node.Parent.Nodes);

                if (node.Index == coll.Count - 1) //last
                {
                    if (node.Parent != null)
                    {
                        TreeNodeCollection parentColl = node.Parent.Parent == null ? treeView1.Nodes : node.Parent.Parent.Nodes;
                        int nodeIndex = node.Parent.Index;
                        node.Remove();
                        parentColl.Insert(nodeIndex + 1, node);
                    }
                }
                else
                {
                    int nodeIndex = node.Index;

                    if (coll[nodeIndex + 1].Nodes.Count != 0) //has child nodes
                    {
                        node.Remove();
                        coll[nodeIndex].Nodes.Insert(0, node);
                    }
                    else
                    {
                        node.Remove();
                        coll.Insert(nodeIndex + 1, node);
                    }
                }
                treeView1.SelectedNode = node;
            }
            treeView1.Select();
        }

        #region Images

        static Image DullImage(Image img)
        {
            Bitmap newBitmap = new Bitmap(img.Width, img.Height, PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(newBitmap))
            {
                //g.DrawImage(image, 0.0f, 0.0f); //draw original image

                ImageAttributes imageAttributes = new ImageAttributes();
                int width = img.Width;
                int height = img.Height;

                float[][] colorMatrixElements =
                {
                    new float[] {1,  0,  0,  0, 0},        // red scaling factor of 1
                    new float[] {0,  1,  0,  0, 0},        // green scaling factor of 1
                    new float[] {0,  0,  1,  0, 0},        // blue scaling factor of 1
                    new float[] {0,  0,  0,  1, 0},        // alpha scaling factor of 1
                    new float[] {.05f, .05f, .05f, 0, 1}    // three translations of 0.2
                };

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

                int xOffset = -4;
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

        #endregion Images

        private void treeView1_AfterSelect(object sender, TreeViewEventArgs e)
        {
            upBtn.Image = upBtn.Enabled ? imgUp : imgUpDisabled;
            downBtn.Image = downBtn.Enabled ? imgDown : imgDownDisabled;
        }

        private void okBtn_Click(object sender, EventArgs e)
        {
            Cursor = Cursors.WaitCursor;
            try
            {
                ProcessDirNode(treeView1.Nodes);
                RemoveEmptyChildDirs(baseDirectory);
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }

        void RemoveEmptyChildDirs(string path)
        {
            int iterator = 0;
            ArrayList dirList = new ArrayList();

            dirList.Add(path);

            while (iterator < dirList.Count)
            {
                foreach (string dir in Directory.GetDirectories(dirList[iterator].ToString()))
                    dirList.Add(dir);
                iterator++;
            }
            dirList.RemoveAt(0);//remove parent dir

            foreach (string dir in dirList)
                if (Directory.Exists(dir) && IsDirEmpty(dir))
                    Directory.Delete(dir, true);
        }

        bool IsDirEmpty(string path)
        {
            int iterator = 0;
            ArrayList dirList = new ArrayList();
            ArrayList fileList = new ArrayList();

            dirList.Add(path);

            while (iterator < dirList.Count)
            {
                foreach (string dir in Directory.GetDirectories(dirList[iterator].ToString()))
                    dirList.Add(dir);
                foreach (string file in Directory.GetFiles(dirList[iterator].ToString()))
                    fileList.Add(file);

                iterator++;
            }
            return fileList.Count == 0;
        }

        void ProcessFileNode(TreeNode node)
        {
            SEItem shellEx = (SEItem)node.Tag;

            //reconstruct full (File not treeView) path
            string newPath = (node.Index * spaceBetweenItems).ToString("D2") + "." + shellEx.GetLogicalFileName();

            TreeNode child = node;
            TreeNode parent;
            while ((parent = child.Parent) != null)
            {
                newPath = Path.Combine((parent.Index * spaceBetweenItems).ToString("D2") + "." + (parent.Tag as SEItem).GetLogicalFileName(), newPath);
                child = parent;
            }
            newPath = Path.Combine(baseDirectory, newPath);
            //Trace.WriteLine(newPath);
            if (newPath != shellEx.location)
            {
                if (!Directory.Exists(Path.GetDirectoryName(newPath)))
                    Directory.CreateDirectory(Path.GetDirectoryName(newPath));
                File.Move(shellEx.location, newPath);
            }
        }

        void ProcessDirNode(TreeNodeCollection nodes)
        {
            ArrayList dirs = new ArrayList();
            foreach (TreeNode node in nodes)
            {
                if ((node.Tag as SEItem).isDir)
                    dirs.Add(node);
                else
                    ProcessFileNode(node);
            }
            foreach (TreeNode node in dirs)
                ProcessDirNode(node.Nodes);
        }

        private void browseBtn_Click(object sender, EventArgs e)
        {
            Process.Start("explorer.exe", "/e, \"" + Environment.ExpandEnvironmentVariables(@"%CSSCRIPT_DIR%\Lib\ShellExtensions\CS-Script") + "\"");
        }

        private void helpBtn_Click(object sender, EventArgs e)
        {
            Process.Start(Environment.ExpandEnvironmentVariables(@"%windir%\System32\rundll32.exe"),
                "\"" + Environment.ExpandEnvironmentVariables(@"%CSSCRIPT_DIR%\Lib\ShellExtensions\CS-Script\ShellExt.cs.{25D84CB0-7345-11D3-A4A1-0080C8ECFED4}.dll") + "\", Help");
        }

        private void ShellExForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F5)
                RefreshTreeView();
        }

        private void refreshBtn_Click(object sender, EventArgs e)
        {
            RefreshTreeView();
        }

        private void editBtn_Click(object sender, EventArgs e)
        {
            TreeNode node = treeView1.SelectedNode;
            if (node != null)
                Process.Start("notepad.exe", "\"" + (node.Tag as SEItem).location + "\"");
        }

        private void cancelBtn_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}

class Script
{
    const string usage = "Usage: cscscript shellEx ...\nStarts configuration console for CS-Script Advanced Shell Extension.\n";

    static public void Main(string[] args)
    {
        if (args.Length == 1 && (args[0] == "?" || args[0] == "/?" || args[0] == "-?" || args[0].ToLower() == "help"))
            Console.WriteLine(usage);
        else
            Application.Run(new CSSScript.ShellExForm());
    }
}