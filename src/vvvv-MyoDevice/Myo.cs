#region usings
using System;
using System.ComponentModel.Composition;

//using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VMath;
using VVVV.Core.Logging;

using MyoSharp.Communication;
using MyoSharp.Device;
using MyoSharp.Exceptions;

#endregion usings

namespace VVVV.Nodes
{
    #region PluginInfo
    [PluginInfo(
        Name = "Myo", 
        Category = "Devices", 
        AutoEvaluate = true, 
        Help = "Provides tracking and gesture data of single Myo", 
        Tags = "tracking, arm, gesture, EMG", 
        Author = "motzi",
        Credits = "MyoSharp (Nick Cosentino and Tayfun Uzun)",
        Bugs = "Can be used with a single Myo only")]
    #endregion PluginInfo
    public class DevicesMyoNode : IPluginEvaluate, IDisposable, IPartImportsSatisfiedNotification
    {
        #region fields & pins
        [Input("Enabled", DefaultValue = 0.0, IsSingle = true)]
        public IDiffSpread<bool> FEnabled;

        [Input("Unlock Timed", DefaultValue = 0.0, IsSingle = true)]
        public IDiffSpread<bool> FUnlockTimed;

        [Input("Unlock Hold", IsSingle = true, IsBang = true)]
        public IDiffSpread<bool> FUnlockHold;

        [Input("Lock", IsSingle = true, IsBang = true)]
        public IDiffSpread<bool> FLock;

        [Input("Vibrate Short", IsSingle = true, IsBang = true)]
        public IDiffSpread<bool> FVibShort;

        [Input("Vibrate Medium", IsSingle = true, IsBang = true)]
        public IDiffSpread<bool> FVibMed;

        [Input("Vibrate Long", IsSingle = true, IsBang = true)]
        public IDiffSpread<bool> FVibLong;

        [Input("Request RSSI", IsSingle = true, IsBang = true)]
        public IDiffSpread<bool> FRssiReq;

        [Output("Unlocked")]
        public ISpread<bool> FUnlocked;

        [Output("Arm")]
        public ISpread<String> FArm;

        [Output("Pose")]
        public ISpread<String> FPose;

        [Output("Available Poses")]
        public ISpread<String> FPosesAvailable;

        [Output("Wave In", IsSingle = true, IsBang = true)]
        public ISpread<bool> FWaveIn;
        [Output("Wave Out", IsSingle = true, IsBang = true)]
        public ISpread<bool> FWaveOut;
        [Output("Spreaded Fingers", IsSingle = true, IsBang = true)]
        public ISpread<bool> FFingers;
        [Output("Fist", IsSingle = true, IsBang = true)]
        public ISpread<bool> FFist;
        [Output("Double Tap", IsSingle = true, IsBang = true)]
        public ISpread<bool> FDoubleTap;

        [Output("Quaternion")]
        public ISpread<Vector4D> FQuat;

        [Output("Rotate")]
        public ISpread<Vector3D> FRotate;

        [Output("Gyroscope")]
        public ISpread<Vector3D> FGyro;

        [Output("Accelerometer")]
        public ISpread<Vector3D> FAcc;
        
        [Output("EMG")]
        public ISpread<int> Femg;

        [Output("EMG Data Received", IsBang = true, IsSingle = true)]
        public ISpread<bool> FEmgReceived;

        [Output("RSSI")]
        public ISpread<float> FRssi;


        private IChannel _channel;
        private IHub _hub;

        private const int NUMBER_OF_SENSORS = 8;

        private bool _emg = false;

        [Import()]
        public ILogger FLogger;
        #endregion fields & pins

        public void OnImportsSatisfied()
        {
            setOutputSliceCount(0);
            FPosesAvailable.SliceCount = 7;
            FPosesAvailable[0] = MyoSharp.Poses.Pose.Rest.ToString();
            FPosesAvailable[1] = MyoSharp.Poses.Pose.Fist.ToString();
            FPosesAvailable[2] = MyoSharp.Poses.Pose.WaveIn.ToString();
            FPosesAvailable[3] = MyoSharp.Poses.Pose.WaveOut.ToString();
            FPosesAvailable[4] = MyoSharp.Poses.Pose.FingersSpread.ToString();
            FPosesAvailable[5] = MyoSharp.Poses.Pose.DoubleTap.ToString();
        }

