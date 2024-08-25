using System;
using System.Collections.Concurrent;
using Kavenegar.Otp.infrastructure;
using Kavenegar.Otp;
using Microsoft.Extensions.Options;
using Xunit;

public class AttemptTrackerTests
{
    private readonly AttemptTracker _attemptTracker;
    private readonly IOptions<Config> _config;

    public AttemptTrackerTests()
    {
        _config = Options.Create(new Config
        {
            MaxAttempts = 3,
            LockoutDuration = TimeSpan.FromMinutes(1)
        });

        _attemptTracker = new AttemptTracker(_config);
    }

    [Fact]
    public void IsLocked_UserNotLocked_ReturnsFalse()
    {
        // Arrange
        string key = "user1";

        // Act
        bool isLocked = _attemptTracker.IsLocked(key);

        // Assert
        Assert.False(isLocked);
    }

    [Fact]
    public void IsLocked_UserExceedsMaxAttemptsAndIsLocked_ReturnsTrue()
    {
        // Arrange
        string key = "user1";
        for (int i = 0; i < _config.Value.MaxAttempts; i++)
        {
            _attemptTracker.IncrementFailedAttempts(key);
        }

        // Act
        bool isLocked = _attemptTracker.IsLocked(key);

        // Assert
        Assert.True(isLocked);
    }

    [Fact]
    public void IsLocked_UserExceedsMaxAttemptsAndLockoutExpires_ReturnsFalse()
    {
        // Arrange
        string key = "user1";
        for (int i = 0; i < _config.Value.MaxAttempts; i++)
        {
            _attemptTracker.IncrementFailedAttempts(key);
        }

        // Simulate waiting past the lockout duration
        System.Threading.Thread.Sleep(60000);

        // Act
        bool isLocked = _attemptTracker.IsLocked(key);

        // Assert
        Assert.False(isLocked);
    }

    [Fact]
    public void IncrementFailedAttempts_IncrementsAttemptsCorrectly()
    {
        // Arrange
        string key = "user1";
        _attemptTracker.IncrementFailedAttempts(key);

        // Act
        var userAttempts = GetUserAttempts(key);

        // Assert
        Assert.NotNull(userAttempts);
        Assert.Equal(1, userAttempts.FailedAttempts);
    }

    [Fact]
    public void IncrementSendAttempts_IncrementsSendAttemptsCorrectly()
    {
        // Arrange
        string key = "user1";
        _attemptTracker.IncrementSendAttempts(key);

        // Act
        var userAttempts = GetUserAttempts(key);

        // Assert
        Assert.NotNull(userAttempts);
        Assert.Equal(1, userAttempts.FailedAttempts);
    }

    [Fact]
    public void ResetAttempts_RemovesUserAttempts()
    {
        // Arrange
        string key = "user1";
        _attemptTracker.IncrementFailedAttempts(key);
        _attemptTracker.ResetAttempts(key);

        // Act
        var userAttempts = GetUserAttempts(key);

        // Assert
        Assert.Null(userAttempts);
    }

    [Fact]
    public void GetRemainingLockoutTime_UserLocked_ReturnsCorrectTimeSpan()
    {
        // Arrange
        string key = "user1";
        for (int i = 0; i < _config.Value.MaxAttempts; i++)
        {
            _attemptTracker.IncrementFailedAttempts(key);
        }

        // Simulate waiting for 10 seconds (not enough to expire the lockout)
        System.Threading.Thread.Sleep(10000);

        // Act
        TimeSpan remainingLockoutTime = _attemptTracker.GetRemainingLockoutTime(key);

        // Assert
        Assert.True(remainingLockoutTime > TimeSpan.Zero);
        Assert.True(remainingLockoutTime < _config.Value.LockoutDuration);
    }

    private AttemptTracker.UserAttempts GetUserAttempts(string key)
    {
        // Use reflection to access the private _attempts field
        var field = typeof(AttemptTracker).GetField("_attempts",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var attempts = field.GetValue(_attemptTracker) as ConcurrentDictionary<string, AttemptTracker.UserAttempts>;

        attempts.TryGetValue(key, out var userAttempts);
        return userAttempts;
    }
}
