using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using SharpDX;
using SharpDX.DirectSound;
using SharpDX.Multimedia;

namespace mcard
{
    public partial class Form1 : Form
    {
        //file fields
        private FileInfo file;


        // method1: DirectSound fields
        private DirectSound _directSoundDevice;

        public Form1()
        {
            InitializeComponent();
            InitializeDirectSound(this.Handle);
        }

        private void button1_Click(object sender, EventArgs e)
        {

        }


        private void selectFileButtonClick(object sender, EventArgs e)
        {
            //TODO: implement file selecting
        }

        
        // methods responsible for directound
        #region directsound
        private void InitializeDirectSound(IntPtr hwnd)
        {
            _directSoundDevice = new DirectSound();
            //IntPtr hwnd = Process.GetCurrentProcess().MainWindowHandle;
            _directSoundDevice.SetCooperativeLevel(hwnd, CooperativeLevel.Priority);
        }


        private void PlayStartDirectSoundButton_Click(object sender, EventArgs e)
        {
            // TODO: ensure that filePath should indeed be file.Name. Note: use file object to get info
            String filePath = file.Name;
            if (string.IsNullOrEmpty(filePath))
                return;

            NativeMethods.PlaySound(filePath, IntPtr.Zero, NativeMethods.SoundFlags.SND_FILENAME | NativeMethods.SoundFlags.SND_ASYNC);
        }

        private void PlayStopDirectSoundButton_Click(object sender, EventArgs e)
        {
            NativeMethods.PlaySound(null, IntPtr.Zero, NativeMethods.SoundFlags.SND_PURGE);
        }

        #endregion

        #region wmp
        //TODO do wwmp
        #endregion


        #region mci
        [DllImport("winmm.dll", SetLastError = true)]
        static extern int waveOutOpen(out IntPtr hWaveOut, int uDeviceID, ref WaveFormat lpFormat, IntPtr dwCallback, IntPtr dwInstance, int dwFlags);

        [DllImport("winmm.dll", SetLastError = true)]
        static extern int waveOutPrepareHeader(IntPtr hWaveOut, ref WaveHeader lpWaveOutHdr, int uSize);

        [DllImport("winmm.dll", SetLastError = true)]
        static extern int waveOutWrite(IntPtr hWaveOut, ref WaveHeader lpWaveOutHdr, int uSize);

        [DllImport("winmm.dll", SetLastError = true)]
        static extern int waveOutClose(IntPtr hWaveOut);

        [DllImport("winmm.dll", SetLastError = true)]
        static extern int waveOutReset(IntPtr hWaveOut);

        [DllImport("winmm.dll", SetLastError = true)]
        static extern int waveOutUnprepareHeader(IntPtr hWaveOut, ref WaveHeader lpWaveOutHdr, int uSize);

        [DllImport("winmm.dll", CharSet = CharSet.Unicode)]
        internal static extern int mciSendString(string lpszCommand, StringBuilder? lpszReturnString, int cchReturn, System.IntPtr hwndCallback);


        
}