        //called when data for any output pin is requested
        public void Evaluate(int SpreadMax)
        {
            if (FEnabled.IsChanged)
            {
                if (FEnabled[0])  // TODO: adapt for every connected Myo
                {
                    EnableMyo();
                    _channel.StartListening();
                }

                else
                {
                    try
                    {
                        _channel.StopListening();
                        this.Dispose();
                    }
                    catch (Exception e) { }

                }
            }

            if (_hub != null) // TODO: Check if this is valid
            {             
                if (FUnlockHold.IsChanged)   
                {
                    if (FUnlockHold[0]) // TODO: adapt for every connected Myo
                    {
                        foreach (var m in _hub.Myos)
                        {
                            m.Unlock(UnlockType.Hold);
                            FLogger.Log(LogType.Debug, "Unlocking Myo {0} (Hold)", m.Handle);
                        }
                    }
                }
                else if (FUnlockTimed.IsChanged && FUnlockTimed[0])
                {
                    if (FUnlockTimed[0]) // TODO: adapt for every connected Myo
                    {
                        foreach (var m in _hub.Myos)
                        {
                            m.Unlock(UnlockType.Timed);
                            FLogger.Log(LogType.Debug, "Unlocking Myo {0} (Timed)", m.Handle);
                        }
                    }
                }

                if (FLock.IsChanged && FLock[0])
                {
                    if (FLock[0]) // TODO: adapt for every connected Myo
                    {
                        foreach (var m in _hub.Myos)
                        {
                            m.Lock();
                            FLogger.Log(LogType.Debug, "Locking Myo {0}", m.Arm);
                        }
                    }
                }

                if (FVibShort.IsChanged)
                {
                    if(FVibShort[0])
                    {
                        foreach (var m in _hub.Myos)
                        {
                            m.Vibrate(VibrationType.Short);
                        }
                    }
                }

                if (FVibMed.IsChanged)
                {
                    if (FVibMed[0])
                    {
                        foreach (var m in _hub.Myos)
                        {
                            m.Vibrate(VibrationType.Medium);
                        }
                    }
                }

                if (FVibLong.IsChanged)
                {
                    if (FVibLong[0])
                    {
                        foreach (var m in _hub.Myos)
                        {
                            m.Vibrate(VibrationType.Long);
                        }
                    }
                }

                if (FRssiReq.IsChanged)
                {
                    if (FRssiReq[0])
                    {
                        foreach (var m in _hub.Myos)
                        {
                            m.RequestRssi();
                        }
                    }
                }
            }

            if(_emg)
            {
                FEmgReceived[0] = true;
                _emg = false;
            }
            else
            {
                FEmgReceived[0] = false;
            }

        }

        private void EnableMyo()
        {
            // get set up to listen for Myo events
            try
            {
                _channel = Channel.Create(
                    ChannelDriver.Create(
                    ChannelBridge.Create(),
                         MyoErrorHandlerDriver.Create(
                             MyoErrorHandlerBridge.Create())));

            }
            catch (Exception e)
            {
                FLogger.Log(LogType.Debug, "Myo: Could not create Channel...");
            }

            try
            {
                _hub = Hub.Create(_channel);
                _hub.MyoConnected += Hub_MyoConnected;
                _hub.MyoDisconnected += Hub_MyoDisconnected;
            }
            catch (Exception e)
            {
                FLogger.Log(LogType.Debug, "Myo: Could not create Hub...");
            }
            FLogger.Log(LogType.Debug, "Myo Hub Activated");
        }

        

        private void setOutputSliceCount(int count)
        {
            FQuat.SliceCount = count;
            FRotate.SliceCount = count;
            FAcc.SliceCount = count;
            FGyro.SliceCount = count;
            FPose.SliceCount = count;
            FUnlocked.SliceCount = count;
            FArm.SliceCount = count;
            Femg.SliceCount = count * NUMBER_OF_SENSORS;
            FRssi.SliceCount = count;
        }

        #region Event Handlers
        private void Hub_MyoDisconnected(object sender, MyoEventArgs e)
        {
            e.Myo.EmgDataAcquired -= Myo_EmgDataAcquired;
            e.Myo.SetEmgStreaming(false);
            e.Myo.Locked -= Myo_Locked;
            e.Myo.Unlocked -= Myo_Unlocked;

            e.Myo.PoseChanged -= Myo_PoseChanged;
            e.Myo.OrientationDataAcquired -= Myo_OrientationDataAcquired;
            e.Myo.AccelerometerDataAcquired -= Myo_AccelerometerDataAcquired;
            e.Myo.GyroscopeDataAcquired -= Myo_GyroscopeDataAcquired;
            e.Myo.Rssi -= Myo_RssiAcquired;

            setOutputSliceCount(0);
           
            FLogger.Log(LogType.Debug, "{0} Myo disconnected!", e.Myo.Handle);
        }

