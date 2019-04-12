using System;

namespace Serial1602ShieldSystemUIController.Tests
{
    public class MockSerialClientWrapper : SerialClientWrapper
    {
        public bool EnableBaseFunctionality = false;

        public string MockReadLine;
        public string MockWriteLine;
        public string Output;

        public bool MockIsOpen;
        public bool MockHasData;

        public override bool IsOpen {
            get {
                if (EnableBaseFunctionality)
                    return base.IsOpen;
                else
                    return MockIsOpen;
            }
        }

        public override bool HasData {
            get {
                if (EnableBaseFunctionality)
                    return base.HasData;
                else
                    return MockHasData;
            }
        }

        public MockSerialClientWrapper ()
        {
        }

        public override void Open ()
        {
            if (EnableBaseFunctionality)
                base.Open ();
        }

        public override string ReadLine ()
        {
            if (EnableBaseFunctionality)
                return base.ReadLine ();
            else
                return MockReadLine;
        }

        public override void WriteLine (string text)
        {
            if (EnableBaseFunctionality)
                base.WriteLine (text);

            MockWriteLine = text;
            Output += text + Environment.NewLine;
        }

        public void ClearOutput ()
        {
            Output = "";
        }
    }
}

