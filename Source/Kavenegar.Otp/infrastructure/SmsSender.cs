using Kavenegar.Core.Models;
using Microsoft.Extensions.Options;
using System.Threading.Tasks;

namespace Kavenegar.Otp.infrastructure
{
    internal class SmsSender: ISmsSender
    {
        private readonly KavenegarApi _api;
        private readonly string _templateName;

        public SmsSender(IOptions<Config> config)
        {
            _api = new KavenegarApi(config.Value.KavenegarApiKey);
            _templateName = config.Value.KavenegarTemplateName;
        }

        public async Task<SendResult> SendOtpSms(string recipient, string Otp)
        {
            return await _api.VerifyLookup(recipient, Otp, _templateName);
        }
    }

    internal interface ISmsSender
    {
        Task<SendResult> SendOtpSms(string recipient, string Otp);
    }
}