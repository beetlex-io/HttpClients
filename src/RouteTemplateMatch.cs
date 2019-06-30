using System;
using System.Collections.Generic;
using System.Text;

namespace BeetleX.Http.Clients
{
    class RouteTemplateMatch
    {
        public RouteTemplateMatch(string value, int offset = 0)
        {
            mTemplate = value;
            mOffset = offset;
            Init();
        }

        private int mOffset;

        private List<MatchItem> mItems = new List<MatchItem>();

        public List<MatchItem> Items => mItems;

        private string mTemplate;

        public string Template => mTemplate;

        private void Init()
        {
            MatchItem item = new MatchItem();
            bool first = true;
            bool inmatch = false;
            int offset = 0;
            for (int i = mOffset; i < mTemplate.Length; i++)
            {
                if (mTemplate[i] == '{')
                {
                    if (!first)
                        item = new MatchItem();
                    inmatch = true;
                    offset = i;
                }
                else if (mTemplate[i] == '}')
                {
                    if (first)
                    {
                        first = false;
                    }
                    item.Name = mTemplate.Substring(offset + 1, i - offset - 1);
                    mItems.Add(item);
                    inmatch = false;
                }
                else
                {
                    if (mItems.Count > 0 && !inmatch)
                        item.Eof += mTemplate[i];
                    if (mItems.Count == 0 && !inmatch)
                        item.Start += mTemplate[i];
                }
            }
        }

        public class MatchItem
        {
            public string Start;

            public string Name;

            public string Eof;

            public int Match(string url, int offset, out string value)
            {
                int count = 0;
                value = "";
                int length = url.Length;
                if (Start != null)
                {
                    for (int k = 0; k < Start.Length; k++)
                    {
                        if (offset + k < length)
                        {
                            if (Start[k] != url[offset + k])
                                return -1;
                        }
                        else
                            return -1;
                    }
                    offset = offset + Start.Length;
                    count += Start.Length;
                }
                if (Eof != null)
                {
                    for (int i = offset; i < length; i++)
                    {
                        if (Eof != null && url[i] == Eof[0])
                        {
                            bool submatch = true;
                            for (int k = 1; k < Eof.Length; k++)
                            {
                                if (url[i + k] != Eof[k])
                                {
                                    submatch = false;
                                    break;
                                }
                            }
                            if (submatch)
                            {
                                value = url.Substring(offset, i - offset);
                                count += Eof.Length;
                                break;
                            }
                        }

                        else
                        {
                            count++;
                        }
                    }
                }
                else
                {
                    count = url.Length - offset;
                    value = url.Substring(offset, count);
                }
                if (value == "")
                    return -1;
                return count;
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            foreach (var item in mItems)
            {
                sb.Append(item.Start).Append(item.Name).Append(item.Eof);
            }
            return sb.ToString();
        }
    }
}
