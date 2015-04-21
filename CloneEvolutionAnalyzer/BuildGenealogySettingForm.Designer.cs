namespace CloneEvolutionAnalyzer
{
    partial class BuildGenealogySettingForm
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
            this.checkBox_blocks = new System.Windows.Forms.CheckBox();
            this.checkBox_functions = new System.Windows.Forms.CheckBox();
            this.button_Build = new System.Windows.Forms.Button();
            this.button_Cancel = new System.Windows.Forms.Button();
            this.label_Warning = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(30, 36);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(119, 12);
            this.label1.TabIndex = 0;
            this.label1.Text = "Select Granularity:";
            // 
            // checkBox_blocks
            // 
            this.checkBox_blocks.AutoSize = true;
            this.checkBox_blocks.Location = new System.Drawing.Point(77, 79);
            this.checkBox_blocks.Name = "checkBox_blocks";
            this.checkBox_blocks.Size = new System.Drawing.Size(60, 16);
            this.checkBox_blocks.TabIndex = 1;
            this.checkBox_blocks.Text = "blocks";
            this.checkBox_blocks.UseVisualStyleBackColor = true;
            // 
            // checkBox_functions
            // 
            this.checkBox_functions.AutoSize = true;
            this.checkBox_functions.Location = new System.Drawing.Point(217, 79);
            this.checkBox_functions.Name = "checkBox_functions";
            this.checkBox_functions.Size = new System.Drawing.Size(78, 16);
            this.checkBox_functions.TabIndex = 2;
            this.checkBox_functions.Text = "functions";
            this.checkBox_functions.UseVisualStyleBackColor = true;
            // 
            // button_Build
            // 
            this.button_Build.Location = new System.Drawing.Point(65, 114);
            this.button_Build.Name = "button_Build";
            this.button_Build.Size = new System.Drawing.Size(75, 23);
            this.button_Build.TabIndex = 3;
            this.button_Build.Text = "Build";
            this.button_Build.UseVisualStyleBackColor = true;
            this.button_Build.Click += new System.EventHandler(this.button_Build_Click);
            // 
            // button_Cancel
            // 
            this.button_Cancel.Location = new System.Drawing.Point(220, 114);
            this.button_Cancel.Name = "button_Cancel";
            this.button_Cancel.Size = new System.Drawing.Size(75, 23);
            this.button_Cancel.TabIndex = 4;
            this.button_Cancel.Text = "Cancel";
            this.button_Cancel.UseVisualStyleBackColor = true;
            this.button_Cancel.Click += new System.EventHandler(this.button_Cancel_Click);
            // 
            // label_Warning
            // 
            this.label_Warning.AutoSize = true;
            this.label_Warning.Location = new System.Drawing.Point(30, 164);
            this.label_Warning.Name = "label_Warning";
            this.label_Warning.Size = new System.Drawing.Size(71, 12);
            this.label_Warning.TabIndex = 5;
            this.label_Warning.Text = "WarningZone";
            // 
            // BuildGenealogySettingForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(399, 219);
            this.Controls.Add(this.label_Warning);
            this.Controls.Add(this.button_Cancel);
            this.Controls.Add(this.button_Build);
            this.Controls.Add(this.checkBox_functions);
            this.Controls.Add(this.checkBox_blocks);
            this.Controls.Add(this.label1);
            this.Name = "BuildGenealogySettingForm";
            this.Text = "BuildGenealogySettingForm";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.CheckBox checkBox_blocks;
        private System.Windows.Forms.CheckBox checkBox_functions;
        private System.Windows.Forms.Button button_Build;
        private System.Windows.Forms.Button button_Cancel;
        private System.Windows.Forms.Label label_Warning;
    }
}