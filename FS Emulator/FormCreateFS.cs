using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FS_Emulator
{
    public partial class FormFormatFS : Form
    {
        public FormFormatFS()
        {
            InitializeComponent();
        }

		public FormFormatFS(string pathToSave):this()
		{
			this.Text = "Форматировать";
			this.PathToSaveTB.Text = pathToSave;
			this.BtSelectPathToSave.Enabled = false;
		}

        int FSCapacity = 4;
        FSClusterSize clusterSize;
        string pathToSave = @"E:\ForFS\FS";

        private void FormCreateFS_Load(object sender, EventArgs e)
        {
            cbox_ClusterSize.DataSource = Enum.GetValues(typeof(FSClusterSize));
            
            cbox_ClusterSize.SelectedIndex = (int)FSClusterSize._1KB;
        }

        private void TBSelectPathToSave_Click(object sender, EventArgs e)
        {
			var dialog = new SaveFileDialog
			{
				CheckPathExists = true,
				InitialDirectory = @"E:\ForFS",
			};
			var result = dialog.ShowDialog();

            if(result == DialogResult.OK)
            {
                PathToSaveTB.Text = dialog.FileName;
            }            

        }

        private void numericUpDown1_ValueChanged(object sender, EventArgs e)
        {
            FSCapacity = (int)(sender as NumericUpDown).Value;
        }

        private void cbox_ClusterSize_SelectedValueChanged(object sender, EventArgs e)
        {
            clusterSize = (FSClusterSize)(sender as ComboBox).SelectedValue;
        }

        private void BtOK_Click(object sender, EventArgs e)
        {
            new FSTools.FS().FormatOrCreate(pathToSave, FSCapacity, (int)Enum.Parse(typeof(FSClusterSize_CorrespondsBytes), cbox_ClusterSize.SelectedValue.ToString()));
			MessageBox.Show("Готово!");
		}

        private void PathToSaveTB_TextChanged(object sender, EventArgs e)
        {
            pathToSave = (sender as TextBox).Text;
        }

	}
}
