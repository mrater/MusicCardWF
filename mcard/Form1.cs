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
using System.Runtime.InteropServices;
using System.Runtime.InteropServices;
using System.Windows.Forms;



namespace mcard
{
    public partial class Form1 : Form
    {
        //file fields
        private FileInfo file;

        private IntPtr hWaveOut = IntPtr.Zero;
        private GCHandle? audioHandle;


        // method1: DirectSound fields
        private DirectSound _directSoundDevice;

        private bool isPlaying = false;

        // Header state for unprepare
        private NativeMethods.WaveHeader waveHeader;
        private bool headerPrepared = false;

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


        #region wave
        private async void PlayWaveAsync()
        {
            if (file == null)
                return;

            // TODO: ensure the following line is correct
            byte[] data = File.ReadAllBytes(file.Name);
          
            if (System.Text.Encoding.ASCII.GetString(data, 0, 4) != "RIFF")
            {
                return;
            }

            // Proste parsowanie WAV headera (PCM)
            int fmtPos = BitConverter.ToInt32(data, 12) == 0x20746D66 ? 12 : 20;
            int sampleRate = BitConverter.ToInt32(data, fmtPos + 12);
            short bits = BitConverter.ToInt16(data, fmtPos + 22);
            short channels = BitConverter.ToInt16(data, fmtPos + 10);

            int dataPos = Array.IndexOf(data, (byte)'d', 36); // znajd  "data"
            while (dataPos < data.Length - 4 && System.Text.Encoding.ASCII.GetString(data, dataPos, 4) != "data")
                dataPos++;
            int dataSize = BitConverter.ToInt32(data, dataPos + 4);
            int dataOffset = dataPos + 8;

            var fmt = new NativeMethods.WaveFormat
            {
                wFormatTag = 1, // PCM
                nChannels = (ushort)channels,
                nSamplesPerSec = (uint)sampleRate,
                wBitsPerSample = (ushort)bits,
                nBlockAlign = (ushort)((bits / 8) * channels),
                nAvgBytesPerSec = (uint)(sampleRate * channels * bits / 8),
                cbSize = 0
            };

            IntPtr localWaveOut;
            int result = NativeMethods.waveOutOpen(out localWaveOut, -1, ref fmt, IntPtr.Zero, IntPtr.Zero, 0);
            if (result != 0)
            {
                return;
            }

            // Pin audio buffer and store state in instance fields so StopWave can access them
            var handle = GCHandle.Alloc(data, GCHandleType.Pinned);
            this.audioHandle = handle;

            this.waveHeader = new NativeMethods.WaveHeader
            {
                lpData = handle.AddrOfPinnedObject() + dataOffset,
                dwBufferLength = (uint)dataSize,
                dwFlags = 0,
                dwLoops = 0
            };

            // assign device handle to instance
            this.hWaveOut = localWaveOut;

            int sz = Marshal.SizeOf<NativeMethods.WaveHeader>();
            NativeMethods.waveOutPrepareHeader(this.hWaveOut, ref this.waveHeader, sz);
            headerPrepared = true;

            NativeMethods.waveOutWrite(this.hWaveOut, ref this.waveHeader, sz);

            isPlaying = true;

            // Poczekaj a  d wi k si  odtworzy (nieblokuj co)
            await System.Threading.Tasks.Task.Delay(dataSize / (int)fmt.nAvgBytesPerSec * 1000 + 500);

            // After playback completes, if not stopped externally, clean up
            try
            {
                if (isPlaying && hWaveOut != IntPtr.Zero)
                {
                    if (headerPrepared)
                    {
                        NativeMethods.waveOutUnprepareHeader(this.hWaveOut, ref this.waveHeader, sz);
                        headerPrepared = false;
                    }
                    NativeMethods.waveOutClose(this.hWaveOut);
                    this.hWaveOut = IntPtr.Zero;
                }
            }
            finally
            {
                if (this.audioHandle.HasValue && this.audioHandle.Value.IsAllocated)
                {
                    this.audioHandle.Value.Free();
                    this.audioHandle = null;
                }
                isPlaying = false;
            }
        }
        #endregion






    }
}