        private void Hub_MyoConnected(object sender, MyoEventArgs e)
        {
            e.Myo.EmgDataAcquired += Myo_EmgDataAcquired;
            e.Myo.SetEmgStreaming(true);
            e.Myo.Locked += Myo_Locked;
            e.Myo.Unlocked += Myo_Unlocked;
            
            e.Myo.PoseChanged += Myo_PoseChanged;
            e.Myo.OrientationDataAcquired += Myo_OrientationDataAcquired;
            e.Myo.AccelerometerDataAcquired += Myo_AccelerometerDataAcquired;
            e.Myo.GyroscopeDataAcquired += Myo_GyroscopeDataAcquired;
            e.Myo.Rssi += Myo_RssiAcquired;
            
            setOutputSliceCount(1);

            e.Myo.Vibrate(VibrationType.Short);

            FLogger.Log(LogType.Debug, "{0} Myo connected!", e.Myo.Handle);
        }

        private void Myo_EmgDataAcquired(object sender, EmgDataEventArgs e)
        {
            // pull data from each sensor
            for (var i = 0; i < NUMBER_OF_SENSORS; ++i)
            {
                Femg[i] = e.EmgData.GetDataForSensor(i);
                // TODO: e.Timestamp
            }
            _emg = true;
        }

        private void Myo_Unlocked(object sender, MyoEventArgs e)
        {
            FUnlocked[0] = true;
            FLogger.Log(LogType.Debug, "{0} arm Myo has unlocked!", e.Myo.Arm);
        }

        private void resetGestureOutputs()
        {
            FWaveIn[0] = false;
            FWaveOut[0] = false;
            FFingers[0] = false;
            FFist[0] = false;
            FDoubleTap[0] = false;
        }

        private void Myo_Locked(object sender, MyoEventArgs e)
        {
            FUnlocked[0] = false;

            resetGestureOutputs();

            FPose[0] = "";
            FArm[0] = "";

            FLogger.Log(LogType.Debug, "{0} arm Myo has locked!", e.Myo.Arm);
        }

        private void Myo_PoseChanged(object sender, PoseEventArgs e)
        {
            FArm[0] = e.Myo.Arm.ToString();
            FPose[0] = e.Myo.Pose.ToString();

            resetGestureOutputs();
            
            // TODO: check whether HeldPose might be more appropriate
            if (e.Myo.Pose == MyoSharp.Poses.Pose.Fist)
            {
                FFist[0] = true;
            }
            else if (e.Myo.Pose == MyoSharp.Poses.Pose.WaveIn)
            {
                FWaveIn[0] = true;
            }
                
            else if (e.Myo.Pose == MyoSharp.Poses.Pose.WaveOut)
            {
                FWaveOut[0] = true;
            }
            else if (e.Myo.Pose == MyoSharp.Poses.Pose.FingersSpread)
            {
                FFingers[0] = true;
            }
            
            else if (e.Myo.Pose == MyoSharp.Poses.Pose.DoubleTap)
            {
                FDoubleTap[0] = true;
            }
            else if (e.Myo.Pose == MyoSharp.Poses.Pose.Rest)
            {
                // do nothing
            }

            FLogger.Log(LogType.Debug, "{0} arm Myo detected {1} pose!", e.Myo.Arm, e.Myo.Pose);
        }
        private void Myo_OrientationDataAcquired(object sender, OrientationDataEventArgs e)
        {
            FRotate[0] = new Vector3D(e.Pitch, e.Yaw, e.Roll);
            FQuat[0] = new Vector4D(e.Orientation.X, e.Orientation.Y, e.Orientation.Z, e.Orientation.W);
        }
        private void Myo_AccelerometerDataAcquired(object sender, AccelerometerDataEventArgs e)
        {
            FAcc[0] = new Vector3D(e.Accelerometer.X, e.Accelerometer.Y, e.Accelerometer.Z);
        }
        private void Myo_GyroscopeDataAcquired(object sender, GyroscopeDataEventArgs e)
        {
            FGyro[0] = new Vector3D(e.Gyroscope.X, e.Gyroscope.Y, e.Gyroscope.Z);
        }
        private void Myo_RssiAcquired(object sender, RssiEventArgs e)
        {
            FRssi[0] = e.Rssi;
        }

        #endregion

        public void Dispose()
        {
            FLogger.Log(LogType.Debug, "Disposing Myo...");
            _hub.Dispose();
            _channel.Dispose();
            setOutputSliceCount(0);
            FLogger.Log(LogType.Debug, "...Done Disposing Myo");
        }

    }
}
