using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading;
using SDServer;
namespace SDServerTests
{
    [TestClass]
    public class SessionTableTests
    {
        private SessionTable sessionTable;

        [TestInitialize]
        public void Setup()
        {
            sessionTable = new SessionTable();
        }

        [TestCleanup]
        public void Cleanup()
        {
            sessionTable.Dispose();
        }

        [TestMethod]
        public void OpenSession_ShouldReturnUniqueSessionId()
        {
            ulong id1 = sessionTable.OpenSession();
            ulong id2 = sessionTable.OpenSession();
            Assert.AreNotEqual(id1, id2);
        }

        [TestMethod]
        public void ResumeSession_ShouldReturnTrueIfSessionExists()
        {
            ulong id = sessionTable.OpenSession();
            Assert.IsTrue(sessionTable.ResumeSession(id));
        }

        [TestMethod]
        public void ResumeSession_ShouldReturnFalseIfSessionDoesNotExist()
        {
            Assert.IsFalse(sessionTable.ResumeSession(9999));
        }

        [TestMethod]
        public void CloseSession_ShouldRemoveSession()
        {
            ulong id = sessionTable.OpenSession();
            sessionTable.CloseSession(id);
            Assert.IsFalse(sessionTable.ResumeSession(id));
        }

        [TestMethod]
        [ExpectedException(typeof(SessionException))]
        public void CloseSession_ShouldThrowIfAlreadyClosed()
        {
            ulong id = sessionTable.OpenSession();
            sessionTable.CloseSession(id);
            sessionTable.CloseSession(id); // should throw
        }

        [TestMethod]
        public void PutAndGetSessionValue_ShouldStoreAndRetrieveCorrectly()
        {
            ulong id = sessionTable.OpenSession();
            sessionTable.PutSessionValue(id, "key", "value");
            string? result = sessionTable.GetSessionValue(id, "key");
            Assert.AreEqual("value", result);
        }

        [TestMethod]
        public void GetSessionValue_ShouldReturnNullIfKeyNotPresent()
        {
            ulong id = sessionTable.OpenSession();
            string? result = sessionTable.GetSessionValue(id, "nonexistent");
            Assert.IsNull(result);
        }

        [TestMethod]
        [ExpectedException(typeof(SessionException))]
        public void GetSessionValue_ShouldThrowIfSessionClosed()
        {
            ulong id = sessionTable.OpenSession();
            sessionTable.CloseSession(id);
            sessionTable.GetSessionValue(id, "key"); // should throw
        }

        [TestMethod]
        [ExpectedException(typeof(SessionException))]
        public void PutSessionValue_ShouldThrowIfSessionClosed()
        {
            ulong id = sessionTable.OpenSession();
            sessionTable.CloseSession(id);
            sessionTable.PutSessionValue(id, "key", "value"); // should throw
        }

        [TestMethod]
        public void ExpiredSession_ShouldBeCleanedUp()
        {
            var shortTimeoutTable = new SessionTable_TestableCleanup(TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(50));
            ulong id = shortTimeoutTable.OpenSession();

            Thread.Sleep(300); // allow cleanup to run
            Assert.IsFalse(shortTimeoutTable.ResumeSession(id));

            shortTimeoutTable.Dispose();
        }


        [TestMethod]
        public void CanOpenAndResumeSession()
        {
            ulong sessionId = sessionTable.OpenSession();
            Assert.IsTrue(sessionTable.ResumeSession(sessionId));
        }

        [TestMethod]
        public void CannotResumeClosedSession()
        {
            ulong sessionId = sessionTable.OpenSession();
            sessionTable.CloseSession(sessionId);
            Assert.IsFalse(sessionTable.ResumeSession(sessionId));
        }

        [TestMethod]
        public void CanPutAndGetSessionValue()
        {
            ulong sessionId = sessionTable.OpenSession();
            sessionTable.PutSessionValue(sessionId, "foo", "bar");
            string? value = sessionTable.GetSessionValue(sessionId, "foo");
            Assert.AreEqual("bar", value);
        }

        [TestMethod]
        public void GetMissingSessionValueReturnsNull()
        {
            ulong sessionId = sessionTable.OpenSession();
            string? value = sessionTable.GetSessionValue(sessionId, "missing");
            Assert.IsNull(value);
        }

        [TestMethod]
        [ExpectedException(typeof(SessionException))]
        public void GetSessionValueThrowsIfClosed()
        {
            ulong sessionId = sessionTable.OpenSession();
            sessionTable.CloseSession(sessionId);
            sessionTable.GetSessionValue(sessionId, "foo");
        }

        [TestMethod]
        [ExpectedException(typeof(SessionException))]
        public void PutSessionValueThrowsIfClosed()
        {
            ulong sessionId = sessionTable.OpenSession();
            sessionTable.CloseSession(sessionId);
            sessionTable.PutSessionValue(sessionId, "foo", "bar");
        }

        // Helper subclass to allow configurable timeouts for testing
        class SessionTable_TestableCleanup : SessionTable
        {
            public SessionTable_TestableCleanup(TimeSpan timeout, TimeSpan interval)
                : base(timeout, interval)
            {
            }
        }
    }
}