﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Ninjacrab.PersistentWindows.Common
{
    public partial class LayoutProfile : Form
    {
        public LayoutProfile()
        {
            InitializeComponent();
        }

        private void ProfileName_TextChanged(object sender, EventArgs e)
        {

        }

        private void AddProfile_Click(object sender, EventArgs e)
        {

        }

        private void SwitchProfile_Click(object sender, EventArgs e)
        {

        }

        private void DeleteProfile_Click(object sender, EventArgs e)
        {

        }

        private void ProfileList_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void ProfileList_KeyDown(object sender, KeyEventArgs e)
        {
            //if (sender == ProfileList)
            if (e.KeyValue == (int)Keys.Delete)
            {
            }
        }

        private void LayoutProfile_Load(object sender, EventArgs e)
        {
#if DEBUG 
            this.ProfileList.Items.Insert(0, "default");
#endif

        }

        private void Close_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
