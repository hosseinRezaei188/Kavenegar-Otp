using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;

namespace Kavenegar.Otp.infrastructure
{
    internal class AttemptTracker : IAttemptTracker
    {
        private readonly ConcurrentDictionary<string, UserAttempts> _attempts;
        private readonly int _maxAttempts;
        private readonly TimeSpan _lockoutDuration;

        public AttemptTracker(IOptions<Config> config)
        {
            _attempts = new ConcurrentDictionary<string, UserAttempts>();
            _maxAttempts = config.Value.MaxAttempts;
            _lockoutDuration = config.Value.LockoutDuration;
        }

        public bool IsLocked(string key)
        {
            if (_attempts.TryGetValue(key, out UserAttempts attempts))
            {
                if (attempts.FailedAttempts >= _maxAttempts &&
                    DateTime.UtcNow - attempts.LastAttemptTime < _lockoutDuration)
                {
                    return true;
                }
                else if (DateTime.UtcNow - attempts.LastAttemptTime >= _lockoutDuration)
                {
                    ResetAttempts(key);
                }
            }
            return false;
        }

        public void IncrementFailedAttempts(string key)
        {
            _attempts.AddOrUpdate(
                key,
                new UserAttempts { FailedAttempts = 1, LastAttemptTime = DateTime.UtcNow },
                (_, oldValue) => new UserAttempts
                {
                    FailedAttempts = oldValue.FailedAttempts + 1,
                    LastAttemptTime = DateTime.UtcNow
                }
            );
        }

        public void IncrementSendAttempts(string key)
        {
            _attempts.AddOrUpdate(
                key,
                new UserAttempts { FailedAttempts = 1, LastAttemptTime = DateTime.UtcNow },
                (_, oldValue) => new UserAttempts
                {
                    FailedAttempts = oldValue.FailedAttempts + 1,
                    LastAttemptTime = DateTime.UtcNow
                }
            );
        }

        public void ResetAttempts(string key)
        {
            _attempts.TryRemove(key, out _);
        }

        public TimeSpan GetRemainingLockoutTime(string key)
        {
            if (_attempts.TryGetValue(key, out UserAttempts attempts))
            {
                if (attempts.FailedAttempts >= _maxAttempts)
                {
                    TimeSpan elapsedTime = DateTime.UtcNow - attempts.LastAttemptTime;
                    if (elapsedTime < _lockoutDuration)
                    {
                        return _lockoutDuration - elapsedTime;
                    }
                }
            }
            return TimeSpan.Zero;
        }

        public class UserAttempts
        {
            public int FailedAttempts { get; set; }
            public DateTime LastAttemptTime { get; set; }
        }
    }


    internal interface IAttemptTracker
    {
        TimeSpan GetRemainingLockoutTime(string key);
        void IncrementFailedAttempts(string key);
        void IncrementSendAttempts(string key);
        bool IsLocked(string key);
        void ResetAttempts(string key);
    }
}