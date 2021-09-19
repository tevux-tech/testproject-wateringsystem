using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DevBot9.Protocols.Homie;
using DevBot9.Protocols.Homie.Utilities;
using System.Diagnostics;
using System.Device.Gpio;
using System.Threading;

namespace WateringSystem {
    class SprayerProducer {
        public GpioController controller;
        public int[] sprayers;


        private PahoHostDeviceConnection _broker;
        private HostDevice _hostDevice;

        private HostChoiceProperty _mode;

        private HostChoiceProperty _sprayer1;
        private HostChoiceProperty _sprayer2;
        private HostChoiceProperty _sprayer3;

        private HostNumberProperty _sprayer1OnTime;
        private HostNumberProperty _sprayer1OffTime;
        private HostNumberProperty _sprayer2OnTime;
        private HostNumberProperty _sprayer2OffTime;
        private HostNumberProperty _sprayer3OnTime;
        private HostNumberProperty _sprayer3OffTime;

        private HostChoiceProperty _sprayer1StartStop;
        private HostChoiceProperty _sprayer2StartStop;
        private HostChoiceProperty _sprayer3StartStop;

        private HostNumberProperty _sequentialOnTime;
        private HostChoiceProperty _sequentialStartStop;

        private HostNumberProperty _allTogetherOnTime;
        private HostNumberProperty _allTogetherOffTime;

        private HostChoiceProperty _allTogetherStartStop;

        private HostChoiceProperty _purgeAll;

        private CancellationTokenSource _tokenSource = new();
        private CancellationToken _token = new();


        public SprayerProducer() {
            _broker = new PahoHostDeviceConnection();
        }

