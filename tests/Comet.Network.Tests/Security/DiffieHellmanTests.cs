using Comet.Network.Security;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Comet.Network.Tests.Security
{
    [TestClass]
    public class DiffieHellmanTests
    {
        [TestMethod]
        public void CreateTest()
        {
            Assert.IsNotNull(DiffieHellman.Create());
        }

        [TestMethod]
        public void InitializeTest()
        {
            DiffieHellman df1 = DiffieHellman.Create(),
                          df2 = DiffieHellman.Create();

            df1.Initialize(df2.PublicKey.ToByteArrayUnsigned(), df2.Modulus.ToByteArrayUnsigned());
            df2.Initialize(df1.PublicKey.ToByteArrayUnsigned(), df1.Modulus.ToByteArrayUnsigned());

            Assert.AreEqual(df1.SharedKey, df2.SharedKey);

        }
    }
}