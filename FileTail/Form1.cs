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
using SpeechLib;
using System.Collections.Concurrent;
using System.Threading;
using System.Runtime.InteropServices;

namespace FileTail
{

    public partial class Form1 : Form
    {
        public static String filename = "";
        private string filenameLog;

        public StreamWriter WriterLog { get; private set; }

        StreamReader reader;
        long lastMaxOffset=0;
        string regex = "";
        string regexIgnore = "";
        bool showAll = true;
        string wavFile = "test.wav";
        //string callsign = "W9MDB";
        readonly Task taskSpeech = new Task(SpeechHandler);
        static readonly ConcurrentBag<string> speechQueue = new ConcurrentBag<string>();
        private const string Off = "Off";
        private const string On = "On";
        private string captureStatus = Off;
        private string soundStatus = Off;

        //FileSystemWatcher watcher;
        public DateTime LastWriteTime { get; private set; }

        QRZ qrz;
        //int nlines = 0;
        private string qrzlogin;
        private string qrzpassword;
        private bool pause;

        public Form1()
        {
            InitializeComponent();
            taskSpeech.Start();
            qrzlogin = Properties.Settings.Default.QRZLogin;
            qrzpassword = Properties.Settings.Default.QRZPassword;
            if (qrzlogin == null || qrzlogin.Length == 0)
                GetQRZLogin();
            qrz = new QRZ(Properties.Settings.Default.QRZLogin, Properties.Settings.Default.QRZPassword, "cache.txt");
            if (qrz == null) MessageBox.Show("QRZ login failed");
        }

        ~Form1()
        {
        }

        static void SpeechHandler()
        {
            var speak = new SpeechLib.Synthesis.SpeechSynthesis();
            while (true)
            {
                while (speechQueue.TryTake(out string result))
                {
                    result = result.Replace("-", " minus ");
                    speak.SpeechSynthesisEngine.Speak(result);
                }
                Thread.Sleep(500);
            }
            //speak.Dispose();
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
                //watcher = new FileSystemWatcher(filename);
                //watcher.Changed += Timer1_Tick;
                //watcher.EnableRaisingEvents = true;
                return true;
            }
            openFileDialog1.Dispose();
            return false;
        }

