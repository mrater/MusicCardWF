using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace mcard
{
    public partial class Form1 : Form
    {
        // === NAGRYWANIE MCI ===
        private bool isRecording = false;
        private string recordedFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "nagranie.wav");

        private void StartRecording()
        {
            try
            {
                mciSendString("close capture", null, 0, IntPtr.Zero);
                mciSendString("open new type waveaudio alias capture", null, 0, IntPtr.Zero);
                mciSendString("record capture", null, 0, IntPtr.Zero);
                isRecording = true;
                MessageBox.Show("Nagrywanie rozpoczęte...");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Błąd nagrywania: " + ex.Message);
            }
        }

        private void StopRecording()
        {
            if (!isRecording) return;

            try
            {
                mciSendString("stop capture", null, 0, IntPtr.Zero);
                mciSendString($"save capture \"{recordedFile}\"", null, 0, IntPtr.Zero);
                mciSendString("close capture", null, 0, IntPtr.Zero);
                isRecording = false;
                MessageBox.Show($"Nagrywanie zakończone. Zapisano jako:\n{recordedFile}");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Błąd zapisu: " + ex.Message);
            }
        }

        private string selectedFile = "";

        public Form1()
        {
            InitializeComponent();

            // Podłączenie przycisków
            button1.Click += button1_Click; // start
            button2.Click += button2_Click; // stop
            button3.Click += button3_Click; // pause
            button4.Click += button4_Click; // wybierz plik
        }

        private void button4_Click(object sender, EventArgs e)
        {
            openFileDialog1.Filter = "Pliki audio|*.wav;*.mp3|Wszystkie pliki|*.*";
            openFileDialog1.Title = "Wybierz plik audio";

            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                selectedFile = openFileDialog1.FileName;
                axWindowsMediaPlayer1.URL = selectedFile;
            }
        }

        // === START ===
        private void button1_Click(object sender, EventArgs e)
        {
            if (checkBox1.Checked)
            {
                StartRecording();
                return;
            }
            if (string.IsNullOrEmpty(selectedFile))
            {
                MessageBox.Show("Najpierw wybierz plik!");
                return;
            }

            if (radioButton1.Checked) // 🔹 PlaySound
            {
                PlaySound(selectedFile, IntPtr.Zero, SND_ASYNC | SND_FILENAME);
            }
            else if (radioButton2.Checked) // 🔹 WaveOutWrite
            {
                StartWaveOut(selectedFile);
            }
            else if (radioButton3.Checked) // 🔹 MCI
            {
                mciSendString("close myAudio", null, 0, IntPtr.Zero);
                mciSendString($"open \"{selectedFile}\" alias myAudio", null, 0, IntPtr.Zero);
                mciSendString("play myAudio", null, 0, IntPtr.Zero);
            }
            else if (radioButton4.Checked) // 🔹 DirectSound
            {
                MessageBox.Show("DirectSound: tu dodaj swoją implementację DirectSoundStart()");
                // np. DirectSoundStart(selectedFile);
            }
        }

        // === STOP ===
        private void button2_Click(object sender, EventArgs e)
        {
            if (checkBox1.Checked)
            {
                StopRecording();
                return;
            }
            if (radioButton1.Checked) // PlaySound
            {
                PlaySound(null, IntPtr.Zero, SND_PURGE);
            }
            else if (radioButton2.Checked) // WaveOutWrite
            {
                StopWaveOut();
            }
            else if (radioButton3.Checked) // MCI
            {
                mciSendString("stop myAudio", null, 0, IntPtr.Zero);
                mciSendString("close myAudio", null, 0, IntPtr.Zero);
            }
            else if (radioButton4.Checked)
            {
                MessageBox.Show("DirectSound: tu dodaj DirectSoundStop()");
            }
        }

        // === PAUSE ===
        private void button3_Click(object sender, EventArgs e)
        {
            if (radioButton3.Checked) // MCI
            {
                mciSendString("pause myAudio", null, 0, IntPtr.Zero);
            }
            else if (radioButton4.Checked)
            {
                MessageBox.Show("DirectSound: tu dodaj DirectSoundPause()");
            }
            else
            {
                MessageBox.Show("Ta metoda nie obsługuje pauzy.");
            }
        }

        // === PLAY SOUND ===
        [DllImport("winmm.dll", SetLastError = true)]
        private static extern bool PlaySound(string pszSound, IntPtr hmod, uint fdwSound);

        private const uint SND_FILENAME = 0x00020000;
        private const uint SND_ASYNC = 0x0001;
        private const uint SND_PURGE = 0x0040;

        // === MCI ===
        [DllImport("winmm.dll")]
        private static extern long mciSendString(string command, StringBuilder returnValue, int returnLength, IntPtr winHandle);

        // === WAVEOUTWRITE ===
        [DllImport("winmm.dll", SetLastError = true)]
        private static extern int waveOutOpen(out IntPtr hWaveOut, int uDeviceID, ref WaveFormat lpFormat, IntPtr dwCallback, IntPtr dwInstance, int dwFlags);

        [DllImport("winmm.dll", SetLastError = true)]
        private static extern int waveOutPrepareHeader(IntPtr hWaveOut, ref WaveHeader lpWaveOutHdr, int uSize);

        [DllImport("winmm.dll", SetLastError = true)]
        private static extern int waveOutWrite(IntPtr hWaveOut, ref WaveHeader lpWaveOutHdr, int uSize);

        [DllImport("winmm.dll", SetLastError = true)]
        private static extern int waveOutReset(IntPtr hWaveOut);

        [DllImport("winmm.dll", SetLastError = true)]
        private static extern int waveOutClose(IntPtr hWaveOut);

        [DllImport("winmm.dll", SetLastError = true)]
        private static extern int waveOutUnprepareHeader(IntPtr hWaveOut, ref WaveHeader lpWaveOutHdr, int uSize);

        [StructLayout(LayoutKind.Sequential)]
        private struct WaveFormat
        {
            public ushort wFormatTag;
            public ushort nChannels;
            public uint nSamplesPerSec;
            public uint nAvgBytesPerSec;
            public ushort nBlockAlign;
            public ushort wBitsPerSample;
            public ushort cbSize;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct WaveHeader
        {
            public IntPtr lpData;
            public uint dwBufferLength;
            public uint dwBytesRecorded;
            public IntPtr dwUser;
            public uint dwFlags;
            public uint dwLoops;
            public IntPtr lpNext;
            public IntPtr reserved;
        }

        private IntPtr hWaveOut = IntPtr.Zero;
        private GCHandle? audioHandle;
        private bool headerPrepared = false;
        private WaveHeader waveHeader;

        private void StartWaveOut(string path)
        {
            byte[] data = File.ReadAllBytes(path);
            if (System.Text.Encoding.ASCII.GetString(data, 0, 4) != "RIFF")
            {
                MessageBox.Show("Nieprawidłowy plik WAV.");
                return;
            }

            int fmtPos = 12;
            int sampleRate = BitConverter.ToInt32(data, fmtPos + 12);
            short bits = BitConverter.ToInt16(data, fmtPos + 22);
            short channels = BitConverter.ToInt16(data, fmtPos + 10);

            int dataPos = Array.IndexOf(data, (byte)'d', 36);
            while (dataPos < data.Length - 4 && System.Text.Encoding.ASCII.GetString(data, dataPos, 4) != "data")
                dataPos++;
            int dataSize = BitConverter.ToInt32(data, dataPos + 4);
            int dataOffset = dataPos + 8;

            var fmt = new WaveFormat
            {
                wFormatTag = 1,
                nChannels = (ushort)channels,
                nSamplesPerSec = (uint)sampleRate,
                wBitsPerSample = (ushort)bits,
                nBlockAlign = (ushort)((bits / 8) * channels),
                nAvgBytesPerSec = (uint)(sampleRate * channels * bits / 8),
                cbSize = 0
            };

            int result = waveOutOpen(out hWaveOut, -1, ref fmt, IntPtr.Zero, IntPtr.Zero, 0);
            if (result != 0)
            {
                MessageBox.Show("Błąd waveOutOpen: " + result);
                return;
            }

            var handle = GCHandle.Alloc(data, GCHandleType.Pinned);
            this.audioHandle = handle;

            waveHeader = new WaveHeader
            {
                lpData = handle.AddrOfPinnedObject() + dataOffset,
                dwBufferLength = (uint)dataSize
            };

            int sz = Marshal.SizeOf<WaveHeader>();
            waveOutPrepareHeader(hWaveOut, ref waveHeader, sz);
            headerPrepared = true;
            waveOutWrite(hWaveOut, ref waveHeader, sz);
        }

        private void StopWaveOut()
        {
            if (hWaveOut == IntPtr.Zero) return;

            waveOutReset(hWaveOut);

            int sz = Marshal.SizeOf<WaveHeader>();
            if (headerPrepared)
            {
                waveOutUnprepareHeader(hWaveOut, ref waveHeader, sz);
                headerPrepared = false;
            }

            waveOutClose(hWaveOut);
            hWaveOut = IntPtr.Zero;

            if (audioHandle.HasValue && audioHandle.Value.IsAllocated)
                audioHandle.Value.Free();
        }
    }
}
