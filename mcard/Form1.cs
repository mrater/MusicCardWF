using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using SharpDX.DirectSound;
using System.Threading.Tasks;
using SharpDX;
using SharpDX.Multimedia;

namespace mcard
{
    public partial class Form1 : Form
    {
        private string selectedFile = "";

        private SecondarySoundBuffer _directSoundBuffer;
        private DirectSound _directSoundDevice;
        private bool isDirectSoundPaused = false;
        private bool isMciPaused = false;


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

        public Form1()
        {
            InitializeComponent();

            // Podłączenie przycisków
            button1.Click += button1_Click; // start
            button2.Click += button2_Click; // stop
            button3.Click += button3_Click; // pause
            button4.Click += button4_Click; // wybierz plik

            _directSoundDevice = new DirectSound();
            IntPtr hwnd = this.Handle;
            _directSoundDevice.SetCooperativeLevel(hwnd, CooperativeLevel.Priority);

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

            if (radioButton1.Checked) //  PlaySound
            {
                PlaySound(selectedFile, IntPtr.Zero, SND_ASYNC | SND_FILENAME);
            }
            else if (radioButton2.Checked) //  WaveOutWrite
            {
                StartWaveOut(selectedFile);
            }
            else if (radioButton3.Checked) //  MCI
            {
                mciSendString("close myAudio", null, 0, IntPtr.Zero);
                mciSendString($"open \"{selectedFile}\" alias myAudio", null, 0, IntPtr.Zero);
                mciSendString("play myAudio", null, 0, IntPtr.Zero);
                isMciPaused = false;
            }
            else if (radioButton4.Checked) //  DirectSound
            {
                DirectSoundPlayWavFileAsync();
            }
            else if (radioButton5.Checked) // Windows Media Player
            {
                axWindowsMediaPlayer1.Ctlcontrols.play();
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
                isMciPaused = false;
            }
            else if (radioButton4.Checked)
            {
                DirectSoundStopPlaybackAsync();
            }
            else if (radioButton5.Checked) // Windows Media Player
            {
                axWindowsMediaPlayer1.Ctlcontrols.stop();
            }
        }

        // === PAUSE ===
        private void button3_Click(object sender, EventArgs e)
        {
            if (radioButton3.Checked) // MCI
            {
                if (isMciPaused)
                {
                    mciSendString("resume myAudio", null, 0, IntPtr.Zero);
                }
                else
                {
                    mciSendString("pause myAudio", null, 0, IntPtr.Zero);
                }
                isMciPaused = !isMciPaused;
            }
            else if (radioButton4.Checked) // DirectSound
            {
                DirectSoundPauseResume();
            }
            else if (radioButton5.Checked) // Windows Media Player
            {
                if (axWindowsMediaPlayer1.playState == WMPLib.WMPPlayState.wmppsPlaying)
                {
                    axWindowsMediaPlayer1.Ctlcontrols.pause();
                }
                else if (axWindowsMediaPlayer1.playState == WMPLib.WMPPlayState.wmppsPaused)
                {
                    axWindowsMediaPlayer1.Ctlcontrols.play();
                }
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

        private const uint WHDR_DONE = 0x00000001;

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
            if (hWaveOut != IntPtr.Zero)
            {
                StopWaveOut();
            }

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

            // Pętla oczekująca, aż sterownik oznaczy bufor jako zakończony (WHDR_DONE)
            int retries = 50; // Czekaj maksymalnie ~1 sekundę
            while ((waveHeader.dwFlags & WHDR_DONE) == 0 && retries > 0)
            {
                System.Threading.Thread.Sleep(20);
                retries--;
            }

            int sz = Marshal.SizeOf<WaveHeader>();
            if (headerPrepared)
            {
                waveOutUnprepareHeader(hWaveOut, ref waveHeader, sz);
                headerPrepared = false;
            }

            waveOutClose(hWaveOut);
            hWaveOut = IntPtr.Zero;

            if (audioHandle.HasValue && audioHandle.Value.IsAllocated)
            {
                audioHandle.Value.Free();
                audioHandle = null;
            }
        }


        // DirectSound
        private async void DirectSoundPlayWavFileAsync()
        {
            if (_directSoundBuffer != null)
            {
                await DirectSoundStopPlaybackAsync();
            }
            isDirectSoundPaused = false;
            
            Stream stream = File.OpenRead(selectedFile); 
            var reader = new SoundStream(stream);
            var format = reader.Format;

            var bufferDescription = new SoundBufferDescription
            {
                Flags = BufferFlags.ControlVolume | BufferFlags.GlobalFocus | (checkBoxEcho.Checked ? BufferFlags.ControlEffects : 0),
                BufferBytes = (int)reader.Length,
                Format = format
            };

            _directSoundBuffer = new SecondarySoundBuffer(_directSoundDevice, bufferDescription);


            if (checkBoxEcho.Checked)
            {
                Guid[] echoGuid = { new Guid("EF3E932C-D40B-4F51-8CCF-3F98F1B29D5D") };
                _directSoundBuffer.SetEffect(echoGuid);
            }



            byte[] audioData = new byte[reader.Length];
            reader.Read(audioData, 0, audioData.Length);
            _directSoundBuffer.Write(audioData, 0, LockFlags.None);


            _directSoundBuffer.Play(0, PlayFlags.Looping);

            // Usunięto Task.Delay, aby umożliwić interakcję podczas odtwarzania
        }

        private void DirectSoundPauseResume()
        {
            if (_directSoundBuffer == null) return;

            if (isDirectSoundPaused)
            {
                _directSoundBuffer.Play(0, PlayFlags.Looping); // Wznów odtwarzanie w pętli
            }
            else
            {
                _directSoundBuffer.Stop(); // Zatrzymaj (spauzuj)
            }
            isDirectSoundPaused = !isDirectSoundPaused;
        }

        private async Task DirectSoundStopPlaybackAsync()
        {
            if (_directSoundBuffer != null)
            {
                _directSoundBuffer.Stop();
                _directSoundBuffer.Dispose();
                _directSoundBuffer = null;
            }
            isDirectSoundPaused = false;

            await Task.CompletedTask;
        }

    }
}