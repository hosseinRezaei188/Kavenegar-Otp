using Kavenegar.Core.Exceptions;
using System;

namespace Kavenegar.Otp.Exceptions
{
    public class SendOtpException : ApiException
    {
        public SendOtpException(string message, int code) : base(message, code)
        {

        }
    }
}
