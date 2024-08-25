using Kavenegar.Core.Exceptions;
using Kavenegar.Otp.Exceptions;
using Kavenegar.Otp.infrastructure;
using Microsoft.Extensions.Options;
using System;
using System.Threading.Tasks;

namespace Kavenegar.Otp
{
    internal class KavenegarOtp : IKavenegarOtp
    {
        private readonly ISmsSender _smsSender;
        private readonly IOptions<Config> _config;
        private readonly IAttemptTracker _attemptTracker;

        public KavenegarOtp(IAttemptTracker attemptTracker, ISmsSender smsSender, IOptions<Config> config)
        {
            _smsSender = smsSender;
            _config = config;
            _attemptTracker = attemptTracker;
        }

        public async Task SendOtp(string phoneNumber, string ipAddress)
        {
            try
            {
                string key = GetSendAttemptKey(phoneNumber, ipAddress);
                if (_attemptTracker.IsLocked(key))
                {
                    TimeSpan remainingLockTime = _attemptTracker.GetRemainingLockoutTime(key);
                    throw new UserLockedException(remainingLockTime);
                }

                var otp = new OtpGenerator(_config.Value, phoneNumber).GenerateOtp();
                await _smsSender.SendOtpSms(phoneNumber, otp);
                _attemptTracker.IncrementSendAttempts(key);
            }
            catch (ApiException ex)
            {
                throw new SendOtpException(ex.Message, (int)ex.Code);
            }
            catch (Exception)
            {
                throw;
            }

        }

        public bool VerifyOtp(string phoneNumber, string ipAddress, string otp)
        {
            string key = GetVerifyAttemptKey(phoneNumber, ipAddress);
            if (_attemptTracker.IsLocked(key))
            {
                TimeSpan remainingLockTime = _attemptTracker.GetRemainingLockoutTime(key);
                throw new UserLockedException(remainingLockTime);
            }

            bool isValid = new OtpGenerator(_config.Value, phoneNumber).VerifyOtp(otp);

            if (!isValid)
            {
                _attemptTracker.IncrementFailedAttempts(key);
            }
            else
            {
                _attemptTracker.ResetAttempts(key);
            }

            return isValid;
        }

        private string GetSendAttemptKey(string phoneNumber, string ipAddress)
        {
            return $"Send_{phoneNumber}:{ipAddress}";
        }
        private string GetVerifyAttemptKey(string phoneNumber, string ipAddress)
        {
            return $"Verify_{phoneNumber}:{ipAddress}";
        }
    }

    public interface IKavenegarOtp
    {
        Task SendOtp(string phoneNumber, string ipAddress);
        bool VerifyOtp(string phoneNumber, string ipAddress, string otp);
    }
}
