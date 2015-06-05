using System;
using System.Collections.Generic;
using System.Text;
using SoundCatcher; // Voice activated recording by monitoring amplitude events (record above selected threshold)
using System.IO;

namespace Turan_SC
{
    public class Recording
    {

        public delegate void AudioEventDetectedHandler();
        public event AudioEventDetectedHandler AudioEventDetected;

        byte[] bf1;


        //public QueueCB cBuff = new QueueCB(96000);

        public Recording()
        {
            if (WaveNative.waveInGetNumDevs() == 0)
            {
                //textBoxConsole.AppendText(DateTime.Now.ToString() + " : Felvevő eszköz nem található\r\n");
                throw new Exception("Felvevő eszköz nem található!");
            }
            else
            {
                //cBuff.SetAsynchronousMode();


                //textBoxConsole.AppendText(DateTime.Now.ToString() + " : Felvevő eszköz észlelve\r\n");
                if (_isPlayer == true)
                    _streamOut = new FifoStream();
                _audioFrame = new AudioFrame(_isTest);
                _audioFrame.IsDetectingEvents_f = Properties.Settings.Default.SettingIsDetectingEvents;
                _audioFrame.AmplitudeThreshold_f = Properties.Settings.Default.SettingAmplitudeThreshold;
                _streamMemory = new MemoryStream();
                _streamMemorySmallBuffer = new MemoryStream();
                Start();
            }

            //RefreshRecordState();
        }

        public void StopRecording()  // destructor
        {
            Stop();
        }

        public double GetCurrentAmplitude()
        {
            return _audioFrame.CurrentAmplitude_f;
        }

        public void TurnOnAndSave()
        {
            _audioFrame.IsDetectingEvents_f = true;
            Properties.Settings.Default.SettingIsDetectingEvents = _audioFrame.IsDetectingEvents_f;
            Properties.Settings.Default.Save();
        }

        public void TurnOffAndSave()
        {
            _audioFrame.IsDetectingEvents_f = false;
            Properties.Settings.Default.SettingIsDetectingEvents = _audioFrame.IsDetectingEvents_f;
            Properties.Settings.Default.Save();
        }

        public void TurnOn()
        {
            _audioFrame.IsDetectingEvents_f = true;
        }

        public void TurnOff()
        {
            _audioFrame.IsDetectingEvents_f = false;
        }


        public int GetSamplesPerSecond()
        {
            return Properties.Settings.Default.SettingSamplesPerSecond;
        }

        public void SetSamplesPerSecond(int sample_per_sec)
        {
            if (sample_per_sec >= 1000 && sample_per_sec <= 32000)
            {
                Properties.Settings.Default.SettingSamplesPerSecond = sample_per_sec;
                Properties.Settings.Default.Save();
            }
            else
            {
                throw new Exception("A mintavételi frekvenciának 1000 és 32000 között kell lennie!");
            }
        }

        public int GetRecordingThreshold()
        {
            return _audioFrame.AmplitudeThreshold_f;
        }

        public void SetRecordingThreshold(int amp_threshold)
        {
            if (amp_threshold >= 1000 && amp_threshold <= 32767)
            {
                _audioFrame.AmplitudeThreshold_f = amp_threshold;
                Properties.Settings.Default.SettingAmplitudeThreshold = amp_threshold;
                Properties.Settings.Default.Save();
            }
            else
            {
                throw new Exception("A felvételi határértéknek 1000 és 32767 között kell lennie!");
            }
        }

        public void TurnOnOffAndSave()
        {
            if (_audioFrame.IsDetectingEvents_f)
            {
                _audioFrame.IsDetectingEvents_f = false;
            }
            else
            {
                _audioFrame.IsDetectingEvents_f = true;
            }

            Properties.Settings.Default.SettingIsDetectingEvents = _audioFrame.IsDetectingEvents_f;
            Properties.Settings.Default.Save();
        }

        public bool IsRecordingActive()
        {
            return _audioFrame.IsDetectingEvents_f;
        }

        //-------------SC-INTEGRATION----------------------------

        string signal_filename = "signal.wav";

