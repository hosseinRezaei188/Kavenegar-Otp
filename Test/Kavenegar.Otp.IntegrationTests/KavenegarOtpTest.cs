using Xunit;
using NSubstitute;
using Microsoft.Extensions.Options;
using System;
using System.Threading.Tasks;
using Kavenegar.Core.Exceptions;
using Kavenegar.Otp.Exceptions;
using Kavenegar.Otp.infrastructure;
using Kavenegar.Otp;
using NSubstitute.ExceptionExtensions;

public class KavenegarOtpTests
{
    private readonly ISmsSender _smsSender;
    private readonly IOptions<Config> _config;
    private readonly IAttemptTracker _attemptTracker;
    private readonly KavenegarOtp _kavenegarOtp;

    public KavenegarOtpTests()
    {
        _smsSender = Substitute.For<ISmsSender>();
        _config = Substitute.For<IOptions<Config>>();
        _config.Value.Returns(new Config
        {
            AppSecret = "otpSecretKey",
            OtpLifeTime = 5,
            OtpCodeLength = 6,
            KavenegarApiKey = "test_api_key",
            KavenegarTemplateName = "test_template"
        });

        _attemptTracker = Substitute.For<IAttemptTracker>();
        _kavenegarOtp = new KavenegarOtp(_attemptTracker, _smsSender, _config);
    }

    [Fact]
    public async Task SendOtp_WhenNotLocked_ShouldSendOtp()
    {
        // Arrange
        string phoneNumber = "1234567890";
        string ipAddress = "127.0.0.1";
        string key = $"Send_{phoneNumber}:{ipAddress}";

        _attemptTracker.IsLocked(key).Returns(false);

        // Act
        await _kavenegarOtp.SendOtp(phoneNumber, ipAddress);

        // Assert
        await _smsSender.Received(1).SendOtpSms(phoneNumber, Arg.Any<string>());
        _attemptTracker.Received(1).IncrementSendAttempts(key);
    }

    [Fact]
    public async Task SendOtp_WhenLocked_ShouldThrowUserLockedException()
    {
        // Arrange
        string phoneNumber = "1234567890";
        string ipAddress = "127.0.0.1";
        string key = $"Send_{phoneNumber}:{ipAddress}";
        TimeSpan lockoutTime = TimeSpan.FromMinutes(5);

        _attemptTracker.IsLocked(key).Returns(true);
        _attemptTracker.GetRemainingLockoutTime(key).Returns(lockoutTime);

        // Act & Assert
        await Assert.ThrowsAsync<UserLockedException>(() => _kavenegarOtp.SendOtp(phoneNumber, ipAddress));
    }

    [Fact]
    public async Task SendOtp_WhenApiExceptionOccurs_ShouldThrowSendOtpException()
    {
        // Arrange
        string phoneNumber = "1234567890";
        string ipAddress = "127.0.0.1";
        _attemptTracker.IsLocked(Arg.Any<string>()).Returns(false);
        _smsSender.SendOtpSms(Arg.Any<string>(), Arg.Any<string>())
            .ThrowsAsync(new ApiException("API Error", 400));
        // Act & Assert
        await Assert.ThrowsAsync<SendOtpException>(() => _kavenegarOtp.SendOtp(phoneNumber, ipAddress));
    }

    [Fact]
    public void VerifyOtp_WhenNotLocked_AndOtpInvalid_ShouldReturnFalse()
    {
        // Arrange
        string phoneNumber = "1234567890";
        string ipAddress = "127.0.0.1";
        string otp = "invalid";
        string key = $"Verify_{phoneNumber}:{ipAddress}";

        _attemptTracker.IsLocked(key).Returns(false);

        // Act
        bool result = _kavenegarOtp.VerifyOtp(phoneNumber, ipAddress, otp);

        // Assert
        Assert.False(result);
        _attemptTracker.Received(1).IncrementFailedAttempts(key);
    }

    [Fact]
    public void VerifyOtp_WhenLocked_ShouldThrowUserLockedException()
    {
        // Arrange
        string phoneNumber = "1234567890";
        string ipAddress = "127.0.0.1";
        string otp = "123456";
        string key = $"Verify_{phoneNumber}:{ipAddress}";
        TimeSpan lockoutTime = TimeSpan.FromMinutes(5);

        _attemptTracker.IsLocked(key).Returns(true);
        _attemptTracker.GetRemainingLockoutTime(key).Returns(lockoutTime);

        // Act & Assert
        Assert.Throws<UserLockedException>(() => _kavenegarOtp.VerifyOtp(phoneNumber, ipAddress, otp));
    }
}