namespace CloneEvolutionAnalyzer
{
    partial class ShowCodeForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
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
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.label_fileName = new System.Windows.Forms.Label();
            this.splitContainer2 = new System.Windows.Forms.SplitContainer();
            this.richTextBox_lineNo = new CloneEvolutionAnalyzer.RichTextBoxEx();
            this.richTextBox_Code = new CloneEvolutionAnalyzer.RichTextBoxEx();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer2)).BeginInit();
            this.splitContainer2.Panel1.SuspendLayout();
            this.splitContainer2.Panel2.SuspendLayout();
            this.splitContainer2.SuspendLayout();
            this.SuspendLayout();
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(0, 0);
            this.splitContainer1.Name = "splitContainer1";
            this.splitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.label_fileName);
            this.splitContainer1.Panel1.Paint += new System.Windows.Forms.PaintEventHandler(this.splitContainer1_Panel1_Paint);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.splitContainer2);
            this.splitContainer1.Size = new System.Drawing.Size(586, 416);
            this.splitContainer1.SplitterDistance = 25;
            this.splitContainer1.TabIndex = 0;
            // 
            // label_fileName
            // 
            this.label_fileName.AutoSize = true;
            this.label_fileName.Location = new System.Drawing.Point(12, 9);
            this.label_fileName.Name = "label_fileName";
            this.label_fileName.Size = new System.Drawing.Size(0, 12);
            this.label_fileName.TabIndex = 0;
            // 
            // splitContainer2
            // 
            this.splitContainer2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer2.Location = new System.Drawing.Point(0, 0);
            this.splitContainer2.Name = "splitContainer2";
            // 
            // splitContainer2.Panel1
            // 
            this.splitContainer2.Panel1.Controls.Add(this.richTextBox_lineNo);
            // 
            // splitContainer2.Panel2
            // 
            this.splitContainer2.Panel2.Controls.Add(this.richTextBox_Code);
            this.splitContainer2.Size = new System.Drawing.Size(586, 387);
            this.splitContainer2.SplitterDistance = 44;
            this.splitContainer2.TabIndex = 0;
            // 
            // richTextBox_lineNo
            // 
            this.richTextBox_lineNo.Dock = System.Windows.Forms.DockStyle.Fill;
            this.richTextBox_lineNo.Location = new System.Drawing.Point(0, 0);
            this.richTextBox_lineNo.Name = "richTextBox_lineNo";
            this.richTextBox_lineNo.Size = new System.Drawing.Size(44, 387);
            this.richTextBox_lineNo.TabIndex = 0;
            this.richTextBox_lineNo.Text = "";
            this.richTextBox_lineNo.WordWrap = false;
            // 
            // richTextBox_Code
            // 
            this.richTextBox_Code.Dock = System.Windows.Forms.DockStyle.Fill;
            this.richTextBox_Code.Location = new System.Drawing.Point(0, 0);
            this.richTextBox_Code.Name = "richTextBox_Code";
            this.richTextBox_Code.Size = new System.Drawing.Size(538, 387);
            this.richTextBox_Code.TabIndex = 0;
            this.richTextBox_Code.Text = "";
            this.richTextBox_Code.WordWrap = false;
            this.richTextBox_Code.TextChanged += new System.EventHandler(this.richTextBox_Code_TextChanged);
            // 
            // ShowCodeForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(586, 416);
            this.Controls.Add(this.splitContainer1);
            this.Name = "ShowCodeForm";
            this.Text = "ShowCode";
            this.Load += new System.EventHandler(this.ShowCodeForm_Load);
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel1.PerformLayout();
            this.splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.splitContainer2.Panel1.ResumeLayout(false);
            this.splitContainer2.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer2)).EndInit();
            this.splitContainer2.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.SplitContainer splitContainer2;
        //private System.Windows.Forms.RichTextBox richTextBox_lineNo;
        //private System.Windows.Forms.RichTextBox richTextBox_Code;
        private RichTextBoxEx richTextBox_lineNo;
        private RichTextBoxEx richTextBox_Code;
        private System.Windows.Forms.Label label_fileName;

    }
}