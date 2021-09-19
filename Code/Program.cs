using System;
using System.Threading;
using System.Device.Gpio;
using System.Diagnostics;
using DevBot9.Protocols.Homie;

namespace WateringSystem {
    class Program {
        
        static void Main(string[] args) {
            void AddToLog(string severity, string message) {
                Debug.WriteLine($"{severity}:{message}");
            }
            GpioController controller = new();
            int[] sprayers = { 13, 19, 16 };
            controller.OpenPin(sprayers[0], PinMode.Output);
            controller.OpenPin(sprayers[1], PinMode.Output);
            controller.OpenPin(sprayers[2], PinMode.Output);

            var sprayerProducer = new SprayerProducer();

            DeviceFactory.Initialize();
            sprayerProducer.sprayers = sprayers;
            sprayerProducer.controller = controller;
            sprayerProducer.Initialize("localhost", (severity, message) => AddToLog(severity, "SprayerProducer:" + message));


            Thread.Sleep(-1);

            Debug.WriteLine("Exiting...");
        }
    }
}
