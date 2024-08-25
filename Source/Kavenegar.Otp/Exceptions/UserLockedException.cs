using System;

namespace Kavenegar.Otp.Exceptions
{
    public class UserLockedException : Exception
    {
        public TimeSpan RemainingLockTime { get; }

        public UserLockedException(TimeSpan remainingLockTime)
        {
            RemainingLockTime = remainingLockTime;
        }
    }
}
