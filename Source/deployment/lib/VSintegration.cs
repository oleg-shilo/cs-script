//css_dbg /t:winexe;
//css_imp VSAddIn.cs;
using System;
using System.IO;
using System.Drawing;
using System.Diagnostics;
using System.Windows.Forms.VisualStyles;
using System.Windows.Forms;

namespace Scripting
{
    public class Form1 : System.Windows.Forms.Form
    {
        const string installCaption = "Install";
        const string insertCaption = "Insert";
        const string importCaption = "Import";
        const string removeCaption = "Remove";
        const string naCaption = "";

        const int ideNameCol = 0;
        const int ideInstalledCol = 1;
        const int addinActionCol = 2;
        const int toolbarInstalledCol = 3;
        const int toolbarInsertActionCol = 4;
        const int toolbarImportActiondCol = 5;
        const int toolbarRemoveActiondCol = 6;

        private DataGridView dataGridView1;
        private Button help;
        private Button close;
        private Timer timer1;
        private System.ComponentModel.IContainer components;

        delegate void addColumnDlgt(DataGridViewButtonColumn column, DataGridView grid);

        public Form1()
        {
            InitializeComponent();

            dataGridView1.Columns.Add(new DataGridViewTextBoxColumn());
            dataGridView1.Columns.Add(new DataGridViewCheckBoxColumn());
            dataGridView1.Columns.Add(new DataGridViewDisableButtonColumn());
            dataGridView1.Columns.Add(new DataGridViewCheckBoxColumn());
            dataGridView1.Columns.Add(new DataGridViewDisableButtonColumn());
            dataGridView1.Columns.Add(new DataGridViewDisableButtonColumn());
            dataGridView1.Columns.Add(new DataGridViewDisableButtonColumn());
            //dataGridView1.Columns.Add(new DataGridViewDisableButtonColumn());

            dataGridView1.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;


            dataGridView1.Columns[ideNameCol].HeaderText = "IDE version";
            dataGridView1.Columns[ideInstalledCol].HeaderText = "IDE installed";
            dataGridView1.Columns[addinActionCol].HeaderText = "VS Extension\n(Add-in)";
            dataGridView1.Columns[toolbarInstalledCol].HeaderText = "Toolbar\nis integrated";
            dataGridView1.Columns[toolbarInsertActionCol].HeaderText = "Toolbar\nSetup Action";
            dataGridView1.Columns[toolbarImportActiondCol].HeaderText = "Toolbar\nSetup Action";
            dataGridView1.Columns[toolbarRemoveActiondCol].HeaderText = "Toolbar\nSetup Action";
            //dataGridView1.Columns[7].HeaderText = "Code Snippets\nSetup Action";

            dataGridView1.Columns[ideInstalledCol].Width = 90;
            dataGridView1.Columns[addinActionCol].Width = 90;
            dataGridView1.Columns[toolbarInstalledCol].Width = 90;
            dataGridView1.Columns[toolbarInsertActionCol].Width = 90;
            dataGridView1.Columns[toolbarImportActiondCol].Width = 90;
            dataGridView1.Columns[toolbarRemoveActiondCol].Width = 90;

            dataGridView1.RowCount = 8;

            dataGridView1.Rows[0].Cells[ideNameCol].Value = "VS 2005";
            dataGridView1.Rows[0].Cells[ideInstalledCol].Value = vsToolbar.IsIdeInstalled;
            dataGridView1.Rows[0].Cells[addinActionCol].Value = naCaption;
            dataGridView1.Rows[0].Cells[toolbarInstalledCol].Value = false;
            dataGridView1.Rows[0].Cells[toolbarInsertActionCol].Value = insertCaption;
            dataGridView1.Rows[0].Cells[toolbarImportActiondCol].Value = importCaption;
            dataGridView1.Rows[0].Cells[toolbarRemoveActiondCol].Value = removeCaption;


            //dataGridView1.Rows[0].Cells[7].Value = installCaption;
            //dataGridView1.Rows[0].Cells[7].Tag = vsCodeSnippet;

            dataGridView1.Rows[1].Cells[ideNameCol].Value = "VS 2005 Express";
            dataGridView1.Rows[1].Cells[ideInstalledCol].Value = vsExpressToolbar.IsIdeInstalled;
            dataGridView1.Rows[1].Cells[addinActionCol].Value = naCaption;
            dataGridView1.Rows[1].Cells[toolbarInstalledCol].Value = false;
            dataGridView1.Rows[1].Cells[toolbarInsertActionCol].Value = insertCaption;
            dataGridView1.Rows[1].Cells[toolbarImportActiondCol].Value = importCaption;
            dataGridView1.Rows[1].Cells[toolbarRemoveActiondCol].Value = removeCaption;
            //dataGridView1.Rows[1].Cells[7].Value = installCaption;
            //dataGridView1.Rows[1].Cells[7].Tag = vsExpressCodeSnippet;

            dataGridView1.Rows[2].Cells[ideNameCol].Value = "VS 2008";
            dataGridView1.Rows[2].Cells[ideInstalledCol].Value = vs9Toolbar.IsIdeInstalled;
            dataGridView1.Rows[2].Cells[addinActionCol].Value = naCaption;
            dataGridView1.Rows[2].Cells[toolbarInstalledCol].Value = vs9Toolbar.IsIdeInstalled;
            dataGridView1.Rows[2].Cells[toolbarInsertActionCol].Value = insertCaption;
            dataGridView1.Rows[2].Cells[toolbarImportActiondCol].Value = importCaption;
            dataGridView1.Rows[2].Cells[toolbarRemoveActiondCol].Value = removeCaption;
            //dataGridView1.Rows[2].Cells[7].Value = installCaption;
            //dataGridView1.Rows[2].Cells[7].Tag = vs9CodeSnippet;

            dataGridView1.Rows[3].Cells[ideNameCol].Value = "VS 2008 Express";
            dataGridView1.Rows[3].Cells[ideInstalledCol].Value = vsExpress9Toolbar.IsIdeInstalled;
            dataGridView1.Rows[3].Cells[addinActionCol].Value = naCaption;
            dataGridView1.Rows[3].Cells[toolbarInstalledCol].Value = false;
            dataGridView1.Rows[3].Cells[toolbarInsertActionCol].Value = insertCaption;
            dataGridView1.Rows[3].Cells[toolbarImportActiondCol].Value = importCaption;
            dataGridView1.Rows[3].Cells[toolbarRemoveActiondCol].Value = removeCaption;
            //dataGridView1.Rows[3].Cells[7].Value = installCaption;
            //dataGridView1.Rows[3].Cells[7].Tag = vsExpress9CodeSnippet;

            dataGridView1.Rows[4].Cells[ideNameCol].Value = "VS 2010";
            dataGridView1.Rows[4].Cells[ideInstalledCol].Value = vs10Toolbar.IsIdeInstalled;
            dataGridView1.Rows[4].Cells[addinActionCol].Value = installCaption;
            dataGridView1.Rows[4].Cells[addinActionCol].Tag = vs10Toolbar;
            dataGridView1.Rows[4].Cells[toolbarInstalledCol].Value = false;
            dataGridView1.Rows[4].Cells[toolbarInsertActionCol].Value = naCaption;
            dataGridView1.Rows[4].Cells[toolbarImportActiondCol].Value = naCaption;
            dataGridView1.Rows[4].Cells[toolbarRemoveActiondCol].Value = naCaption;
            //dataGridView1.Rows[4].Cells[7].Value = naCaption;

            dataGridView1.Rows[5].Cells[ideNameCol].Value = "VS 2010 Express";
            dataGridView1.Rows[5].Cells[ideInstalledCol].Value = vsExpress10Toolbar.IsIdeInstalled;
            dataGridView1.Rows[5].Cells[addinActionCol].Value = naCaption;
            dataGridView1.Rows[5].Cells[toolbarInstalledCol].Value = vsExpress10Toolbar.IsIdeInstalled;
            dataGridView1.Rows[5].Cells[toolbarInsertActionCol].Value = insertCaption;
            dataGridView1.Rows[5].Cells[toolbarImportActiondCol].Value = importCaption;
            dataGridView1.Rows[5].Cells[toolbarRemoveActiondCol].Value = removeCaption;
            //dataGridView1.Rows[5].Cells[7].Value = naCaption;
            //dataGridView1.Rows[5].Cells[7].Tag = vsExpress10CodeSnippet;

            dataGridView1.Rows[6].Cells[ideNameCol].Value = "VS 2012";
            dataGridView1.Rows[6].Cells[ideInstalledCol].Value = vs12Toolbar.IsIdeInstalled;
            dataGridView1.Rows[6].Cells[addinActionCol].Value = installCaption;
            dataGridView1.Rows[6].Cells[addinActionCol].Tag = vs12Toolbar;
            dataGridView1.Rows[6].Cells[toolbarInstalledCol].Value = false;
            dataGridView1.Rows[6].Cells[toolbarInsertActionCol].Value = naCaption;
            dataGridView1.Rows[6].Cells[toolbarImportActiondCol].Value = naCaption;
            dataGridView1.Rows[6].Cells[toolbarRemoveActiondCol].Value = naCaption;
            //dataGridView1.Rows[4].Cells[7].Value = naCaption;

            dataGridView1.Rows[7].Cells[ideNameCol].Value = "VS 2012 Express";
            dataGridView1.Rows[7].Cells[ideInstalledCol].Value = vsExpress12Toolbar.IsIdeInstalled;
            dataGridView1.Rows[7].Cells[addinActionCol].Value = naCaption;
            dataGridView1.Rows[7].Cells[toolbarInstalledCol].Value = vsExpress12Toolbar.IsIdeInstalled;
            dataGridView1.Rows[7].Cells[toolbarInsertActionCol].Value = insertCaption;
            dataGridView1.Rows[7].Cells[toolbarImportActiondCol].Value = importCaption;
            dataGridView1.Rows[7].Cells[toolbarRemoveActiondCol].Value = removeCaption;
            //dataGridView1.Rows[5].Cells[7].Value = naCaption;
            //dataGridView1.Rows[5].Cells[7].Tag = vsExpress10CodeSnippet;

            dataGridView1.BorderStyle = BorderStyle.None;
            dataGridView1.ShowCellToolTips = true;
            RefreshLayout();
        }