        public void Initialize(string mqttBrokerIpAddress, AddToLogDelegate addToLog) {
            _token = _tokenSource.Token;
            
            _hostDevice = DeviceFactory.CreateHostDevice("rpi-sprayers", "Nice sprayers on RPi");
            
            #region Mode selector

            _hostDevice.UpdateNodeInfo("mode-selector","Mode selector","no-type");

            _mode = _hostDevice.CreateHostChoiceProperty(PropertyType.Parameter, "mode-selector", "mode", "Select mode", new[] { "Manual", "Timed", "Sequential", "All together", "Purge" }, "Manual");
            _mode.PropertyChanged += (sender, e) => {
                _tokenSource.Cancel();
                for (int i=0; i<sprayers.Length; i++){
                    controller.Write(sprayers[i], PinValue.Low);
                }
                _tokenSource.Dispose();
                _tokenSource = new();
                _token = _tokenSource.Token;
            };

            #endregion

            #region Manual control

            _hostDevice.UpdateNodeInfo("manual-control", "Control sprayers manually", "no-type");

            _sprayer1 = _hostDevice.CreateHostChoiceProperty(PropertyType.Parameter, "manual-control", "sprayer1", "Sprayer 1", new[] { "OFF", "ON" }, "OFF");
            _sprayer1.PropertyChanged += (sender, e) => {
                if (_mode.Value == "Manual") {
                    Task.Run(() => {
                        if (_sprayer1.Value == "ON") {
                            controller.Write(sprayers[0], PinValue.High);
                        } else {
                            controller.Write(sprayers[0], PinValue.Low);
                        }

                    }, _token);
                }
            };

            _sprayer2 = _hostDevice.CreateHostChoiceProperty(PropertyType.Parameter, "manual-control", "sprayer2", "Sprayer 2", new[] { "OFF", "ON" }, "OFF");
            _sprayer2.PropertyChanged += (sender, e) => {
                if (_mode.Value == "Manual") {
                    Task.Run(() => {
                        if (_sprayer2.Value == "ON") {
                            controller.Write(sprayers[1], PinValue.High);
                        } else {
                            controller.Write(sprayers[1], PinValue.Low);
                        }

                    }, _token);
                }
            };

            _sprayer3 = _hostDevice.CreateHostChoiceProperty(PropertyType.Parameter, "manual-control", "sprayer3", "Sprayer 3", new[] { "OFF", "ON" }, "OFF");
            _sprayer3.PropertyChanged += (sender, e) => {
                if (_mode.Value == "Manual") {
                    Task.Run(() => {
                        if (_sprayer3.Value == "ON") {
                            controller.Write(sprayers[2], PinValue.High);
                        } else {
                            controller.Write(sprayers[2], PinValue.Low);
                        }

                    }, _token);
                }
            };

            #endregion

            #region Timed control

            _hostDevice.UpdateNodeInfo("timed-control", "Automatic timed sprayer control", "no-type");

            _sprayer1OnTime = _hostDevice.CreateHostNumberProperty(PropertyType.Parameter, "timed-control", "sprayer1-ontime", "Sprayer 1 on time", 1, "s");
            _sprayer1OffTime = _hostDevice.CreateHostNumberProperty(PropertyType.Parameter, "timed-control", "sprayer1-offtime", "Sprayer 1 off time", 1, "s");
            _sprayer2OnTime = _hostDevice.CreateHostNumberProperty(PropertyType.Parameter, "timed-control", "sprayer2-ontime", "Sprayer 2 on time", 1, "s");
            _sprayer2OffTime = _hostDevice.CreateHostNumberProperty(PropertyType.Parameter, "timed-control", "sprayer2-offtime", "Sprayer 2 off time", 1, "s");
            _sprayer3OnTime = _hostDevice.CreateHostNumberProperty(PropertyType.Parameter, "timed-control", "sprayer3-ontime", "Sprayer 3 on time", 1, "s");
            _sprayer3OffTime = _hostDevice.CreateHostNumberProperty(PropertyType.Parameter, "timed-control", "sprayer3-offtime", "Sprayer 3 off time", 1, "s");

            _sprayer1StartStop = _hostDevice.CreateHostChoiceProperty(PropertyType.Parameter, "timed-control", "sprayer1-startstop", "Start or stop sprayer 1", new[] { "Start", "Stop" }, "Stop");
            _sprayer2StartStop = _hostDevice.CreateHostChoiceProperty(PropertyType.Parameter, "timed-control", "sprayer2-startstop", "Start or stop sprayer 2", new[] { "Start", "Stop" }, "Stop");
            _sprayer3StartStop = _hostDevice.CreateHostChoiceProperty(PropertyType.Parameter, "timed-control", "sprayer3-startstop", "Start or stop sprayer 3", new[] { "Start", "Stop" }, "Stop");

            _sprayer1StartStop.PropertyChanged += (sender, e) => {
                if (_mode.Value == "Timed") {
                    var count = 0;
                    if (_sprayer1StartStop.Value == "Start") {
                        Task.Run(() => {
                            while (true) {
                                if (count == _sprayer1OnTime.Value + _sprayer1OffTime.Value) count = 0;
                                else if (count < _sprayer1OnTime.Value) {
                                    controller.Write(sprayers[0], PinValue.High);
                                    count++;
                                } else {
                                    controller.Write(sprayers[0], PinValue.Low);
                                    count++;
                                }
                                Thread.Sleep(1000);
                            }
                        }, _token);
                    } else { 
                        _tokenSource.Cancel();
                    }
                }
            };

            _sprayer2StartStop.PropertyChanged += (sender, e) => {
                if (_mode.Value == "Timed") {
                    var count = 0;
                    if (_sprayer2StartStop.Value == "Start") {
                        Task.Run(() => {
                            while (true) {
                                if (count == _sprayer2OnTime.Value + _sprayer2OffTime.Value) count = 0;
                                else if (count < _sprayer1OnTime.Value) {
                                    controller.Write(sprayers[1], PinValue.High);
                                    count++;
                                } else {
                                    controller.Write(sprayers[1], PinValue.Low);
                                    count++;
                                }
                                Thread.Sleep(1000);
                            }
                        }, _token);
                    } else {
                        _tokenSource.Cancel();
                    }
                }
            };

            _sprayer3StartStop.PropertyChanged += (sender, e) => {
                if (_mode.Value == "Timed") {
                    var count = 0;
                    if (_sprayer3StartStop.Value == "Start") {
                        Task.Run(() => {
                            while (true) {
                                if (count == _sprayer3OnTime.Value + _sprayer3OffTime.Value) count = 0;
                                else if (count < _sprayer3OnTime.Value) {
                                    controller.Write(sprayers[2], PinValue.High);
                                    count++;
                                } else {
                                    controller.Write(sprayers[2], PinValue.Low);
                                    count++;
                                }
                                Thread.Sleep(1000);
                            }
                        }, _token);
                    } else {
                        _tokenSource.Cancel();
                    }
                }
            };

            #endregion

            #region Sequential control

            _hostDevice.UpdateNodeInfo("sequential-control", "Sequential sprayer control", "no-type");

            _sequentialOnTime = _hostDevice.CreateHostNumberProperty(PropertyType.Parameter, "sequential-control", "sequential-ontime", "Sequential on time", 1, "s");
            _sequentialStartStop = _hostDevice.CreateHostChoiceProperty(PropertyType.Parameter, "sequential-control", "sequential-startstop", "Start or stop sequential control", new[] { "Start", "Stop" }, "Stop");

            _sequentialStartStop.PropertyChanged += (sender, e) => {
                if (_mode.Value == "Sequential") {
                    var count = 0;
                    if (_sequentialStartStop.Value == "Start") {
                        Task.Run(() => {
                            while (true) {
                                controller.Write(sprayers[count], PinValue.High);
                                for (int i = 0; i < _sequentialOnTime.Value; i++) {
                                    Thread.Sleep(1000);
                                }
                                controller.Write(sprayers[count], PinValue.Low);
                                if (count % sprayers.Length == sprayers.Length - 1) count = 0;
                                else count++;
                            }
                        }, _token);
                    } else {
                        _tokenSource.Cancel();
                    }
                }

            };

            #endregion

            #region All together

            _hostDevice.UpdateNodeInfo("alltogether-control", "Sprayer control for all sprayers together", "no-type");

            _allTogetherOnTime = _hostDevice.CreateHostNumberProperty(PropertyType.Parameter, "alltogether-control", "alltogether-ontime", "On time for all sprayers", 1, "s");
            _allTogetherOffTime = _hostDevice.CreateHostNumberProperty(PropertyType.Parameter, "alltogether-control", "alltogether-offtime", "Off time for all sprayers", 1, "s");

            _allTogetherStartStop = _hostDevice.CreateHostChoiceProperty(PropertyType.Parameter, "alltogether-control", "alltogether-startstop", "Start or stop all sprayers", new[] { "Start", "Stop" }, "Stop");

            _allTogetherStartStop.PropertyChanged += (sender, e) => {
                if (_mode.Value == "All together") {
                    var count = 0;
                    if (_sprayer3StartStop.Value == "Start") {
                        Task.Run(() => {
                            while (true) {
                                if (count == _allTogetherOnTime.Value + _allTogetherOffTime.Value) count = 0;
                                else if (count < _allTogetherOnTime.Value) {
                                    for (int i = 0; i < sprayers.Length; i++) {
                                        controller.Write(sprayers[i], PinValue.High);
                                    }
                                    count++;
                                } else {
                                    for (int i = 0; i < sprayers.Length; i++) {
                                        controller.Write(sprayers[i], PinValue.Low);
                                    }
                                    count++;
                                }
                                Thread.Sleep(1000);
                            }
                        }, _token);
                    } else {
                        _tokenSource.Cancel();
                    }
                }
            };

            #endregion

            #region Purge all

            _hostDevice.UpdateNodeInfo("purgeall-control", "Sprayer control to purge all sprayers", "no-type");

            _purgeAll = _hostDevice.CreateHostChoiceProperty(PropertyType.Parameter, "purgeall-control", "purgeall-startstop", "Start or stop purge", new[] { "Start", "Stop" }, "Stop");

            _purgeAll.PropertyChanged += (sender, e) => {
                if (_mode.Value == "Purge") {
                    if (_sequentialStartStop.Value == "Start") {
                        for (int i = 0; i < sprayers.Length; i++) {
                            controller.Write(sprayers[i], PinValue.High);
                        }
                    } else {
                        _tokenSource.Cancel();
                    }
                }

            };

            #endregion

            _broker.Initialize(mqttBrokerIpAddress, (severity, message) => addToLog(severity, "Broker:" + message));
            _hostDevice.Initialize(_broker, (severity, message) => addToLog(severity, "HostDevice:" + message));
        }
    }
}