        public string Signal_filename_f
        {
            get { return signal_filename; }
            set { signal_filename = value; }
        }

        private WaveInRecorder _recorder;
        private byte[] _recorderBuffer;
        private WaveOutPlayer _player;
        private byte[] _playerBuffer;
        private WaveFormat _waveFormat;
        private AudioFrame _audioFrame;
        private FifoStream _streamOut;
        private MemoryStream _streamMemory;

        private MemoryStream _streamMemorySmallBuffer;

        private Stream _streamWave;
        private FileStream _streamFile;
        private bool _isPlayer = false;  // audio output for testing
        private bool _isTest = false;  // signal generation for testing
        private bool _isSaving = false;
        //private bool _isShown = true;
        private string _sampleFilename;
        private DateTime _timeLastDetection;


        private void Start()
        {
            Stop();
            try
            {
                _waveFormat = new WaveFormat(Properties.Settings.Default.SettingSamplesPerSecond, Properties.Settings.Default.SettingBitsPerSample, Properties.Settings.Default.SettingChannels);

                _recorder = new WaveInRecorder(Properties.Settings.Default.SettingAudioInputDevice, _waveFormat, Properties.Settings.Default.SettingBytesPerFrame * Properties.Settings.Default.SettingChannels, 3, new BufferDoneEventHandler(DataArrived));

                if (_isPlayer == true)
                    _player = new WaveOutPlayer(Properties.Settings.Default.SettingAudioOutputDevice, _waveFormat, Properties.Settings.Default.SettingBytesPerFrame * Properties.Settings.Default.SettingChannels, 3, new BufferFillEventHandler(Filler));
            }
            catch (Exception)
            {

            }
        }
        private void Stop()
        {
            if (_recorder != null)
                try
                {
                    _recorder.Dispose();
                }
                finally
                {
                    _recorder = null;
                }
            if (_isPlayer == true)
            {
                if (_player != null)
                    try
                    {
                        _player.Dispose();
                    }
                    finally
                    {
                        _player = null;
                    }
                _streamOut.Flush(); // clear all pending data
            }
        }



        private void Filler(IntPtr data, int size)
        {
            if (_isPlayer == true)
            {
                if (_playerBuffer == null || _playerBuffer.Length < size)
                    _playerBuffer = new byte[size];
                if (_streamOut.Length >= size)
                    _streamOut.Read(_playerBuffer, 0, size);
                else
                    for (int i = 0; i < _playerBuffer.Length; i++)
                        _playerBuffer[i] = 0;
                System.Runtime.InteropServices.Marshal.Copy(_playerBuffer, 0, data, size);
            }
        }


        //   --- DataArrived ---


