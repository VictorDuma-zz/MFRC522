using GHIElectronics.TinyCLR.Devices.Gpio;
using GHIElectronics.TinyCLR.Devices.Spi;
using System;
using System.Collections;
using System.Diagnostics;
using System.Text;
using System.Threading;

namespace MFRC522 {
    public abstract class Command {
        internal const byte PCD_TRANSCEIVE = 0x0C;
        internal const byte PCD_RESETPHASE = 0x0F;
        internal const byte PCD_IDLE = 0x00;
        internal const byte PCD_MEM = 0x01;
        internal const byte PCD_CRC = 0x03;
    }

        public abstract class Register  {
        internal const byte Anticollision_1   = 0x93;
        internal const byte Anticollision_2   = 0x20;
        internal const byte Command           = 0x01;
        internal const byte CommIEn           = 0x02;
        internal const byte Error             = 0x06;
        internal const byte FIFOData          = 0x09;
        internal const byte FIFOLevel         = 0x0A;
        internal const byte Control           = 0x0C;
        internal const byte BitFraming        = 0x0D;
        internal const byte Coll              = 0x0E;
        internal const byte ComIrq            = 0x04;
        internal const byte Mode              = 0x11;
        internal const byte TxMode            = 0x12;
        internal const byte RxMode            = 0x13;
        internal const byte TxControl         = 0x14;
        internal const byte TxAuto            = 0x15;
        internal const byte MifareReg         = 0x1C;
        internal const byte ModeWidth         = 0x24;
        internal const byte RFCfg             = 0x26;
        internal const byte TMode             = 0x2A;
        internal const byte TPrescaler        = 0x2B;
        internal const byte TReloadRegH       = 0x2C;
        internal const byte TReloadRegL       = 0x2D;
        internal const byte AutoTest          = 0x36;
     }

    class RC522 : IDisposable {
        private readonly SpiDevice spi;
        private readonly GpioPin reset;
        private byte[] writeBuffer = new byte[2];
        private byte[] readBuffer = new byte[2];

        public RC522(SpiDevice spi, GpioPin reset) {
            this.spi = spi;
            this.reset = reset;
            this.reset.SetDriveMode(GpioPinDriveMode.Output);
            this.reset.Write(GpioPinValue.High);

            Init();
        }

        ~RC522() => this.Dispose(false);

        public void Dispose() {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing) {
            if (disposing) {
                this.spi.Dispose();
                this.reset.Dispose();
            }
        }

        public void SelfTest() {
            var emptyBuf = new byte[25];

            WriteRegister(Register.Command, Command.PCD_RESETPHASE); // reset
            WriteRegister(Register.FIFOLevel, 0x80);

            WriteToFifo(emptyBuf);
            WriteRegister(Register.Command, Command.PCD_MEM);

            WriteRegister(Register.AutoTest, 0x09);
            WriteToFifo(0x00);

            WriteRegister(Register.Command, Command.PCD_CRC);
            Thread.Sleep(500);
            var buffer = ReadFromFifo(64);
            foreach (var i in buffer)
                Debug.WriteLine(i.ToString("X2"));
        }

        public void Init() {
            WriteRegister(Register.Command, Command.PCD_RESETPHASE);

            WriteRegister(Register.TxMode, 0x00);
            WriteRegister(Register.RxMode, 0x00);
            WriteRegister(Register.ModeWidth, 0x26);

            WriteRegister(Register.TMode, 0x80);
            WriteRegister(Register.TPrescaler, 0xA9);
            WriteRegister(Register.TReloadRegL, 0xE8);
            WriteRegister(Register.TReloadRegH, 0x03);
            WriteRegister(Register.TxAuto, 0x40);
            WriteRegister(Register.Mode, 0x3D);

            AntennaOn();
        }

        public int CheckTag() {
            WriteRegister(Register.BitFraming, 0x07);
            TagHandler(Register.RFCfg);

            return ReadFromFifo();
        }

        public byte Anticollision() {
            WriteRegister(Register.BitFraming, 0x00);
            var data = new byte[2];
            data[0] = Register.Anticollision_1;
            data[1] = Register.Anticollision_2;
            TagHandler(data);

            return ReadFromFifo();
        }

        protected void TagHandler(params byte[] data) {
            WriteRegister(Register.Command, Command.PCD_IDLE);

            WriteRegister(Register.ComIrq, 0x7F);
            SetRegisterBits(Register.FIFOLevel, 0x80);

            WriteToFifo(data);

            WriteRegister(Register.Command, Command.PCD_TRANSCEIVE);
            SetRegisterBits(Register.BitFraming, 0x80);

            ClearRegisterBits(Register.BitFraming, 0x80);
            Thread.Sleep(25);
        }

        protected void SetRegisterBits(byte register, byte bits) {
            var currentValue = ReadRegister(register);
            WriteRegister(register, (byte)(currentValue | bits));
        }

        protected void ClearRegisterBits(byte register, byte bits) {
            var currentValue = ReadRegister(register);
            WriteRegister(register, (byte)(currentValue & ~bits));
        }

        protected void AntennaOn() {
            SetRegisterBits(Register.TxControl, 0x03);
        }

        protected byte[] ReadFromFifo(int length) {
            var buffer = new byte[length];

            for (int i = 0; i < length; i++) {
                buffer[i] = ReadRegister(Register.FIFOData);
                Thread.Sleep(10);
            }

            return buffer;
        }

        protected byte ReadFromFifo() {
            return ReadFromFifo(1)[0];
        }

        protected void WriteToFifo(params byte[] values) {
            foreach (var b in values)
                WriteRegister(Register.FIFOData, b);
        }

        protected int GetFifoLevel() {
            return ReadRegister(Register.FIFOLevel);
        }

        protected ushort ReadFromFifoShort() {
            var low = ReadRegister(Register.FIFOData);
            var high = (ushort)(ReadRegister(Register.FIFOData) << 8);

            return (ushort)(high | low);
        }

        public void WriteRegister(byte reg, byte value) {
            writeBuffer[0] = (byte)((reg << 1) & 0x7E);
            writeBuffer[1] = value;
            spi.Write(writeBuffer);
        }

        public byte ReadRegister(byte reg) {
            writeBuffer[0] = (byte)((reg << 1) | 0x80);
            writeBuffer[1] = 0;
            spi.TransferSequential(writeBuffer, readBuffer);
            return readBuffer[0];
        }

    }
}
