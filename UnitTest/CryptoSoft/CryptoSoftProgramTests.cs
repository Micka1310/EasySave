using System.Text;
using CryptoSoft;

// Tests Program.Main : arguments, fichier manquant, chiffrement, mono-instance (mutex global).
// Le mutex Windows est réentrant sur le même thread : le blocage « autre instance » doit
// être simulé depuis un thread distinct.
[TestClass]
[DoNotParallelize]
public class CryptoSoftProgramTests
{
    [TestMethod]
    public void Main_InvalidUsage_Returns2()
    {
        Assert.AreEqual(2, Program.Main([]));
        Assert.AreEqual(2, Program.Main(["--encrypt"]));
        Assert.AreEqual(2, Program.Main(["--wrong", "x"]));
    }

    [TestMethod]
    public void Main_FileNotFound_Returns3()
    {
        string missing = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + "_nope.bin");
        Assert.AreEqual(3, Program.Main(["--encrypt", missing]));
    }

    [TestMethod]
    public void Main_ValidFile_Returns0_And_ModifiesFile()
    {
        string path = Path.GetTempFileName();
        try
        {
            File.WriteAllText(path, "secret", Encoding.UTF8);
            byte[] before = File.ReadAllBytes(path);

            int code = Program.Main(["--encrypt", path]);

            Assert.AreEqual(0, code);
            CollectionAssert.AreNotEqual(before, File.ReadAllBytes(path));
        }
        finally
        {
            try { File.Delete(path); } catch { /* ignore */ }
        }
    }

    [TestMethod]
    public void Main_DoubleEncrypt_RestoresOriginalBytes()
    {
        string path = Path.GetTempFileName();
        try
        {
            File.WriteAllBytes(path, [1, 2, 3, 4, 5]);
            byte[] original = File.ReadAllBytes(path);

            Assert.AreEqual(0, Program.Main(["--encrypt", path]));
            Assert.AreEqual(0, Program.Main(["--encrypt", path]));

            CollectionAssert.AreEqual(original, File.ReadAllBytes(path));
        }
        finally
        {
            try { File.Delete(path); } catch { /* ignore */ }
        }
    }

    [TestMethod]
    public void Main_WhenSingleInstanceMutexAlreadyHeld_Returns4_And_DoesNotEncrypt()
    {
        string path = Path.GetTempFileName();
        try
        {
            File.WriteAllText(path, "plain", Encoding.UTF8);
            byte[] before = File.ReadAllBytes(path);

            using ManualResetEventSlim releaseHolder = new(false);
            using ManualResetEventSlim holderReady = new(false);
            Exception? holderError = null;

            Thread holderThread = new(() =>
            {
                try
                {
                    // initialOwned=true ne garantit pas la propriété si le mutex existe déjà.
                    using Mutex blocker = new(false, Program.SingleInstanceMutexName);
                    if (!blocker.WaitOne(TimeSpan.FromSeconds(10)))
                    {
                        throw new TimeoutException("Acquisition du mutex pour le test mono-instance.");
                    }

                    holderReady.Set();
                    releaseHolder.Wait(TimeSpan.FromSeconds(30));
                }
                catch (Exception ex)
                {
                    holderError = ex;
                    holderReady.Set();
                }
            });

            holderThread.IsBackground = true;
            holderThread.Start();
            Assert.IsTrue(holderReady.Wait(TimeSpan.FromSeconds(5)), "Le thread détenteur du mutex devrait signaler qu’il est prêt.");
            if (holderError is not null)
                Assert.Fail(holderError.ToString());

            try
            {
                int code = Program.Main(["--encrypt", path]);

                Assert.AreEqual(4, code);
                CollectionAssert.AreEqual(before, File.ReadAllBytes(path));
            }
            finally
            {
                releaseHolder.Set();
                Assert.IsTrue(holderThread.Join(TimeSpan.FromSeconds(15)), "Le thread détenteur du mutex devrait se terminer.");
            }
        }
        finally
        {
            try { File.Delete(path); } catch { /* ignore */ }
        }
    }

    [TestMethod]
    public void Main_AfterReleasingBlocker_EncryptSucceeds()
    {
        string path = Path.GetTempFileName();
        try
        {
            File.WriteAllText(path, "data", Encoding.UTF8);
            byte[] before = File.ReadAllBytes(path);

            using ManualResetEventSlim releaseHolder = new(false);
            using ManualResetEventSlim holderReady = new(false);

            Thread holderThread = new(() =>
            {
                using Mutex blocker = new(false, Program.SingleInstanceMutexName);
                if (!blocker.WaitOne(TimeSpan.FromSeconds(10)))
                {
                    throw new TimeoutException("Acquisition du mutex pour le test mono-instance.");
                }

                holderReady.Set();
                releaseHolder.Wait(TimeSpan.FromSeconds(30));
            });

            holderThread.IsBackground = true;
            holderThread.Start();
            Assert.IsTrue(holderReady.Wait(TimeSpan.FromSeconds(5)));

            try
            {
                Assert.AreEqual(4, Program.Main(["--encrypt", path]));
            }
            finally
            {
                releaseHolder.Set();
                Assert.IsTrue(holderThread.Join(TimeSpan.FromSeconds(15)));
            }

            Assert.AreEqual(0, Program.Main(["--encrypt", path]));
            CollectionAssert.AreNotEqual(before, File.ReadAllBytes(path));
        }
        finally
        {
            try { File.Delete(path); } catch { /* ignore */ }
        }
    }
}
