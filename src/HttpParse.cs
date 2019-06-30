using BeetleX.Buffers;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace BeetleX.Http.Clients
{
    public class HttpParse
    {
        public const string GET_TAG = "GET";

        public const string POST_TAG = "POST";

        public const string DELETE_TAG = "DELETE";

        public const string PUT_TAG = "PUT";

        public const string OPTIONS_TAG = "OPTIONS";

        [ThreadStatic]
        private static char[] mCharCacheBuffer;

        public static char[] GetCharBuffer()
        {
            if (mCharCacheBuffer == null)
                mCharCacheBuffer = new char[1024 * 4];
            return mCharCacheBuffer;
        }

        [ThreadStatic]
        private static byte[] mByteBuffer;
        public static byte[] GetByteBuffer()
        {
            if (mByteBuffer == null)
                mByteBuffer = new byte[1024 * 4];
            return mByteBuffer;
        }
        [ThreadStatic]
        private static char[] mToLowerBuffer;
        public static char[] GetToLowerBuffer()
        {
            if (mToLowerBuffer == null)
            {
                mToLowerBuffer = new char[1024];
            }
            return mToLowerBuffer;
        }

        public static ReadOnlySpan<char> ReadCharLine(IndexOfResult result)
        {
            int offset = 0;
            char[] data = HttpParse.GetCharBuffer();
            IMemoryBlock memory = result.Start;
            for (int i = result.StartPostion; i < memory.Length; i++)
            {
                data[offset] = (char)result.Start.Data[i];
                offset++;
                if (offset == result.Length)
                    break;
            }
            if (offset < result.Length)
            {

            Next:
                memory = result.Start.NextMemory;
                int count;
                if (memory.ID == result.End.ID)
                {
                    count = result.EndPostion + 1;
                }
                else
                {
                    count = memory.Length;
                }
                for (int i = 0; i < count; i++)
                {
                    data[offset] = (char)memory.Data[i];
                    offset++;
                    if (offset == result.Length)
                        break;
                }
                if (offset < result.Length)
                    goto Next;
            }
            return new ReadOnlySpan<char>(data, 0, result.Length - 2);

        }

        public static string CharToLower(ReadOnlySpan<char> url)
        {
            char[] buffer = GetToLowerBuffer();
            for (int i = 0; i < url.Length; i++)
                buffer[i] = Char.ToLower(url[i]);
            return new string(buffer, 0, url.Length);
        }

        public static unsafe string GetBaseUrl(ReadOnlySpan<char> url)
        {
            fixed (char* purl = url)
            {
                for (int i = 0; i < url.Length; i++)
                {
                    if (url[i] == '?')
                    {
                        return new string(purl, 0, i);
                    }
                }
                return new string(purl, 0, url.Length);
            }
        }

        public static string GetBaseUrlToLower(ReadOnlySpan<char> url)
        {
            for (int i = 0; i < url.Length; i++)
            {
                if (url[i] == '?')
                {
                    return CharToLower(url.Slice(0, i));
                }
            }
            return CharToLower(url);
        }

        public static string MD5Encrypt(string filename)
        {
            using (MD5 md5Hash = MD5.Create())
            {
                byte[] hash = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(filename));

                return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            }
        }

        public static string GetBaseUrlExt(ReadOnlySpan<char> url)
        {
            int offset = 0;
            for (int i = 0; i < url.Length; i++)
            {
                if (url[i] == '.')
                {
                    offset = i + 1;
                }
            }
            if (offset > 0)
                return CharToLower(url.Slice(offset, url.Length - offset));
            return null;
        }

        public unsafe static void AnalyzeCookie(ReadOnlySpan<char> cookieData, Cookies cookies)
        {
            fixed (char* pData = cookieData)
            {
                int offset = 0;
                string name = null, value = null;
                for (int i = 0; i < cookieData.Length; i++)
                {
                    if (cookieData[i] == '=')
                    {
                        if (cookieData[offset] == ' ')
                            offset++;
                        name = new string(pData, offset, i - offset);
                        offset = i + 1;
                    }
                    if (name != null && cookieData[i] == ';')
                    {
                        value = new string(pData, offset, i - offset);
                        offset = i + 1;
                        cookies.Add(name, value);
                        name = null;
                    }
                }
                if (name != null)
                {
                    value = new string(pData, offset, cookieData.Length - offset);
                    cookies.Add(name, value);
                }
            }
        }


        private unsafe static ContentHeaderProperty[] GetProperties(ReadOnlySpan<char> line)
        {
            fixed (char* pline = line)
            {
                List<ContentHeaderProperty> proerties = new List<ContentHeaderProperty>();
                int offset = 0;
                string name = null;
                string value;
                for (int i = 0; i < line.Length; i++)
                {
                    if (line[i] == ' ')
                    {
                        offset++;
                        continue;
                    }
                    if (line[i] == '=')
                    {
                        name = new string(pline, offset, i - offset);
                        offset = i + 1;
                    }
                    else if (line[i] == ';')
                    {
                        if (!string.IsNullOrEmpty(name))
                        {
                            value = new string(pline, offset + 1, i - offset - 2);
                            proerties.Add(new ContentHeaderProperty() { Name = name, Value = value });
                            offset = i + 1;
                            name = null;
                        }
                    }
                }
                if (name != null)
                {
                    value = new string(pline, offset + 1, line.Length - offset - 2);
                    proerties.Add(new ContentHeaderProperty() { Name = name, Value = value });
                }
                return proerties.ToArray();
            }
        }

        public unsafe static ContentHeader AnalyzeContentHeader(ReadOnlySpan<char> line)
        {
            fixed (char* pline = line)
            {
                ContentHeader result = new ContentHeader();
                ReadOnlySpan<char> property = line;
                int offset = 0;
                for (int i = 0; i < line.Length; i++)
                {
                    if (line[i] == ':')
                    {
                        result.Name = new string(pline, 0, i);
                        offset = i + 1;
                    }
                    else if (offset > 0 && line[i] == ' ')
                        offset = i + 1;
                    else if (line[i] == ';')
                    {
                        result.Value = new string(pline, offset, i - offset);
                        property = line.Slice(i + 1);
                        offset = 0;
                        break;
                    }
                }
                if (offset > 0)
                {
                    result.Value = new string(pline, offset, line.Length - offset);
                }
                if (property.Length != line.Length)
                {
                    result.Properties = GetProperties(property);
                }
                return result;
            }
        }


        public unsafe static Tuple<string, string> AnalyzeHeader(ReadOnlySpan<byte> line)
        {
            Span<char> charbuffer = GetCharBuffer();
            fixed (byte* pline = line)
            {
                fixed (char* pchar = charbuffer)
                {
                    var len = Encoding.UTF8.GetChars(pline, line.Length, pchar, charbuffer.Length);
                    return AnalyzeHeader(charbuffer.Slice(0, len));
                }
            }
        }
        public unsafe static Tuple<string, string> AnalyzeHeader(ReadOnlySpan<char> line)
        {
            fixed (char* pline = line)
            {
                string name = null, value = null;
                int offset = 0;
                for (int i = 0; i < line.Length; i++)
                {
                    if (line[i] == ':' && name == null)
                    {
                        name = new string(pline, offset, i - offset);
                        offset = i + 1;
                    }
                    else
                    {
                        if (name != null)
                        {
                            if (line[i] == ' ')
                                offset++;
                            else
                                break;
                        }
                    }
                }
                value = new string(pline, offset, line.Length - offset);
                return new Tuple<string, string>(name, value);
            }
        }

        public unsafe static Tuple<string, int, string> AnalyzeResponseLine(ReadOnlySpan<byte> line)
        {
            Span<char> charbuffer = GetCharBuffer();
            fixed (byte* pline = line)
            {
                fixed (char* pchar = charbuffer)
                {
                    var len = Encoding.UTF8.GetChars(pline, line.Length, pchar, charbuffer.Length);
                    return AnalyzeResponseLine(charbuffer.Slice(0, len));
                }
            }
        }
        public unsafe static void AnalyzeResponseLine(ReadOnlySpan<char> line, Response response)
        {
            fixed (char* pline = line)
            {
                int offset = 0;
                int count = 0;
                for (int i = 0; i < line.Length; i++)
                {
                    if (line[i] == ' ')
                    {
                        if (count == 0)
                        {
                            response.HttpVersion = new string(pline,offset,i-offset);
                            offset = i + 1;
                        }
                        else
                        {
                            response.Code = new string(pline, offset, i - offset);
                            offset = i + 1;
                            response.CodeMsg = new string(pline, offset, line.Length - offset);
                            return;
                        }
                        count++;
                    }
                }
            }
        }
        public unsafe static Tuple<string, int, string> AnalyzeResponseLine(ReadOnlySpan<char> line)
        {
            fixed (char* pline = line)
            {
                string httpversion = null, codemsg = null;
                int code = 200;
                int offset = 0;
                int count = 0;
                for (int i = 0; i < line.Length; i++)
                {
                    if (line[i] == ' ')
                    {
                        if (count == 0)
                        {
                            httpversion = new string(pline, offset, i - offset);
                            offset = i + 1;
                        }
                        else
                        {
                            code = int.Parse(new string(pline, offset, i - offset));
                            offset = i + 1;
                            codemsg = new string(pline, offset, line.Length - offset);
                            break;
                        }
                        count++;
                    }
                }
                return new Tuple<string, int, string>(httpversion, code, codemsg);
            }
        }

   


        public struct ContentHeader
        {
            public string Name;

            public string Value;

            public ContentHeaderProperty[] Properties { get; set; }

        }

        public struct ContentHeaderProperty
        {
            public string Name;
            public string Value;
        }

    }
}
