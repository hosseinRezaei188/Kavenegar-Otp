using System;

namespace Kavenegar.Otp
{
    public class Config
    {
        public string? AppSecret { get; set; }
        public int OtpLifeTime { get; set; }
        public int OtpCodeLength { get; set; }
        public string? KavenegarTemplateName { get; set; }
        public string? KavenegarApiKey { get; set; }
        public int MaxAttempts { get; set; }
        public TimeSpan LockoutDuration { get; set; }
    }
}
