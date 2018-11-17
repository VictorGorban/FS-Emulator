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
    public partial class FormCreateFS : Form
    {
        public FormCreateFS()
        {
            InitializeComponent();
        }

        int FSCapacity = 4;
        FSClusterSize clusterSize = FSClusterSize.KB4;

        private void FormCreateFS_Load(object sender, EventArgs e)
        {
            cbox_ClusterSize.DataSource = Enum.GetValues(typeof(FSClusterSize));
            
            cbox_ClusterSize.SelectedIndex = (int)FSClusterSize.KB4;
        }

        private void TBSelectPathToSave_Click(object sender, EventArgs e)
        {
            var dialog = new SaveFileDialog();
            dialog.ShowDialog();

        }

        private void numericUpDown1_ValueChanged(object sender, EventArgs e)
        {
            FSCapacity = (int)(sender as NumericUpDown).Value;
        }

        private void cbox_ClusterSize_SelectedValueChanged(object sender, EventArgs e)
        {
            clusterSize = (FSClusterSize)(sender as ComboBox).SelectedValue;
        }


        // Ok, it's time to rest and next to process real creating my FS
    }
}
