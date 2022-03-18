using System;
using System.Runtime;
using SduHealthier.Core;
using Xunit;
using static SduHealthier.Core.Utilities.Encrypting;

namespace SduHealthier.UnitTest
{
    public class EncryptTest
    {
        [Fact]
        public void MainTest()
        {
            var rsa = GenerateRsa("201900600054abczxw~!ltltltlt");
            Assert.Equal(
                "92D3FF43A7879E85F7D6C0E0A56C0BCE169743A51B5E10091AA9E766493C3B6505D03169081606FB693110503DB38B62693110503DB38B62",
                rsa);
        }

        [Fact]
        public void ExtendByte()
        {
            var str = "201900600054abczxw~!ltltltlt";
            Assert.Equal(
                new byte[]
                {
                    0, 50, 0, 48, 0, 49, 0, 57, 0, 48, 0, 48, 0, 54, 0, 48, 0, 48, 0, 48, 0, 53, 0, 52, 0, 97, 0, 98, 0,
                    99, 0, 122, 0, 120, 0, 119, 0, 126, 0, 33, 0, 108, 0, 116, 0, 108, 0, 116, 0, 108, 0, 116, 0, 108,
                    0, 116
                }
                , ExtendTo16Bits(str));

            Assert.Equal(
                new byte[] {0, 49, 0, 0, 0, 0, 0, 0},
                ExtendTo16Bits("1"));
        }
    }
}