        void RefreshLayout()
        {
            CSSToolbar[] toolbars = new CSSToolbar[] { vsToolbar, vsExpressToolbar, vs9Toolbar, vsExpress9Toolbar, vs10Toolbar, vsExpress10Toolbar, vs12Toolbar, vsExpress12Toolbar };
            IVSCodeSnippet[] snippets = new IVSCodeSnippet[] { vsCodeSnippet, vsExpressCodeSnippet, vs9CodeSnippet, vsExpress9CodeSnippet, vs10CodeSnippet, vsExpress10CodeSnippet, vs12CodeSnippet, vsExpress12CodeSnippet };
            for (int i = 0; i < 8; i++)
            {
                CSSToolbar toolbar = toolbars[i];
                IVSCodeSnippet snippet = snippets[i];

                bool installed = toolbar.IsInstalled;
                
                dataGridView1.Rows[i].Cells[toolbarInstalledCol].Value = installed;
                (dataGridView1.Rows[i].Cells[toolbarInsertActionCol] as DataGridViewDisableButtonCell).Enabled = toolbar.IsIdeInstalled && !installed;
                (dataGridView1.Rows[i].Cells[toolbarImportActiondCol] as DataGridViewDisableButtonCell).Enabled = toolbar.IsIdeInstalled && !installed;
                (dataGridView1.Rows[i].Cells[toolbarRemoveActiondCol] as DataGridViewDisableButtonCell).Enabled = toolbar.IsIdeInstalled && installed && toolbar.IsRestoreAvailable;
                //(dataGridView1.Rows[i].Cells[7] as DataGridViewDisableButtonCell).Enabled = toolbar.IsIdeInstalled;
                //if (!(dataGridView1.Rows[i].Cells[7] as DataGridViewDisableButtonCell).Enabled)
                //    dataGridView1.Rows[i].Cells[7].Value = "";
                //else
                //    dataGridView1.Rows[i].Cells[7].Value = snippet.IsInstalled ? removeCaption : installCaption;

                for (int j = 0; j < 7; j++)
                    if (dataGridView1.Rows[i].Cells[j].Value.ToString() == naCaption && dataGridView1.Rows[i].Cells[j] is DataGridViewDisableButtonCell)
                        (dataGridView1.Rows[i].Cells[j] as DataGridViewDisableButtonCell).Enabled = false;

                dataGridView1.Rows[i].Cells[toolbarInsertActionCol].ToolTipText = "Insert CS-Script toolbar by modifying IDE settings (.vssettings file) directly.\nYou will be able to undo this action by pressing Remove button.";
                dataGridView1.Rows[i].Cells[toolbarImportActiondCol].ToolTipText = "Insert CS-Script toolbar by importing (custom .vssettings file). Safer but less reliable option.\nYou will be able to undo this action by pressing Remove button";
                //dataGridView1.Rows[i].Cells[6].ToolTipText = "Install CS-Script Code Snippet";
            }

            Refresh();
        }

