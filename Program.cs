using GHIElectronics.TinyCLR.Devices.Gpio;
using GHIElectronics.TinyCLR.Devices.Spi;
using GHIElectronics.TinyCLR.Networking.SPWF04Sx;
using GHIElectronics.TinyCLR.Pins;
using MFRC522;
using System;
using System.Collections;
using System.Diagnostics;
using System.Text;
using System.Threading;

namespace NFC
{
    class Program {
        private static SPWF04SxInterface wifi;

        static void Main() {
            var cont = GpioController.GetDefault();
            var reset = cont.OpenPin(FEZ.GpioPin.D9);
            var settings = new SpiConnectionSettings(FEZ.GpioPin.D10) {
                Mode = SpiMode.Mode0,
                ClockFrequency = 1000000,
                SharingMode = SpiSharingMode.Exclusive,
                DataBitLength = 8,
            };

            var spi = SpiDevice.FromId(FEZ.SpiBus.Spi1, settings);

            RC522 reader = new RC522(spi, reset);
            //reader.SelfTest();
            while (true) {
                Thread.Sleep(100);
                Debug.WriteLine(reader.CheckTag().ToString("X2"));
            }
        }
    }
}
