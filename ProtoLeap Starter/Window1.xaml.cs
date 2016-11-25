/* This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>.
    */
using System.Collections.Generic;
using System;
using System.IO;
using System.Windows;

namespace ProtoLeap_Starter
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class Window1 : Window
    {
        string directpath = Path.GetDirectoryName(System.AppDomain.CurrentDomain.BaseDirectory);
        Dictionary<string, string> TempStoN = new Dictionary<string, string>();

        public Window1(string SN, string ID)
        {
            InitializeComponent();
            DeviceID.Text = ID;
            DeviceSN.Text = SN;

            var fr = File.Open("SerialNumber.txt", FileMode.OpenOrCreate, FileAccess.Read);
            var SR = new StreamReader(fr);

            using (SR)
            {
                string line;
                while ((line = SR.ReadLine()) != null)
                {
                    string[] parts = line.Split(',');
                    if (parts.Length > 1)
                    {
                        TempStoN.Add(parts[0], parts[1]);
                    }
                }
            }
            SR.Close();
        }

        private void BClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void BAdd_Click(object sender, RoutedEventArgs e)
        {
            var fw = File.Open("SerialNumber.txt", FileMode.Append, FileAccess.Write);
            var SW = new StreamWriter(fw);

            if (DeviceID.Text.Length < 1 | DeviceSN.Text.Length < 10)
            {
                Status1.Text = "Device ID or SN missing or too short!";
                Status2.Text = "";
            }
            else if (!(TempStoN.ContainsKey(DeviceSN.Text) | TempStoN.ContainsValue(DeviceID.Text)))
            {
                Status1.Text = "Device ID: " + DeviceID.Text + ", Device SN: " + DeviceSN.Text;
                Status2.Text = "added successfully!";
                TempStoN.Add(DeviceSN.Text, DeviceID.Text);
                SW.WriteLine(DeviceSN.Text + "," + DeviceID.Text);
            }
            else
            {
                Status1.Text = "Device ID or SN already exists!";
                Status2.Text = "";
            }
            SW.Close();
        }

        private void BDelete_Click(object sender, RoutedEventArgs e)
        {
            string tempFile = Path.GetTempFileName();
            var fw = File.Open("SerialNumber.txt", FileMode.Truncate, FileAccess.Write);
            var SW = new StreamWriter(fw);

            if (!(TempStoN.ContainsKey(DeviceSN.Text) | TempStoN.ContainsValue(DeviceID.Text)))
            {
                Status1.Text = "This Device is not currently stored!";
                Status2.Text = "";
            }
            else
            {
                Status1.Text = "Device ID: " + DeviceID.Text + ", Device SN: " + DeviceSN.Text;
                Status2.Text = "deleted successfully!";
                TempStoN.Remove(DeviceSN.Text);
                foreach (var entry in TempStoN)
                    SW.WriteLine(entry.Key + "," + entry.Value);

            }
            SW.Close();

        }
    }
}
