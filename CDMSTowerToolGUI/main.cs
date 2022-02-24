using System;
using System.Drawing;
using System.IO;
using System.IO.Ports;
using System.Windows.Forms;
using SharpDX.DirectInput;

namespace CDMSTowerToolGUI
{
    public partial class frmMain : Form
    {
        const double mmperstep = 0.000064;
        bool locked = false;
        Joystick joystick = null;
        int joystickY = 32767;
        int joystickX = 32767;
        const int joystickMAX = 65535;

        public frmMain()
        {
            InitializeComponent();

            foreach (string s in SerialPort.GetPortNames())
            {
                cmbPorts.Items.Add(s);
            }
            txtAccel.Text = Properties.Settings.Default["Accel"].ToString();
            txtVelo.Text = Properties.Settings.Default["Veloc"].ToString();
            txtStep.Text = Properties.Settings.Default["Step"].ToString();


            // Initialize DirectInput
            var directInput = new DirectInput();
            // Find a Joystick Guid
            var joystickGuid = Guid.Empty;
            foreach (var deviceInstance in directInput.GetDevices(DeviceType.Gamepad,
                        DeviceEnumerationFlags.AllDevices))
                joystickGuid = deviceInstance.InstanceGuid;
            // If Gamepad not found, look for a Joystick
            if (joystickGuid == Guid.Empty)
                foreach (var deviceInstance in directInput.GetDevices(DeviceType.Joystick,
                        DeviceEnumerationFlags.AllDevices))
                    joystickGuid = deviceInstance.InstanceGuid;
           
            if (joystickGuid != Guid.Empty)
            {
                // Instantiate the joystick
                joystick = new Joystick(directInput, joystickGuid);
                lblJoystick.Text = "Found Joystick";


                // Set BufferSize in order to use buffered data.
                joystick.Properties.BufferSize = 128;

                // Acquire the joystick
                joystick.Acquire();
                chkJoystick.Checked = true;
                btnUp.Enabled = false;
                btnDown.Enabled = false;
                timer2.Enabled = true;
            }

        }

        private String COMSend(String msg)
        {
            if (!serialPort.IsOpen)
            {
                return "";
            }


            serialPort.Write(msg.Trim('#') + "\r\n");
            if (!msg.StartsWith("#"))
            {
                txtEditor.Text += msg + Environment.NewLine;
            }
            System.Threading.Thread.Sleep(100);
            return serialPort.ReadExisting();

        }

        private void btnConnect_Click(object sender, EventArgs e)
        {
            if (cmbPorts.SelectedItem != null)
            {
                serialPort.PortName = cmbPorts.SelectedItem.ToString();
                try
                {
                    serialPort.Open();
                }
                catch (Exception Ex)
                {
                    MessageBox.Show("Failed to open selected port. Make sure no other programs are using it.");
                    return;
                }
                // Initialize settings

                COMSend("#C");
                COMSend("#C");
                COMSend("#HR");

                btnConnect.Enabled = false;
                btnDisconnect.Enabled = true;
                btnStop.Enabled = true;
            }
        }

        private void btnDisconnect_Click(object sender, EventArgs e)
        {
            serialPort.Close();
            btnConnect.Enabled = true;
            btnDisconnect.Enabled = false;
            btnStop.Enabled = false;
        }

        private void txtEditor_KeyDown(object sender, KeyEventArgs e)
        {
            //if (e.KeyCode == Keys.Enter)
            //{
            //    COMSend(txtEditor.Text);
            //}
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            COMSend("ST\r\n");

        }


