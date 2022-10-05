﻿using System;
using System.Globalization;
using Microsoft.Extensions.Logging;
using Usenet.Nntp.Responses;

namespace Usenet.Nntp.Parsers
{
    internal class DateResponseParser : IResponseParser<NntpDateResponse>
    {
        private readonly ILogger log = Logger.Create<DateResponseParser>();

        public bool IsSuccessResponse(int code) => code == 111;

        public NntpDateResponse Parse(int code, string message)
        {
            string[] responseSplit = message.Split(' ');

            if (IsSuccessResponse(code) && responseSplit.Length >= 1 && DateTimeOffset.TryParseExact(responseSplit[0], "yyyyMMddHHmmss",
                CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out DateTimeOffset dateTime))
            {
                return new NntpDateResponse(code, message, true, dateTime);
            }
            
            log.LogError("Invalid response message: {Message} Expected: {{yyyymmddhhmmss}}", message);
            return new NntpDateResponse(code, message, false, DateTimeOffset.MinValue);
        }
    }
}
