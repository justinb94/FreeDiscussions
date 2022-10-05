﻿using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Usenet.Nntp.Builders;
using Usenet.Nntp.Models;
using Usenet.Nntp.Responses;
using Usenet.Util;

namespace Usenet.Nntp.Parsers
{
    [Flags]
    internal enum ArticleRequestType
    {
        Head = 0x01,
        Body = 0x02,
        Article = 0x03
    }

    internal class ArticleResponseParser : IMultiLineResponseParser<NntpArticleResponse>
    {
        private readonly ILogger log = Logger.Create<ArticleResponseParser>();
        private readonly ArticleRequestType requestType;
        private readonly int successCode;

        public ArticleResponseParser(ArticleRequestType requestType)
        {
            switch (this.requestType = requestType)
            {
                case ArticleRequestType.Head:
                    successCode = 221;
                    break;

                case ArticleRequestType.Body:
                    successCode = 222;
                    break;

                case ArticleRequestType.Article:
                    successCode = 220;
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(requestType), requestType, null);
            }
        }

        public bool IsSuccessResponse(int code) => code == successCode;

        public NntpArticleResponse Parse(int code, string message, IEnumerable<string> dataBlock)
        {
            if (!IsSuccessResponse(code))
            {
                return new NntpArticleResponse(code, message, false, null);
            }

            // get response line
            string[] responseSplit = message.Split(' ');
            if (responseSplit.Length < 2)
            {
                log.LogError("Invalid response message: {Message} Expected: {{number}} {{messageid}}", message);
            }

            long.TryParse(responseSplit.Length > 0 ? responseSplit[0] : null, out long number);
            string messageId = responseSplit.Length > 1 ? responseSplit[1] : string.Empty;

            if (dataBlock == null)
            {
                // no headers and no body
                return new NntpArticleResponse(code, message, true, new NntpArticle(number, messageId, null, null, null));
            }

            using (IEnumerator<string> enumerator = dataBlock.GetEnumerator())
            {
                // get headers if requested
                MultiValueDictionary<string, string> headers = (requestType & ArticleRequestType.Head) == ArticleRequestType.Head
                    ? GetHeaders(enumerator)
                    : MultiValueDictionary<string, string>.Empty;

                // get groups
                NntpGroups groups = headers.TryGetValue(NntpHeaders.Newsgroups, out ICollection<string> values)
                    ? new NntpGroupsBuilder().Add(values).Build()
                    : null;

                // get body if requested
                IEnumerable<string> bodyLines = (requestType & ArticleRequestType.Body) == ArticleRequestType.Body
                    ? EnumerateBodyLines(enumerator)
                    : new string[0];

                if (dataBlock is ICollection<string>)
                {
                    // no need to keep enumerator if input is not a stream
                    // memoize the body lines
                    bodyLines = bodyLines.ToList();
                }

                return new NntpArticleResponse(
                    code, message, true,
                    new NntpArticle(number, messageId, groups, headers, bodyLines));
            }
        }

        private MultiValueDictionary<string, string> GetHeaders(IEnumerator<string> enumerator)
        {
            var headers = new List<Header>();
            Header prevHeader = null;
            while (enumerator.MoveNext())
            {
                string line = enumerator.Current;
                if (string.IsNullOrEmpty(line))
                {
                    // no more headers (skip empty line)
                    break;
                }
                if (char.IsWhiteSpace(line[0]) && prevHeader != null)
                {
                    prevHeader.Value += " " + line.Trim();
                }
                else
                {
                    int splitPos = line.IndexOf(':');
                    if (splitPos < 0)
                    {
                        log.LogError("Invalid header line: {Line} Expected: {{key}}:{{value}}", line);
                    }
                    else
                    {

                        prevHeader = new Header(line.Substring(0, splitPos), line.Substring(splitPos + 1).Trim());
                        headers.Add(prevHeader);
                    }
                }
            }

            var dict = new MultiValueDictionary<string, string>();
            foreach (Header header in headers)
            {
                dict.Add(header.Key, header.Value);
            }
            return dict;
        }

        private static IEnumerable<string> EnumerateBodyLines(IEnumerator<string> enumerator)
        {
            if (enumerator == null)
            {
                yield break;
            }
            while (enumerator.MoveNext())
            {
                yield return enumerator.Current;
            }
        }

        private class Header
        {
            public string Key { get; }
            public string Value { get; set; }

            public Header(string key, string value)
            {
                Key = key;
                Value = value;
            }
        }
    }
}
