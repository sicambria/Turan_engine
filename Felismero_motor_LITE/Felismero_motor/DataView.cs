/***************************************************************************
 *                                                                         *
 *   This program is free software; you can redistribute it and/or modify  *
 *   it under the terms of the GNU General Public License as published by  *
 *   the Free Software Foundation; either version 2 of the License, or     *
 *   (at your option) any later version.                                   *
 *                                                                         *
 ***************************************************************************/

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace Felismero_motor
{
    public partial class DataView : Form
    {
        public DataView()
        {
            InitializeComponent();
        }

        public void SetData(string data)
        {
            textBox1.Text = data;
        }

        public void SetFormName(string formname)
        {
            this.Text = formname;
        }
    }
}
