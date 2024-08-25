using Microsoft.Extensions.Options;
using OtpNet;
using System;
using System.Collections.Generic;
using System.Text;
namespace Kavenegar.Otp.infrastructure
{
    internal class OtpGenerator
    {
        private readonly Totp _totp;
        public OtpGenerator(Config config, string mobile)
        {
            byte[] secretKeyBytes = Encoding.UTF8.GetBytes(string.Format("{0}{1}", config.AppSecret, mobile));
            _totp = new Totp(secretKeyBytes, config.OtpLifeTime, OtpHashMode.Sha256, config.OtpCodeLength);
        }

        public string GenerateOtp()
        {
            return _totp.ComputeTotp();
        }

        public bool VerifyOtp(string otp)
        {
            return _totp.VerifyTotp(otp, out _, VerificationWindow.RfcSpecifiedNetworkDelay);
        }
    }

    internal interface IOtpGenerator
    {
        string GenerateOtp();
        bool VerifyOtp(string otp);
    }
}
