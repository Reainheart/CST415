// SessionTable.cs
//
// Pete Myers
// CST 415
// Fall 2019
// 
// Noah Etchemendy
// CST 415
// Spring 2025


using System;
using System.Collections.Generic;
using System.Threading;

namespace SDServer
{
    public class SessionException : Exception
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
    public class SessionTable
    {
        private object _lock = new object();
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
        public SessionTable(TimeSpan SessionTimeout, TimeSpan CleanupInterval)
        {
            sessionTimeout= SessionTimeout;
            cleanupInterval = CleanupInterval;
            sessions = new Dictionary<ulong, Session>();
            nextSessionId = 1;
            Task.Run(SessionCleanupLoop); // Fire and forget
        }
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
        public ulong OpenSession()
        {
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
        public bool ResumeSession(ulong sessionId)
        {
            bool sessionExists = false;
            lock (_lock)
            {
                sessionExists = sessions.ContainsKey(sessionId);
            }
            return sessionExists;
        }


        /// <summary>
        ///  closes the session, will no longer be open and cannot be reused
        /// </summary>
        public void CloseSession(ulong sessionId)
        {
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
        public string? GetSessionValue(ulong sessionId, string key)
        {
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
        public void PutSessionValue(ulong sessionId, string key, string value)
        {
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
            cleanupTokenSource.Dispose(); // clean up resources
        }

    }
}
