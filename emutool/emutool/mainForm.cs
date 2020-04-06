using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Windows.Forms;

namespace emutool
{
    public partial class MainForm : Form
    {
        List<Amiibo> CurrentAmiibo;

        WebClient WC = new WebClient();

        public MainForm()
        {
            InitializeComponent();

            CurrentAmiibo = new List<Amiibo>();

            if (AmiiboAPI.GetAllAmiibos())
            {
                toolStripStatusLabel1.Text = "Amiibo API was accessed. Amiibo list was loaded.";

                if (AmiiboAPI.AmiiboSeries.Any())
                {
                    foreach (string amiiboSerie in AmiiboAPI.AmiiboSeries)
                        comboBox1.Items.Add(amiiboSerie);

                    comboBox1.SelectedIndex = 0;
                }
            }
            else
            {
                toolStripStatusLabel1.Text  = "Unable to download amiibo list from amiibo API.";
                toolStripStatusLabel1.Image = Properties.Resources.cancel;

                groupBox1.Enabled = false;
                groupBox2.Enabled = false;
                groupBox3.Enabled = false;
            }


        }

        private void ComboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            comboBox2.Items.Clear();
            CurrentAmiibo.Clear();

            if (AmiiboAPI.AllAmiibo.Any())
            {
                foreach (Amiibo amiibo in AmiiboAPI.AllAmiibo)
                    if (amiibo.SeriesName == comboBox1.Text)
                        CurrentAmiibo.Add(amiibo);

                CurrentAmiibo.Sort(delegate (Amiibo A, Amiibo B) { return A.AmiiboName.CompareTo(B.AmiiboName); });

                comboBox2.Items.AddRange(CurrentAmiibo.ToArray());

                comboBox2.SelectedIndex = 0;
            }
            /*
            if(CurrentAmiibo[0].SeriesName == "Animal Crossing")
            {
                foreach (Amiibo a in CurrentAmiibo)
                    if (!System.IO.File.Exists("Animal Crossing\\" + a.CharacterName.Replace("/", "-") + a.ImageURL.Substring(a.ImageURL.LastIndexOf('.'))))
                        WC.DownloadFile(a.ImageURL, "Animal Crossing\\" + a.CharacterName.Replace("/","-") + a.ImageURL.Substring(a.ImageURL.LastIndexOf('.')));

            }//*/
        }

        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            pictureBox1.ImageLocation = CurrentAmiibo[comboBox2.SelectedIndex].ImageURL;
            textBox1.Text = CurrentAmiibo[comboBox2.SelectedIndex].AmiiboName;
        }

        private void Button1_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(textBox1.Text))
            {
                MessageBox.Show("No amiibo name was specified.", Text, MessageBoxButtons.OK, MessageBoxIcon.Error);

                return;
            }

            string emuiiboDir = "", driveName = "";

            if (DriveInfo.GetDrives().Any())
            {
                foreach (DriveInfo driveInfo in DriveInfo.GetDrives())
                {
                    if (driveInfo.IsReady)
                    {
                        if (Directory.Exists(Path.Combine(driveInfo.Name, Path.Combine("emuiibo", "amiibo"))))
                            emuiiboDir = Path.Combine(driveInfo.Name, Path.Combine("emuiibo", "amiibo"));
                        else if (Directory.Exists(Path.Combine(driveInfo.Name, "emuiibo")))
                        {
                            Directory.CreateDirectory(Path.Combine(driveInfo.Name, Path.Combine("emuiibo", "amiibo")));
                            emuiiboDir = Path.Combine(driveInfo.Name, Path.Combine("emuiibo", "amiibo"));
                        }

                        if (emuiiboDir != "") //this string cannot be null
                        {
                            driveName = driveInfo.VolumeLabel;
                            break;
                        }
                    }
                }
            }

            
            string amiiboDir =  "";
            bool doBrowser = true;

            if (driveName != "" && MessageBox.Show($"Emuiibo directory was found on drive '{driveName}'\r\nWould you like to use a different directory?", Text, MessageBoxButtons.YesNo, MessageBoxIcon.Information) != DialogResult.Yes)
                doBrowser = false;

            if(doBrowser)
            {

                FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog
                {
                    Description = "Select root directory to generate the virtual amiibo on",
                    ShowNewFolderButton = false,
                    SelectedPath = emuiiboDir
                };

                if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
                    amiiboDir = Path.Combine(folderBrowserDialog.SelectedPath, textBox1.Text);
                else 
                    return;
            }
            else
                amiiboDir = Path.Combine(emuiiboDir, textBox1.Text);



            if (MessageBox.Show($"Virtual amiibo will be created in '{amiiboDir}'.{Environment.NewLine + Environment.NewLine}The directory will be deleted if it already exists.{Environment.NewLine + Environment.NewLine}Proceed with amiibo creation?", Text, MessageBoxButtons.OKCancel, MessageBoxIcon.Question) != DialogResult.OK)
                return;

            if (Directory.Exists(amiiboDir))
            {
                Directory.Delete(amiiboDir, true);
            }

            try
            {
                Directory.CreateDirectory(amiiboDir);

                JObject tag = new JObject();

                if (checkBox1.Checked)
                {
                    tag["randomUuid"] = true;
                }
                else
                {
                    tag["uuid"] = AmiiboAPI.MakeRandomHexString(18);
                }

                File.WriteAllText(Path.Combine(amiiboDir, "tag.json"), tag.ToString());

                JObject model = new JObject()
                {
                    ["amiiboId"] = CurrentAmiibo[comboBox2.SelectedIndex].AmiiboId
                };

                File.WriteAllText(Path.Combine(amiiboDir, "model.json"), model.ToString());

                string dateTime = DateTime.Now.ToString("yyyy-MM-dd");

                JObject register = new JObject()
                {
                    ["name"] = textBox1.Text,
                    ["firstWriteDate"] = dateTime,
                    ["miiCharInfo"] = "mii-charinfo.bin"
                };

                File.WriteAllText(Path.Combine(amiiboDir, "register.json"), register.ToString());

                JObject common = new JObject()
                {
                    ["lastWriteDate"] = dateTime,
                    ["writeCounter"] = 0,
                    ["version"] = 0
                };

                File.WriteAllText(Path.Combine(amiiboDir, "common.json"), common.ToString());

                MessageBox.Show("Virtual amiibo was successfully created.", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch
            {
                MessageBox.Show("An error ocurred attempting to create the virtual amiibo.", Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

        }

        private void CheckBox1_CheckedChanged(object sender, EventArgs e)
        {
            if(checkBox1.Checked)
            {
                if(MessageBox.Show("Please, keep in mind that the random UUID feature might cause in some cases (Smash Bros., for example) the amiibo not to be recognized.\n(for example, when saving data to the amiibo, it could not be recognized as the original one)\n\nWould you really like to enable this feature?", "emutool - Randomize UUID", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                {
                    checkBox1.Checked = false;
                }
            }
        }
    }
}