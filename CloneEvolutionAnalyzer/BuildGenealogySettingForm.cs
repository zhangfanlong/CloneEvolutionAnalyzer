using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace CloneEvolutionAnalyzer
{
    public partial class BuildGenealogySettingForm : Form
    {
        public BuildGenealogySettingForm()
        {
            InitializeComponent();
        }

        private void button_Cancel_Click(object sender, EventArgs e)
        {
            this.Dispose();
        }

        private void button_Build_Click(object sender, EventArgs e)
        {
            if (!this.checkBox_blocks.Checked && !this.checkBox_functions.Checked)
            {
                this.label_Warning.ForeColor = Color.Red;
                this.label_Warning.Text = "Please check on at least one granularity type!";
                return;
            }
            this.Close();
            if (this.checkBox_blocks.Checked)
            { 
                CloneGenealogy.mapFileCollection = AdjacentVersionMapping.GetMapFileCollection(true);
                CloneGenealogy.BuildAndSaveAll(CloneGenealogy.mapFileCollection);
                MessageBox.Show("Building Genealogy on Blocks level FINISHED!");
            }
            if (this.checkBox_functions.Checked)
            { 
                CloneGenealogy.mapFileCollection = AdjacentVersionMapping.GetMapFileCollection(false);
                CloneGenealogy.BuildAndSaveAll(CloneGenealogy.mapFileCollection);
                MessageBox.Show("Building Genealogy on Functions level FINISHED!");
            }
        }


    }
}