        private void txtPos_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                COMSend("FP" + txtPos.Text);
            }
        }

        private void txtAccel_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                COMSend("AC" + txtAccel.Text);
                COMSend("DE" + txtAccel.Text);
            }
        }


        private void txtVelo_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                COMSend("VE" + txtVelo.Text);
            }
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                File.WriteAllText(saveFileDialog.FileName, txtEditor.Text);
            }
        }

        private void btnLoad_Click(object sender, EventArgs e)
        {
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                txtEditor.Text = File.ReadAllText(openFileDialog.FileName);
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            // Update position & status
            if (serialPort.IsOpen)
            {
                String pos = COMSend("#SP");
                string[] x = pos.Trim().Split('=');
                if (x.Length > 1)
                {
                    int position = 0;
                    Int32.TryParse(x[1], out position);
                    txtPos.Text = (position * mmperstep).ToString();
                }



                String stat = COMSend("#RS");
                x = stat.Trim().Split('=');
                if (x.Length > 1)
                {
                    lblStatus.Text = x[1];
                }
            }
            
        }

        private void btnExpand_Click(object sender, EventArgs e)
        {
            if (this.Size.Width < 670)
            {
                this.Size = new Size(670, 470);
                btnExpand.Text = "<";
            }
            else
            {
                this.Size = new Size(415, 470);
                btnExpand.Text = ">";
            }
        }

        private void btnUp_Click(object sender, EventArgs e)
        {
            String stat = COMSend("#RS");
            if (!stat.ToUpper().Contains("M"))
            {
                COMSend(String.Format("FL{0}", Math.Round(Convert.ToInt32(txtStep.Text) / mmperstep)));
            }
        }

        private void btnDown_Click(object sender, EventArgs e)
        {
            String stat = COMSend("#RS");
            if (!stat.ToUpper().Contains("M"))
            {
                COMSend(String.Format("FL-{0}", Math.Round(Convert.ToInt32(txtStep.Text) / mmperstep)));
            }
        }

        private void timer2_Tick(object sender, EventArgs e)
        {
            // Get gamepad state
            if (joystick != null)
            {
                try
                {
                    joystick.Poll();
                }
                catch(Exception Ex)
                {
                    timer2.Enabled = false;
                    lblJoystick.Text = "Joystick disabled";
                    return;
                }
                var datas = joystick.GetBufferedData();
                foreach (var state in datas)
                {
                    //txtEditor.Text += state.ToString() + Environment.NewLine;
                    switch (state.Offset)
                    {
                        case JoystickOffset.X:
                            joystickX = state.Value;
                            break;
                        case JoystickOffset.Y:
                            joystickY = state.Value;
                            break;  
                        case JoystickOffset.PointOfViewControllers0:
                            break;
                        case JoystickOffset.PointOfViewControllers1:
                            break;
                        case JoystickOffset.PointOfViewControllers2:
                            break;
                        case JoystickOffset.PointOfViewControllers3:
                            break;
                        case JoystickOffset.Buttons1:
                        case JoystickOffset.Buttons2:
                        case JoystickOffset.Buttons3:
                        case JoystickOffset.Buttons0:
                            if (state.Value > 0)
                            {
                                btnStop_Click(null, null);
                            }                            
                        
                            break;
                        case JoystickOffset.Buttons4:
                            break;
                        default:
                            break;
                    }
                    
                }

                if(joystickY > joystickMAX * 3 / 4)
                {
                    if (!locked)
                    {
                        btnDown_Click(null, null);
                        locked = true;
                    }
                    
                }
                else if(joystickY < joystickMAX / 4)
                {
                    if (!locked)
                    {
                        btnUp_Click(null, null);
                        locked = true;
                    }
                }
                else
                {
                    String stat = COMSend("#RS");
                    if (stat.ToUpper().Contains("M"))
                    {
                        COMSend("#ST\r\n");
                    }
                    locked = false;
                }
            }
        }

        private void btnReset_Click(object sender, EventArgs e)
        {

            COMSend("#C");
            COMSend("#C");
            COMSend("#HR");

            // Initialize DirectInput
            var directInput = new DirectInput();
            // Find a Joystick Guid
            var joystickGuid = Guid.Empty;
            foreach (var deviceInstance in directInput.GetDevices(DeviceType.Gamepad,
                        DeviceEnumerationFlags.AllDevices))
                joystickGuid = deviceInstance.InstanceGuid;
            // If Gamepad not found, look for a Joystick
            if (joystickGuid == Guid.Empty)
                foreach (var deviceInstance in directInput.GetDevices(DeviceType.Joystick,
                        DeviceEnumerationFlags.AllDevices))
                    joystickGuid = deviceInstance.InstanceGuid;

            if (joystickGuid != Guid.Empty)
            {
                // Instantiate the joystick
                joystick = new Joystick(directInput, joystickGuid);
                lblJoystick.Text = "Found Joystick";


                // Set BufferSize in order to use buffered data.
                joystick.Properties.BufferSize = 128;

                // Acquire the joystick
                joystick.Acquire();

                timer2.Enabled = true;
            }
        }

        private void chkJoystick_CheckedChanged(object sender, EventArgs e)
        {
            if (chkJoystick.Checked)
            {

                // Initialize DirectInput
                var directInput = new DirectInput();
                // Find a Joystick Guid
                var joystickGuid = Guid.Empty;
                foreach (var deviceInstance in directInput.GetDevices(DeviceType.Gamepad,
                            DeviceEnumerationFlags.AllDevices))
                    joystickGuid = deviceInstance.InstanceGuid;
                // If Gamepad not found, look for a Joystick
                if (joystickGuid == Guid.Empty)
                    foreach (var deviceInstance in directInput.GetDevices(DeviceType.Joystick,
                            DeviceEnumerationFlags.AllDevices))
                        joystickGuid = deviceInstance.InstanceGuid;

                if (joystickGuid != Guid.Empty)
                {
                    // Instantiate the joystick
                    joystick = new Joystick(directInput, joystickGuid);
                    lblJoystick.Text = "Found Joystick";


                    // Set BufferSize in order to use buffered data.
                    joystick.Properties.BufferSize = 128;

                    // Acquire the joystick
                    joystick.Acquire();
                    
                    btnUp.Enabled = false;
                    btnDown.Enabled = false;
                    timer2.Enabled = true;
                }
            }
            else
            {
                btnUp.Enabled = true;
                btnDown.Enabled = true;
                timer2.Enabled = false;
            }
        }

        private void frmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            Properties.Settings.Default["Accel"] = Convert.ToInt32(txtAccel.Text);
            Properties.Settings.Default["Veloc"] = Convert.ToInt32(txtVelo.Text);
            Properties.Settings.Default["Step"] = Convert.ToDouble(txtStep.Text);

            Properties.Settings.Default.Save();

        }
    }
}