        private void DataArrived(IntPtr data, int size)
        {

            int circular_puffer_size = 90000000; //524288; //131072; // 65536;

            if (_isSaving == true)
            {
                byte[] recBuffer = new byte[size];
                System.Runtime.InteropServices.Marshal.Copy(data, recBuffer, 0, size);
                _streamMemory.Write(recBuffer, 0, recBuffer.Length);
            }
            else
            {
                byte[] recBuffer = new byte[size];
                System.Runtime.InteropServices.Marshal.Copy(data, recBuffer, 0, size);


                if (_streamMemory.Position >= circular_puffer_size)
                {
                    _streamMemory.Position = 0;
                }

                _streamMemory.Write(recBuffer, 0, recBuffer.Length);
                //_streamMemorySmallBuffer.Write(recBuffer, 0, recBuffer.Length);




            }

            if (_recorderBuffer == null || _recorderBuffer.Length != size)
            {
                _recorderBuffer = new byte[size];
            }

            if (_recorderBuffer != null)
            {
                System.Runtime.InteropServices.Marshal.Copy(data, _recorderBuffer, 0, size);
                if (_isPlayer == true)
                    _streamOut.Write(_recorderBuffer, 0, _recorderBuffer.Length);



                _audioFrame.Process(ref _recorderBuffer);

                if (_audioFrame.IsEventActive == true)
                {
                    if (_isSaving == false && Properties.Settings.Default.SettingIsSaving == true)
                    {
                        _sampleFilename = signal_filename;
                        _timeLastDetection = DateTime.Now;
                        _isSaving = true;
                    }
                    else
                    {
                        _timeLastDetection = DateTime.Now;
                    }
                    //Invoke(new MethodInvoker(AmplitudeEvent));
                    AmplitudeEvent();
                }

                if (_isSaving == true && DateTime.Now.Subtract(_timeLastDetection).Seconds > Properties.Settings.Default.SettingSecondsToSave)
                {
                    // felvétel lezárása

                    // HEADER + KÖRPUFFER TARTALOM


                    //byte[] korPuffer = new byte[circular_puffer_size];
                    //_streamMemorySmallBuffer.Read(korPuffer,0,korPuffer.Length);

                    //if (_streamMemorySmallBuffer.Position >= circular_puffer_size)
                    //{
                    //    _streamMemorySmallBuffer.Position = 0;
                    //}

                    //_streamMemory.Write(korPuffer, (int)_streamMemorySmallBuffer.Position, circular_puffer_size - (int)_streamMemorySmallBuffer.Position);

                    //_streamMemory.Write(korPuffer, 0, (int)_streamMemorySmallBuffer.Position);

                    ////----kp

                    //int counter = 0;

                    //byte[] korPuffer = new byte[64000];


                    //for (int i = 0; i < korPuffer.Length; i++)
                    //{
                    //    korPuffer[counter] = (byte)cBuff.Dequeue();
                    //    counter++;
                    //}

                    //_streamMemory.Write(korPuffer, 0, korPuffer.Length);

                    ////----kp



                    //_streamMemory.Write(bf1, 0, bf1.Length);


                    byte[] preBuffer = new byte[Properties.Settings.Default.SettingBitsPerSample];
                    _streamWave = WaveStream.CreateStream(_streamMemory, _waveFormat);

                    //preBuffer = new byte[_streamWave.Length - _streamWave.Position];
                    long wavStPos = 0;

                    if (_streamWave.Position - 3000 < 1)
                    {
                        wavStPos = 0;
                    }
                    else
                    {
                        wavStPos = _streamWave.Position - 3000;
                    }


                    preBuffer = new byte[wavStPos];

                    _streamWave.Read(preBuffer, 0, preBuffer.Length);




                    //----------------------------------------
                    //----------------------------------------

                    // FELVETT WAV (_streamWave)

                    byte[] waveBuffer = new byte[Properties.Settings.Default.SettingBitsPerSample];
                    // _streamWave = WaveStream.CreateStream(_streamMemory, _waveFormat);

                    waveBuffer = new byte[_streamWave.Length];
                    _streamWave.Read(waveBuffer, 0, waveBuffer.Length);

                    //----------------------------------------
                    //----------------------------------------

                    try
                    {
                        File.Delete(Properties.Settings.Default.SettingOutputPath + "\\" + signal_filename);
                    }
                    catch (Exception)
                    { }

                    try
                    {
                        if (Properties.Settings.Default.SettingOutputPath != "")
                            _streamFile = new FileStream(Properties.Settings.Default.SettingOutputPath + "\\" + _sampleFilename, FileMode.Create);
                        else
                            _streamFile = new FileStream(_sampleFilename, FileMode.Create);


                        _streamFile.Write(preBuffer, 0, preBuffer.Length);

                        _streamFile.Write(waveBuffer, 0, waveBuffer.Length);


                        if (_streamWave != null) { _streamWave.Close(); }
                        if (_streamFile != null) { _streamFile.Close(); }
                        _streamMemory = new MemoryStream();
                        _isSaving = false;

                        //throw new Exception("BR");

                        //cBuff.Clear();


                        //Invoke(new MethodInvoker(FileSavedEvent));

                    }
                    catch (Exception)
                    { }

                    FileSavedEvent();



                }
            }
        }


        private void AmplitudeEvent()
        {

        }


        private void FileSavedEvent()
        {
            if (AudioEventDetected != null)
            {
                AudioEventDetected();
            }
        }
               

    }
}
