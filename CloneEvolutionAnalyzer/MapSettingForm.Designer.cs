namespace CloneEvolutionAnalyzer
{
    partial class MapSettingForm
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
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.textBox2 = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.button_MapBetweenVersions = new System.Windows.Forms.Button();
            this.button_Clear = new System.Windows.Forms.Button();
            this.button_Cancel1 = new System.Windows.Forms.Button();
            this.label_Warning1 = new System.Windows.Forms.Label();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.label_Warning2 = new System.Windows.Forms.Label();
            this.button_Cancel2 = new System.Windows.Forms.Button();
            this.button_MapAll = new System.Windows.Forms.Button();
            this.label4 = new System.Windows.Forms.Label();
            this.checkBox_blocks = new System.Windows.Forms.CheckBox();
            this.checkBox_functions = new System.Windows.Forms.CheckBox();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.Location = new System.Drawing.Point(31, 64);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(101, 30);
            this.label1.TabIndex = 0;
            this.label1.Text = "SourceSysVersion(*-withCRD.xml)";
            // 
            // label2
            // 
            this.label2.Location = new System.Drawing.Point(23, 103);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(135, 30);
            this.label2.TabIndex = 1;
            this.label2.Text = "DestinationSysVersion   (*-withCRD.xml)";
            // 
            // textBox1
            // 
            this.textBox1.Location = new System.Drawing.Point(168, 64);
            this.textBox1.Name = "textBox1";
            this.textBox1.Size = new System.Drawing.Size(288, 21);
            this.textBox1.TabIndex = 2;
            // 
            // textBox2
            // 
            this.textBox2.Location = new System.Drawing.Point(168, 106);
            this.textBox2.Name = "textBox2";
            this.textBox2.Size = new System.Drawing.Size(288, 21);
            this.textBox2.TabIndex = 3;
            this.textBox2.TextChanged += new System.EventHandler(this.textBox2_TextChanged);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(22, 35);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(305, 12);
            this.label3.TabIndex = 4;
            this.label3.Text = "Check from the left tree to select versions to map";
            // 
            // button_MapBetweenVersions
            // 
            this.button_MapBetweenVersions.Location = new System.Drawing.Point(156, 158);
            this.button_MapBetweenVersions.Name = "button_MapBetweenVersions";
            this.button_MapBetweenVersions.Size = new System.Drawing.Size(188, 23);
            this.button_MapBetweenVersions.TabIndex = 5;
            this.button_MapBetweenVersions.Text = "MapBetweenVersions";
            this.button_MapBetweenVersions.UseVisualStyleBackColor = true;
            this.button_MapBetweenVersions.Click += new System.EventHandler(this.button_MapBetweenVersions_Click);
            // 
            // button_Clear
            // 
            this.button_Clear.Location = new System.Drawing.Point(40, 158);
            this.button_Clear.Name = "button_Clear";
            this.button_Clear.Size = new System.Drawing.Size(75, 23);
            this.button_Clear.TabIndex = 6;
            this.button_Clear.Text = "Clear";
            this.button_Clear.UseVisualStyleBackColor = true;
            this.button_Clear.Click += new System.EventHandler(this.button_Clear_Click);
            // 
            // button_Cancel1
            // 
            this.button_Cancel1.Location = new System.Drawing.Point(381, 158);
            this.button_Cancel1.Name = "button_Cancel1";
            this.button_Cancel1.Size = new System.Drawing.Size(75, 23);
            this.button_Cancel1.TabIndex = 7;
            this.button_Cancel1.Text = "Cancel";
            this.button_Cancel1.UseVisualStyleBackColor = true;
            this.button_Cancel1.Click += new System.EventHandler(this.button_Cancel_Click);
            // 
            // label_Warning1
            // 
            this.label_Warning1.AutoSize = true;
            this.label_Warning1.Location = new System.Drawing.Point(25, 205);
            this.label_Warning1.Name = "label_Warning1";
            this.label_Warning1.Size = new System.Drawing.Size(71, 12);
            this.label_Warning1.TabIndex = 8;
            this.label_Warning1.Text = "WarningZone";
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.button_MapBetweenVersions);
            this.groupBox1.Controls.Add(this.label_Warning1);
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Controls.Add(this.button_Cancel1);
            this.groupBox1.Controls.Add(this.label2);
            this.groupBox1.Controls.Add(this.button_Clear);
            this.groupBox1.Controls.Add(this.textBox1);
            this.groupBox1.Controls.Add(this.textBox2);
            this.groupBox1.Controls.Add(this.label3);
            this.groupBox1.Location = new System.Drawing.Point(12, 12);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(495, 241);
            this.groupBox1.TabIndex = 9;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "MapBetweenVersions";
            this.groupBox1.Enter += new System.EventHandler(this.groupBox1_Enter);
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.label_Warning2);
            this.groupBox2.Controls.Add(this.button_Cancel2);
            this.groupBox2.Controls.Add(this.button_MapAll);
            this.groupBox2.Controls.Add(this.label4);
            this.groupBox2.Controls.Add(this.checkBox_blocks);
            this.groupBox2.Controls.Add(this.checkBox_functions);
            this.groupBox2.Location = new System.Drawing.Point(12, 277);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(495, 181);
            this.groupBox2.TabIndex = 10;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "MapAll";
            // 
            // label_Warning2
            // 
            this.label_Warning2.AutoSize = true;
            this.label_Warning2.Location = new System.Drawing.Point(25, 150);
            this.label_Warning2.Name = "label_Warning2";
            this.label_Warning2.Size = new System.Drawing.Size(71, 12);
            this.label_Warning2.TabIndex = 9;
            this.label_Warning2.Text = "WarningZone";
            // 
            // button_Cancel2
            // 
            this.button_Cancel2.Location = new System.Drawing.Point(280, 99);
            this.button_Cancel2.Name = "button_Cancel2";
            this.button_Cancel2.Size = new System.Drawing.Size(125, 23);
            this.button_Cancel2.TabIndex = 4;
            this.button_Cancel2.Text = "Cancel";
            this.button_Cancel2.UseVisualStyleBackColor = true;
            this.button_Cancel2.Click += new System.EventHandler(this.button_Cancel_Click);
            // 
            // button_MapAll
            // 
            this.button_MapAll.Location = new System.Drawing.Point(89, 99);
            this.button_MapAll.Name = "button_MapAll";
            this.button_MapAll.Size = new System.Drawing.Size(117, 23);
            this.button_MapAll.TabIndex = 3;
            this.button_MapAll.Text = "MapAll";
            this.button_MapAll.UseVisualStyleBackColor = true;
            this.button_MapAll.Click += new System.EventHandler(this.button_MapAll_Click);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(33, 49);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(71, 12);
            this.label4.TabIndex = 2;
            this.label4.Text = "Granularity";
            // 
            // checkBox_blocks
            // 
            this.checkBox_blocks.AutoSize = true;
            this.checkBox_blocks.Location = new System.Drawing.Point(309, 49);
            this.checkBox_blocks.Name = "checkBox_blocks";
            this.checkBox_blocks.Size = new System.Drawing.Size(60, 16);
            this.checkBox_blocks.TabIndex = 1;
            this.checkBox_blocks.Text = "blocks";
            this.checkBox_blocks.UseVisualStyleBackColor = true;
            // 
            // checkBox_functions
            // 
            this.checkBox_functions.AutoSize = true;
            this.checkBox_functions.Location = new System.Drawing.Point(177, 48);
            this.checkBox_functions.Name = "checkBox_functions";
            this.checkBox_functions.Size = new System.Drawing.Size(78, 16);
            this.checkBox_functions.TabIndex = 0;
            this.checkBox_functions.Text = "functions";
            this.checkBox_functions.UseVisualStyleBackColor = true;
            // 
            // MapSettingForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(533, 470);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.Name = "MapSettingForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "MapSettings";
            this.TopMost = true;
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MappingSettingForm_FormClosing);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Button button_MapBetweenVersions;
        private System.Windows.Forms.Button button_Clear;
        private System.Windows.Forms.Button button_Cancel1;
        internal System.Windows.Forms.Label label_Warning1;
        internal System.Windows.Forms.TextBox textBox1;
        internal System.Windows.Forms.TextBox textBox2;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Button button_Cancel2;
        private System.Windows.Forms.Button button_MapAll;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.CheckBox checkBox_blocks;
        private System.Windows.Forms.CheckBox checkBox_functions;
        internal System.Windows.Forms.Label label_Warning2;
    }
}