        VSToolbar vsToolbar = new VSToolbar();
        VSEToolbar vsExpressToolbar = new VSEToolbar();
        VS9Toolbar vs9Toolbar = new VS9Toolbar();
        VSE9Toolbar vsExpress9Toolbar = new VSE9Toolbar();
        VS10Toolbar vs10Toolbar = new VS10Toolbar();
        VSE10Toolbar vsExpress10Toolbar = new VSE10Toolbar();
        VS12Toolbar vs12Toolbar = new VS12Toolbar();
        VSE12Toolbar vsExpress12Toolbar = new VSE12Toolbar();
        VSCodeSnippet vsCodeSnippet = new VSCodeSnippet();
        VSECodeSnippet vsExpressCodeSnippet = new VSECodeSnippet();
        VS9CodeSnippet vs9CodeSnippet = new VS9CodeSnippet();
        VSE9CodeSnippet vsExpress9CodeSnippet = new VSE9CodeSnippet();
        VS10CodeSnippet vs10CodeSnippet = new VS10CodeSnippet();
        VSE10CodeSnippet vsExpress10CodeSnippet = new VSE10CodeSnippet();
        VS10CodeSnippet vs12CodeSnippet = new VS10CodeSnippet();
        VSE10CodeSnippet vsExpress12CodeSnippet = new VSE10CodeSnippet();
        
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
            this.components = new System.ComponentModel.Container();
            this.dataGridView1 = new System.Windows.Forms.DataGridView();
            this.help = new System.Windows.Forms.Button();
            this.close = new System.Windows.Forms.Button();
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).BeginInit();
            this.SuspendLayout();
            // 
            // dataGridView1
            // 
            this.dataGridView1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.dataGridView1.BackgroundColor = System.Drawing.SystemColors.Control;
            this.dataGridView1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridView1.EditMode = System.Windows.Forms.DataGridViewEditMode.EditProgrammatically;
            this.dataGridView1.Location = new System.Drawing.Point(12, 12);
            this.dataGridView1.MultiSelect = false;
            this.dataGridView1.Name = "dataGridView1";
            this.dataGridView1.ReadOnly = true;
            this.dataGridView1.RowHeadersVisible = false;
            this.dataGridView1.Size = new System.Drawing.Size(645, 196);
            this.dataGridView1.TabIndex = 0;
            this.dataGridView1.CellClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dataGridView1_CellClick);
            this.dataGridView1.KeyDown += new System.Windows.Forms.KeyEventHandler(this.dataGridView1_KeyDown);
            // 
            // help
            // 
            this.help.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.help.Location = new System.Drawing.Point(344, 214);
            this.help.Name = "help";
            this.help.Size = new System.Drawing.Size(84, 27);
            this.help.TabIndex = 1;
            this.help.Text = "Help";
            this.help.Click += new System.EventHandler(this.help_Click);
            // 
            // close
            // 
            this.close.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.close.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.close.Location = new System.Drawing.Point(241, 214);
            this.close.Name = "close";
            this.close.Size = new System.Drawing.Size(84, 27);
            this.close.TabIndex = 1;
            this.close.Text = "Close";
            this.close.Click += new System.EventHandler(this.close_Click);
            // 
            // timer1
            // 
            this.timer1.Enabled = true;
            this.timer1.Interval = 5000;
            this.timer1.Tick += new System.EventHandler(this.timer1_Tick);
            // 
            // Form1
            // 
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
            this.CancelButton = this.close;
            this.ClientSize = new System.Drawing.Size(671, 253);
            this.Controls.Add(this.close);
            this.Controls.Add(this.help);
            this.Controls.Add(this.dataGridView1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
            this.KeyPreview = true;
            this.Name = "Form1";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "CS-Script Visual Studio Add-ins";
            this.TopMost = true;
            this.Load += new System.EventHandler(this.Form1_Load);
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.Form1_KeyDown);
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).EndInit();
            this.ResumeLayout(false);

        }
        #endregion

        private void dataGridView1_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (dataGridView1.Rows[e.RowIndex].Cells[e.ColumnIndex] is DataGridViewDisableButtonCell)
            {
                DataGridViewDisableButtonCell buttonCell = (dataGridView1.Rows[e.RowIndex].Cells[e.ColumnIndex] as DataGridViewDisableButtonCell);

                if (buttonCell.Enabled)
                {
                    Cursor.Current = Cursors.WaitCursor;
                    if (buttonCell.Tag is VSCodeSnippet)
                    {
                        if (dataGridView1.Rows[e.RowIndex].Cells[e.ColumnIndex].Value.ToString() == removeCaption)
                            (buttonCell.Tag as VSCodeSnippet).Remove();
                        else
                            (buttonCell.Tag as VSCodeSnippet).Install();
                    }
                    else if (buttonCell.Tag is VSECodeSnippet)
                    {
                        if (dataGridView1.Rows[e.RowIndex].Cells[e.ColumnIndex].Value.ToString() == removeCaption)
                            (buttonCell.Tag as VSECodeSnippet).Remove();
                        else
                            (buttonCell.Tag as VSECodeSnippet).Install();
                    }
                    else if (buttonCell.Tag is VSE9CodeSnippet)
                    {
                        if (dataGridView1.Rows[e.RowIndex].Cells[e.ColumnIndex].Value.ToString() == removeCaption)
                            (buttonCell.Tag as VSE9CodeSnippet).Remove();
                        else
                            (buttonCell.Tag as VSE9CodeSnippet).Install();
                    }
                    else if (buttonCell.Tag is VS10Toolbar)
                    {
                        (buttonCell.Tag as VS10Toolbar).Install();
                    }
                    else
                    {
                        CSSToolbar toolbar;
                        if (e.RowIndex == 0)
                            toolbar = vsToolbar;
                        else if (e.RowIndex == 1)
                            toolbar = vsExpressToolbar;
                        else if (e.RowIndex == 2)
                            toolbar = vs9Toolbar;
                        else if (e.RowIndex == 3)
                            toolbar = vsExpress9Toolbar;
                        else if (e.RowIndex == 4)
                            toolbar = vs10Toolbar;
                        else
                            toolbar = vsExpress10Toolbar;

                        if (dataGridView1.Rows[e.RowIndex].Cells[e.ColumnIndex].Value.ToString() == removeCaption
                            && toolbar.IsRestoreAvailable)
                        {
                            toolbar.RestoreOldSettings();
                        }
                        else if (dataGridView1.Rows[e.RowIndex].Cells[e.ColumnIndex].Value.ToString() == insertCaption
                            && !toolbar.IsInstalled)
                        {
                            toolbar.Install();
                        }
                        else if (dataGridView1.Rows[e.RowIndex].Cells[e.ColumnIndex].Value.ToString() == importCaption
                            && !toolbar.IsInstalled)
                        {
                            toolbar.Import();
                        }
                    }
                    RefreshLayout();

                    Cursor.Current = Cursors.Default;
                }
            }
        }
        private void help_Click(object sender, EventArgs e)
        {
            string homeDir = Environment.GetEnvironmentVariable("CSSCRIPT_DIR");
            if (homeDir != null)
            {
                Help.ShowHelp(this, Path.Combine(homeDir, @"Docs\Help\CSScript.chm"), "ConfigUtils.html#_vs_integration");
            }
        }
        private void close_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F5)
                RefreshLayout();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            RefreshLayout();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            timer1.Enabled = true;
        }

        private void dataGridView1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
                Close();
        }
    }

    class Script
    {
        const string usage = "Usage: cscscript VSIntegration ...\nCS-Script Visual Studio toolbar configuration console.\n";

        static public void Main(string[] args)
        {
            if (args.Length == 1 && (args[0] == "?" || args[0] == "/?" || args[0] == "-?" || args[0].ToLower() == "help"))
            {
                Console.WriteLine(usage);
            }
            else
            {
                Application.EnableVisualStyles();
                Application.Run(new Form1());
            }
        }
    }

    public class DataGridViewDisableButtonColumn : DataGridViewButtonColumn
    {
        public DataGridViewDisableButtonColumn()
        {
            this.CellTemplate = new DataGridViewDisableButtonCell();
        }
    }

    public class DataGridViewDisableButtonCell : DataGridViewButtonCell
    {
        private bool enabledValue;
        public bool Enabled
        {
            get { return enabledValue; }
            set { enabledValue = value; }
        }

        // Override the Clone method so that the Enabled property is copied.
        public override object Clone()
        {
            DataGridViewDisableButtonCell cell = (DataGridViewDisableButtonCell)base.Clone();
            cell.Enabled = this.Enabled;
            return cell;
        }

        // By default, enable the button cell.
        public DataGridViewDisableButtonCell()
        {
            this.enabledValue = true;
        }

        protected override void Paint(Graphics graphics,
            Rectangle clipBounds, Rectangle cellBounds, int rowIndex,
            DataGridViewElementStates elementState, object value,
            object formattedValue, string errorText,
            DataGridViewCellStyle cellStyle,
            DataGridViewAdvancedBorderStyle advancedBorderStyle,
            DataGridViewPaintParts paintParts)
        {


            // The button cell is disabled, so paint the border,  
            // background, and disabled button for the cell.
            if (!this.enabledValue)
            {
                // Draw the cell background, if specified.
                if ((paintParts & DataGridViewPaintParts.Background) ==
                    DataGridViewPaintParts.Background)
                {
                    SolidBrush cellBackground =
                    new SolidBrush(cellStyle.BackColor);
                    graphics.FillRectangle(cellBackground, cellBounds);
                    cellBackground.Dispose();
                }

                // Draw the cell borders, if specified.
                if ((paintParts & DataGridViewPaintParts.Border) ==
                    DataGridViewPaintParts.Border)
                {
                    PaintBorder(graphics, clipBounds, cellBounds, cellStyle,
                        advancedBorderStyle);
                }

                // Calculate the area in which to draw the button.
                Rectangle buttonArea = cellBounds;
                Rectangle buttonAdjustment =
                    this.BorderWidths(advancedBorderStyle);
                buttonArea.X += buttonAdjustment.X;
                buttonArea.Y += buttonAdjustment.Y;
                buttonArea.Height -= buttonAdjustment.Height;
                buttonArea.Width -= buttonAdjustment.Width;

                // Draw the disabled button.                
                ButtonRenderer.DrawButton(graphics, buttonArea, PushButtonState.Disabled);

                // Draw the disabled button text. 
                if (this.FormattedValue is String)
                {
                    TextRenderer.DrawText(graphics,
                        (string)this.FormattedValue,
                        this.DataGridView.Font,
                        buttonArea, enabledValue ? SystemColors.ControlText : SystemColors.GrayText);
                }
            }
            else
            {
                // The button cell is enabled, so let the base class 
                // handle the painting.
                base.Paint(graphics, clipBounds, cellBounds, rowIndex,
                    elementState, value, formattedValue, errorText,
                   cellStyle, advancedBorderStyle, paintParts);
            }
        }
    }
}
