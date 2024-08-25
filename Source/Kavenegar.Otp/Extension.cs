using Kavenegar.Otp.infrastructure;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Kavenegar.Otp
{
    public static class KavengearOtpExtension
    {
        public static IServiceCollection AddKavengearOtp(this IServiceCollection services, Action<Config> configure)
        {
            services.Configure(configure);
            services.AddTransient<IKavenegarOtp, KavenegarOtp>();
            services.AddTransient<ISmsSender, SmsSender>();
            services.AddSingleton<IAttemptTracker, AttemptTracker>();
            return services;
        }

    }
}
