using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using System.Media;

namespace FileTail
{

    public partial class Form1 : Form
    {
        public static String filename = "";
        StreamReader reader;
        long lastMaxOffset=0;
        string regex = "";
        string regexIgnore = "";
        bool showAll = true;
        string wavFile = "test.wav";
        public Form1()
        {
            InitializeComponent();
        }

        ~Form1()
        {
        }

        private bool GetFile()
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog
            {

                // Set filter options and filter index.
                Filter = "All Files (*.*)|*.*",
                FilterIndex = 1,

                Multiselect = false
            };

            // Call the ShowDialog method to show the dialog box.
            DialogResult userClickedOK = openFileDialog1.ShowDialog();

            // Process input if the user clicked OK.
            if (userClickedOK.Equals(DialogResult.OK))
            {
                // Open the selected file to read.
                filename = openFileDialog1.InitialDirectory + openFileDialog1.FileName;

                reader = new StreamReader(new FileStream(filename,
                                               FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
                openFileDialog1.Dispose();
                return true;
            }
            openFileDialog1.Dispose();
            return false;
        }

    
        private void Form1_Load(object sender, EventArgs e)
        {
            regex = Properties.Settings.Default.RegEx;
            regexIgnore = Properties.Settings.Default.RegExIgnore;
            showAll = Properties.Settings.Default.ShowAll;
            wavFile = Properties.Settings.Default.WavFile;
            if (Properties.Settings.Default.Maximized)
            {
                WindowState = FormWindowState.Maximized;
                Location = Properties.Settings.Default.Location;
                Size = Properties.Settings.Default.Size;
            }
            else if (Properties.Settings.Default.Minimized)
            {
                WindowState = FormWindowState.Minimized;
                Location = Properties.Settings.Default.Location;
                Size = Properties.Settings.Default.Size;
            }
            else
            {
                Location = Properties.Settings.Default.Location;
                Size = Properties.Settings.Default.Size;
            }
            filename = Properties.Settings.Default.Filename;
            string[] args = Environment.GetCommandLineArgs();
            if (args.Length > 1)
            {
                filename = args[1];
                if (!System.IO.File.Exists(filename))
                {
                    MessageBox.Show(filename + " does not exist");
                    filename = "";
                }
            }
            
            if (filename.Length == 0 || !System.IO.File.Exists(filename))
            {
                if (!GetFile())
                {
                    Application.Exit();
                }
            }
            else
            {
                reader = new StreamReader(new FileStream(filename,
                                               FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
            }
            Help();
            timer1.Interval = 1000;
            timer1.Start();
        }

        private void Timer1_Tick(object sender, EventArgs e)
        {
            timer1.Stop();
            if (lastMaxOffset == 0)
            {
                reader.ReadToEnd();
                lastMaxOffset = reader.BaseStream.Position;
                timer1.Start();
                return;
            }
            //if the file size has changed, update our window
            if (reader.BaseStream.Length != lastMaxOffset)
            {
                //seek to the last max offset
                reader.BaseStream.Seek(lastMaxOffset, SeekOrigin.Begin);

                //read out of the file until the EOF
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    try
                    {
                        bool matched = regex.Length > 0 && Regex.IsMatch(line, regex);
                        bool matchedIgnore = regexIgnore.Length > 0 && Regex.IsMatch(line, regexIgnore);
                        if (matched && !matchedIgnore)
                        {
                            richTextBox1.AppendText(line + "\n");
                            SoundPlayer player = new SoundPlayer(@"test.wav");
                            player.Play();
                            player.Dispose();

                        }
                        else if ((!matched || matchedIgnore) && showAll)
                        {
                            richTextBox1.AppendText(line + "\n");
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                    }
                }
                //update the last max offset
                lastMaxOffset = reader.BaseStream.Position;
            }
            timer1.Start();
        }

        void Help()
        {
            string f5 = "On";
            if (!showAll) f5 = "Off";
            richTextBox1.AppendText("F1-Help,  F2-FileOpen, F3-Clear, F4=RegEx, F5-Showall("+f5+"), F6-WavFile\n");
            richTextBox1.AppendText("Watching " + filename);
            if (regex.Length>0)
            {
                richTextBox1.AppendText(" for " + regex);
            }
            if (regexIgnore.Length>0)
            {
                richTextBox1.AppendText(" but not " + regexIgnore);
            }
            richTextBox1.AppendText("\n");
        }

        private void GetRegEx()
        {
            string response = regex;
            InputQuery("RegEx Expression match", "Enter expression to watch for", ref response);   
            regex = response;
            Properties.Settings.Default.RegEx = response;
            Properties.Settings.Default.Save();
            response = regexIgnore;
            InputQuery("RegEx Expressio ignore", "Enter expression to ignore", ref response);
            regexIgnore = response;
            Properties.Settings.Default.RegExIgnore = response;
            Properties.Settings.Default.Save();
        }

        private void GetWavFile()
        {
            OpenFileDialog myDialog = new OpenFileDialog
            {
                DefaultExt = "wav"
            };
            DialogResult result = myDialog.ShowDialog(this);
            myDialog.CheckFileExists = true;
            if (result == DialogResult.OK)
            {
                wavFile = myDialog.FileName;
            }
            myDialog.Dispose();
            Properties.Settings.Default.WavFile = wavFile;
            Properties.Settings.Default.Save();
        }

        private void OnPreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.F1:
                    Help();
                    break;
                case Keys.F2:
                    GetFile();
                    lastMaxOffset = 0;
                    Help();
                    break;
                case Keys.F3:
                    richTextBox1.Clear();
                    break;
                case Keys.F4:
                    GetRegEx();
                    if (regex.Length==0 && regexIgnore.Length==0)
                    {
                        showAll = true;
                    }
                    Help();
                    break;
                case Keys.F5:
                    if (regex.Length > 0 || regexIgnore.Length > 0)
                    {
                        showAll = !showAll;
                        Properties.Settings.Default.ShowAll = showAll;
                        Properties.Settings.Default.Save();
                        Help();
                    }
                    else
                    {
                        MessageBox.Show(this,"No RegEx so showAll remains ON");
                    }
                    break;
                case Keys.F6:
                    GetWavFile();
                    break;
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (WindowState == FormWindowState.Maximized)
            {
                Properties.Settings.Default.Location = RestoreBounds.Location;
                Properties.Settings.Default.Size = RestoreBounds.Size;
                Properties.Settings.Default.Maximized = true;
                Properties.Settings.Default.Minimized = false;
            }
            else if (WindowState == FormWindowState.Normal)
            {
                Properties.Settings.Default.Location = Location;
                Properties.Settings.Default.Size = Size;
                Properties.Settings.Default.Maximized = false;
                Properties.Settings.Default.Minimized = false;
            }
            else
            {
                Properties.Settings.Default.Location = RestoreBounds.Location;
                Properties.Settings.Default.Size = RestoreBounds.Size;
                Properties.Settings.Default.Maximized = false;
                Properties.Settings.Default.Minimized = true;
            }
            Properties.Settings.Default.Filename = filename;
            Properties.Settings.Default.Save();
        }
        public static Boolean InputQuery(String caption, String prompt, ref String value)
        {
            Form form;
            form = new Form
            {
                AutoScaleMode = AutoScaleMode.Font,
                Font = SystemFonts.IconTitleFont
            };

            SizeF dialogUnits;
            dialogUnits = form.AutoScaleDimensions;

            form.FormBorderStyle = FormBorderStyle.FixedDialog;
            form.MinimizeBox = false;
            form.MaximizeBox = false;
            form.Text = caption;

            form.ClientSize = new Size(
                        MulDiv(180, (int)dialogUnits.Width, 4),
                        MulDiv(63, (int)dialogUnits.Height, 8));

            form.StartPosition = FormStartPosition.CenterScreen;

            System.Windows.Forms.Label lblPrompt;
            lblPrompt = new System.Windows.Forms.Label
            {
                Parent = form,
                AutoSize = true,
                Left = MulDiv(8, (int)dialogUnits.Width, 4),
                Top = MulDiv(8, (int)dialogUnits.Height, 8),
                Text = prompt
            };

            System.Windows.Forms.TextBox edInput;
            edInput = new TextBox
            {
                Parent = form,
                Left = lblPrompt.Left,
                Top = MulDiv(19, (int)dialogUnits.Height, 8),
                Width = MulDiv(164, (int)dialogUnits.Width, 4),
                Text = value
            };
            edInput.SelectAll();
            lblPrompt.Dispose();

            int buttonTop = MulDiv(41, (int)dialogUnits.Height, 8);
            //Command buttons should be 50x14 dlus
            //Size buttonSize = ScaleSize(new Size(50, 14), dialogUnits.Width / 4, dialogUnits.Height / 8);

            System.Windows.Forms.Button bbOk = new System.Windows.Forms.Button
            {
                Parent = form,
                Text = "OK",
                DialogResult = DialogResult.OK
            };
            form.AcceptButton = bbOk;
            bbOk.Location = new Point(MulDiv(38, (int)dialogUnits.Width, 4), buttonTop);
            //bbOk.Size = buttonSize;

            System.Windows.Forms.Button bbCancel = new System.Windows.Forms.Button
            {
                Parent = form,
                Text = "Cancel",
                DialogResult = DialogResult.Cancel
            };
            form.CancelButton = bbCancel;
            bbCancel.Location = new Point(MulDiv(92, (int)dialogUnits.Width, 4), buttonTop);
            //bbCancel.Size = buttonSize;

            if (form.ShowDialog() == DialogResult.OK)
            {
                value = edInput.Text;
                edInput.Dispose();
                return true;
            }
            edInput.Dispose();
            return false;
        }

        /// <summary>
        /// Multiplies two 32-bit values and then divides the 64-bit result by a 
        /// third 32-bit value. The final result is rounded to the nearest integer.
        /// </summary>
        public static int MulDiv(int nNumber, int nNumerator, int nDenominator)
        {
            return (int)Math.Round((float)nNumber * nNumerator / nDenominator);
        }
    }
}