        private bool GetFileLog()
        {
            try
            {
                OpenFileDialog openFileDialog1 = new OpenFileDialog
                {

                    // Set filter options and filter index.
                    Filter = "All Files (*.*)|*.*",
                    FilterIndex = 1,
                    Multiselect = false,
                    CheckFileExists = false
                };
                openFileDialog1.CheckFileExists = false;
                // Call the ShowDialog method to show the dialog box.
                DialogResult userClickedOK = openFileDialog1.ShowDialog();

                // Process input if the user clicked OK.
                if (userClickedOK.Equals(DialogResult.OK))
                {
                    if (filenameLog != "") WriterLog.Close();
                    // Open the selected file to read.
                    filenameLog = openFileDialog1.InitialDirectory + openFileDialog1.FileName;

                    WriterLog = new StreamWriter(new FileStream(filenameLog,
                                                   FileMode.Create, FileAccess.Write, FileShare.ReadWrite));
                    openFileDialog1.Dispose();
                    captureStatus = filenameLog;
                    //watcher = new FileSystemWatcher(filename);
                    //watcher.Changed += Timer1_Tick;
                    //watcher.EnableRaisingEvents = true;
                    return true;
                }
                else
                {
                    filenameLog = "";
                    richTextBox1.AppendText("Log file canceled\n");
                    if (WriterLog != null)
                    {
                        WriterLog.Close();
                        WriterLog.Dispose();
                        WriterLog = null;
                        captureStatus = Off;
                    }
                }
                openFileDialog1.Dispose();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + "\n" + ex.StackTrace);
            }
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
            filenameLog = Properties.Settings.Default.FilenameLog;
            if (filenameLog == null) return;
            if (filenameLog.Length != 0)
            {
                WriterLog = new StreamWriter(new FileStream(filenameLog, FileMode.Append, FileAccess.Write));
                captureStatus = filenameLog;
            }
            Help();
            timer1.Interval = 1000;
            timer1.Start();
        }
        /*
        void Speak(string text)
        {
            var speak = new SpeechLib.Synthesis.SpeechSynthesis();
            speak.Speak(text);
            speak.Dispose();
        }
        */

        private void Timer1_Tick(object sender, EventArgs e)
        {
            string line = "No line yet";
            try
            {
                timer1.Stop();
                if (lastMaxOffset == 0)
                {
                    //reader.ReadToEnd();
                    reader.BaseStream.Seek(0, SeekOrigin.End);    
                    lastMaxOffset = reader.BaseStream.Position;
                    timer1.Start();
                    return;
                }
                //richTextBox1.AppendText("Loop " + richTextBox1.Lines.Count() + "\n");
                //if the file size has changed, update our window
                if (File.GetLastWriteTime(filename) != LastWriteTime)
                {
                    LastWriteTime = File.GetLastWriteTime(filename);
                    if (reader.BaseStream.Length != lastMaxOffset)
                    {
                        //seek to the last max offset
                        reader.BaseStream.Seek(lastMaxOffset, SeekOrigin.Begin);

                        //read out of the file until the EOF
                        while ((line = reader.ReadLine()) != null)
                        {
                            if (pause) continue;
                            if (line.Contains("handle_") || line.Contains("Decod") || line.Contains("Lost") || line.Contains("his")) continue;
                            //nlines++;
                            try
                            {
                                var allText = new AllText();
                                var callsign = allText.Callsign(line);
                                if (callsign != null && qrz != null)
                                {
                                    var result = qrz.GetCallsign(callsign, out bool cached);
                                    if (result)
                                    {
                                        if (qrz.dxcc == 291)
                                        {
                                            if (cached)
                                            {
                                                line = "<" + qrz.license + "!> " + line;
                                            }
                                            else
                                            {
                                                line = "<" + qrz.license + " > " + line;
                                            }
                                        }
                                        else
                                        {
                                            if (cached)
                                            {
                                                line = "<?!> " + line;
                                            }
                                            else
                                            {
                                                line = "<? > " + line;
                                            }
                                        }
                                    }
                                }
                                bool matched = regex.Length > 0 && Regex.IsMatch(line, regex);
                                bool matchedIgnore = regexIgnore.Length > 0 && Regex.IsMatch(line, regexIgnore);
                                if (matched && !matchedIgnore)
                                {
                                    if (WriterLog != null)
                                    {
                                        WriterLog.WriteLine(line);
                                        WriterLog.Flush();
                                    }
                                    richTextBox1.AppendText(line + "\n");
                                    if (soundStatus.Equals(On))
                                    {
                                        SoundPlayer player = new SoundPlayer(wavFile);
                                        player.Play();
                                        player.Dispose();
                                    }

                                }
                                else if (showAll)
                                {
                                    if (WriterLog != null)
                                    {
                                        WriterLog.WriteLine(line);
                                        WriterLog.Flush();
                                    }
                                    richTextBox1.AppendText(line + "\n");
                                }
                                Application.DoEvents();
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show(ex.Message + "\n" + ex.StackTrace);
                            }
                        }
                        //update the last max offset
                        lastMaxOffset = reader.BaseStream.Position;
                    }
                }
                timer1.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show(line + "\n" + ex.Message + "\n" + ex.StackTrace);
            }
        }

        void Help()
        {
            string f5 = "On";
            if (!showAll) f5 = Off;
            richTextBox1.AppendText("By W9MDB in the public domain for ARRL Volunteer Monitoring use\n");
            richTextBox1.AppendText("F1-Help,  F2-FileOpen, F3-Clear, F4=RegEx, F5-Showall is " + f5 + "\nF6-WavFile, F7-QRZ Login, F8-Capture File=" + captureStatus +", F9=Pause\n");
            richTextBox1.AppendText("F11-Sound="+soundStatus+", F12-Witnessed\n");
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
            InputQuery("RegEx Expression ignore", "Enter expression to ignore", ref response);
            regexIgnore = response;
            Properties.Settings.Default.RegExIgnore = response;
            Properties.Settings.Default.Save();
        }
        private void GetQRZLogin()
        {
            string response = qrzlogin;
            InputQuery("QRZ Login Name", "Enter qrz login name", ref response);
            qrzlogin = response;
            Properties.Settings.Default.QRZLogin = response;
            Properties.Settings.Default.Save();
            response = qrzpassword;
            InputQuery("QRZ Password", "Enter qrz password", ref response);
            qrzpassword = response;
            Properties.Settings.Default.QRZPassword = response;
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
                case Keys.F7:
                    GetQRZLogin();
                    qrz = new QRZ(Properties.Settings.Default.QRZLogin, Properties.Settings.Default.QRZPassword, "cache.txt");
                    break;
                case Keys.F8:
                    if (!captureStatus.Equals(Off))
                    {
                        captureStatus = Off;
                        filenameLog = "";
                    }
                    else
                    {
                        GetFileLog();
                    }
                    Help();
                    //Speak("W9MDB");
                    break;
                case Keys.F9:
                    pause = !pause;
                    if (pause) richTextBox1.AppendText("Paused...F9 to resume...");
                    else richTextBox1.AppendText("continuing\n");
                    break;
                case Keys.F11:
                    soundStatus = soundStatus.Equals(Off) ? On : Off;
                    Help();
                    break;
                case Keys.F12:
                    richTextBox1.SelectionStart = richTextBox1.TextLength-1;
                    richTextBox1.SelectionLength = 1;
                    richTextBox1.SelectedText = " WITNESSED" + "\n";
                    WriterLog.BaseStream.Seek(-2, SeekOrigin.End);
                    WriterLog.WriteLine(" WITNESSED");
                    WriterLog.Flush();
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
            Properties.Settings.Default.QRZLogin = qrzlogin;
            Properties.Settings.Default.QRZPassword = qrzpassword;
            Properties.Settings.Default.FilenameLog = filenameLog;
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

        private void Timer2_Tick(object sender, EventArgs e)
        {
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int SendMessage(IntPtr hWnd, int wMsg, IntPtr wParam, IntPtr lParam);
        private const int WM_VSCROLL = 277;
        private const int SB_PAGEBOTTOM = 7;

        public static void ScrollToBottom(RichTextBox MyRichTextBox)
        {
            SendMessage(MyRichTextBox.Handle, WM_VSCROLL, (IntPtr)SB_PAGEBOTTOM, IntPtr.Zero);
        }
        private void RichTextBox1_TextChanged(object sender, EventArgs e)
        {
            int maxLines = 500;
            int trimToLines = 400;
            if (richTextBox1.Lines.Count() > maxLines)
            {
                richTextBox1.Lines = richTextBox1.Lines.Skip(richTextBox1.Lines.Length - trimToLines).ToArray();
            }
            ScrollToBottom(richTextBox1);
            //richTextBox1.SelectionStart = richTextBox1.Lines.Count();
            //richTextBox1.SelectionLength = 1;
            //richTextBox1.ScrollToCaret();
        }

        private void Form1_TextChanged(object sender, EventArgs e)
        {
        }
    }
}
