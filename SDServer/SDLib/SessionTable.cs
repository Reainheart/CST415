// SessionTable.cs
//
// Pete Myers
// CST 415
// Fall 2019
// 
// Noah Etchemendy
// CST 415
// Spring 2025
// SD Server Implementation - .NET 9 Console App
// Attribution: Based on specifications provided in a university-level assignment prompt.

using System;
using System.Collections.Generic;
using System.Threading;

namespace SDServer
{
    class SessionException : Exception
    {
        public SessionException(string msg) : base(msg)
        {
        }
    }
    /// <summary>
    /// thread safe collection
    /// represents the SDServer's session table, where we track session data per client
    /// client sessions are identified by an unsigned long session ID
    /// session IDs are never reused
    /// when the session table is first created, it is empty, with no client session data
    /// client session data is made up of arbitrary key/value pairs, where each are text
    /// </summary>
    class SessionTable
    {
        private readonly object _lock = new object();
        private readonly TimeSpan sessionTimeout = TimeSpan.FromMinutes(30);
        private readonly TimeSpan cleanupInterval = TimeSpan.FromMinutes(5);
        private readonly CancellationTokenSource cleanupTokenSource = new();

        private class Session
        {
            public ulong SessionId { get; }
            public Dictionary<string, string> Values { get; }
            public DateTime LastAccessed { get; private set; }

            public Session(ulong sessionId)
            {
                SessionId = sessionId;
                Values = new Dictionary<string, string>();
                Touch();
            }

            public void Touch()
            {
                LastAccessed = DateTime.UtcNow;
            }
        }


        private Dictionary<ulong, Session> sessions;    // sessionId --> Session instance
        private ulong nextSessionId;                    // next value to use for the next new session

        public SessionTable()
        {
            sessions = new Dictionary<ulong, Session>();
            nextSessionId = 1;
            Task.Run(SessionCleanupLoop); // Fire and forget
        }

        /// <summary>
        /// Periodically scans and removes expired sessions based on a timeout threshold.
        /// </summary>
        private async Task SessionCleanupLoop()
        {
            var token = cleanupTokenSource.Token;
            while (!token.IsCancellationRequested)
            {
                await Task.Delay(cleanupInterval, token);

                lock (_lock)
                {
                    var now = DateTime.UtcNow;
                    var expired = new List<ulong>();
                    foreach (var pair in sessions)
                    {
                        if (now - pair.Value.LastAccessed > sessionTimeout)
                            expired.Add(pair.Key);
                    }

                    foreach (var id in expired)
                        sessions.Remove(id);
                }
            }
        }


        /// <summary>
        /// allocate and return a new session to the caller
        /// this method should be thread-safe
        /// </summary>
        public async Task<ulong> OpenSessionAsync()
        {
            await Task.Yield(); // Simulate async
            lock (_lock)
            {
                ulong sessionId = nextSessionId++;
                sessions[sessionId] = new Session(sessionId);
                return sessionId;
            }
        }



        /// <summary>
        /// returns true only if sessionID is a valid and open sesssion, false otherwise
        /// </summary>
        public async Task<bool> ResumeSessionAsync(ulong sessionId)
        {
            await Task.Yield();
            lock (_lock)
            {
                return sessions.ContainsKey(sessionId);
            }
        }


        /// <summary>
        ///  closes the session, will no longer be open and cannot be reused
        /// </summary>
        public async Task CloseSessionAsync(ulong sessionId)
        {
            await Task.Yield();
            lock (_lock)
            {
                if (!sessions.Remove(sessionId))
                    throw new SessionException("Session not found or already closed.");
            }
        }
        /// <summary>
        /// retrieves a session value, given session ID and key
        /// returns the value if it exists, or null if it does not
        /// </summary>
        public async Task<string?> GetSessionValueAsync(ulong sessionId, string key)
        {
            await Task.Yield();
            lock (_lock)
            {
                if (!sessions.TryGetValue(sessionId, out var session))
                    throw new SessionException("Session not found or already closed.");

                session.Touch();
                session.Values.TryGetValue(key, out var value);
                return value;
            }
        }


        /// <summary>
        /// stores a session value by session ID and key, replaces value if it already exists
        /// throws a session exception if the session is not open
        /// </summary>
        public async Task PutSessionValueAsync(ulong sessionId, string key, string value)
        {
            await Task.Yield();
            lock (_lock)
            {
                if (!sessions.TryGetValue(sessionId, out var session))
                    throw new SessionException("Session not found or already closed.");

                session.Values[key] = value;
                session.Touch();
            }
        }


        /// <summary>
        /// When disposing of the server, stop the cleanup loop
        /// </summary>
        public void Dispose()
        {
            cleanupTokenSource.Cancel();
        }

    }
}
