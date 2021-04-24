using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using BeetleX.Buffers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BeetleX.Http.Clients
{
    public interface IBodyFormater
    {
        string ContentType { get; }

        void Serialization(Request request, object data, PipeStream stream);

        object Deserialization(Response response, BeetleX.Buffers.PipeStream stream, Type type, int length);

        void Setting(Request request);
    }

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Interface | AttributeTargets.Class)]
    public abstract class FormaterAttribute : Attribute, IBodyFormater
    {
        public abstract string ContentType { get; }

        public abstract void Serialization(Request request, object data, PipeStream stream);

        public abstract object Deserialization(Response response, BeetleX.Buffers.PipeStream stream, Type type, int length);

        public virtual void Setting(Request request) { }
    }

    public class FormUrlFormater : FormaterAttribute
    {
        public override string ContentType => "application/x-www-form-urlencoded";

        public override object Deserialization(Response response, PipeStream stream, Type type, int length)
        {
            return stream.ReadString(length);
        }
        public override void Serialization(Request request, object data, PipeStream stream)
        {
            if (data != null)
            {
                System.Collections.IDictionary keyValuePairs = data as IDictionary;
                if (keyValuePairs != null)
                {
                    int i = 0;
                    foreach (object key in keyValuePairs.Keys)
                    {
                        object value = keyValuePairs[key];
                        if (value != null)
                        {
                            if (i > 0)
                                stream.Write("&");
                            stream.Write(key.ToString() + "=");
                            if (value is string)
                            {
                                stream.Write(System.Net.WebUtility.UrlEncode((string)value));
                            }
                            else
                            {
                                if (value is IEnumerable subitems)
                                {
                                    List<string> values = new List<string>();
                                    foreach (var v in subitems)
                                        values.Add(v.ToString());
                                    stream.Write(string.Join(",", values));
                                }
                                else
                                {
                                    stream.Write(System.Net.WebUtility.UrlEncode(value.ToString()));
                                }
                            }
                            i++;
                        }
                    }
                }
                else
                {
                    stream.Write(data.ToString());
                }
            }
        }
    }

    public class JsonFormater : FormaterAttribute
    {
        public override string ContentType => "application/json";

        public override object Deserialization(Response response, PipeStream stream, Type type, int length)
        {
            using (stream.LockFree())
            {
                if (type == null)
                {
                    using (System.IO.StreamReader streamReader = new System.IO.StreamReader(stream))
                    using (JsonTextReader reader = new JsonTextReader(streamReader))
                    {
                        JsonSerializer jsonSerializer = JsonSerializer.CreateDefault();
                        object token = jsonSerializer.Deserialize(reader);
                        return token;
                    }
                }
                else
                {
                    using (StreamReader streamReader = new StreamReader(stream))
                    {
                        JsonSerializer serializer = JsonSerializer.CreateDefault();
                        object result = serializer.Deserialize(streamReader, type);
                        return result;
                    }
                }
            }
        }

        public override void Serialization(Request request, object data, PipeStream stream)
        {

            if (data != null)
            {
                if (data is string text)
                {
                    stream.Write(text);
                }
                else if (data is StringBuilder sb)
                {
                    stream.Write(sb);
                }
                else
                {
                    using (stream.LockFree())
                    {
                        using (StreamWriter writer = new StreamWriter(stream))
                        {
                            IDictionary dictionary = data as IDictionary;
                            JsonSerializer serializer = new JsonSerializer();

                            if (dictionary != null && dictionary.Count == 1)
                            {
                                object[] vlaues = new object[dictionary.Count];
                                dictionary.Values.CopyTo(vlaues, 0);
                                serializer.Serialize(writer, vlaues[0]);
                            }
                            else
                            {
                                serializer.Serialize(writer, data);
                            }

                        }
                    }
                }
            }
        }
    }

    public class FromDataFormater : FormaterAttribute
    {
        public override string ContentType => "multipart/form-data";

        public override void Setting(Request request)
        {
            request.Boundary = "----Beetlex.io" + DateTime.Now.ToString("yyyyMMddHHmmss");
            var value = request.Header["Content-Type"];
            value += "; boundary=" + request.Boundary;
            request.Header["Content-Type"] = value;
            base.Setting(request);

        }

        public override object Deserialization(Response response, PipeStream stream, Type type, int length)
        {
            return stream.ReadString(length);
        }

        public override void Serialization(Request request, object data, PipeStream stream)
        {
            if (data == null)
                return;
            if (data is IDictionary<string, object> dictionary)
            {
                foreach (var item in dictionary)
                {
                    stream.Write("--");
                    stream.WriteLine(request.Boundary);
                    if (item.Value is UploadFile uploadFile)
                    {
                        stream.WriteLine($"Content-Disposition: form-data; name=\"{item.Key}\"; filename=\"{uploadFile.Name}\"");
                        stream.WriteLine($"Content-Type: binary");
                        stream.WriteLine("");
                        stream.Write(uploadFile.Data.Array, uploadFile.Data.Offset, uploadFile.Data.Count);
                        stream.WriteLine("");
                    }
                    else if (item.Value is FileInfo file)
                    {
                        stream.WriteLine($"Content-Disposition: form-data; name=\"{item.Key}\"; filename=\"{file.Name}\"");
                        stream.WriteLine($"Content-Type: binary");
                        stream.WriteLine("");
                        using (System.IO.Stream open = file.OpenRead())
                        {
                            open.CopyTo(stream);
                        }
                        stream.WriteLine("");
                    }
                    else
                    {
                        stream.WriteLine($"Content-Disposition: form-data; name=\"{item.Key}\"");
                        stream.WriteLine("");
                        if (item.Value is IEnumerable subitems)
                        {
                            List<string> values = new List<string>();
                            foreach (var v in subitems)
                                values.Add(v.ToString());
                            stream.Write(string.Join(",", values));
                        }
                        else
                        {
                            stream.Write(item.Value.ToString());
                        }
                        stream.WriteLine("");
                    }
                }
                if (dictionary.Count > 0)
                {
                    stream.Write("--");
                    stream.Write(request.Boundary);
                    stream.WriteLine("--");
                }
            }
            else
            {
                throw new HttpClientException($"post data must be IDictionary<string, object>!");
            }
        }
    }

    public class BinaryFormater : FormaterAttribute
    {
        public override string ContentType => "application/octet-stream";

        public override object Deserialization(Response response, PipeStream stream, Type type, int length)
        {
            var result = System.Buffers.ArrayPool<byte>.Shared.Rent(length);
            stream.Read(result, 0, length);
            return new ArraySegment<Byte>(result, 0, length);
        }

        public override void Serialization(Request request, object data, PipeStream stream)
        {
            if (data is Byte[] buffer)
            {
                stream.Write(buffer, 0, buffer.Length);
            }
            else if (data is ArraySegment<byte> array)
            {
                stream.Write(array.Array, array.Offset, array.Count);
            }
            else
            {
                throw new Exception("Commit data must be byte[] or ArraySegment<byte>");
            }
        }
    }


}
