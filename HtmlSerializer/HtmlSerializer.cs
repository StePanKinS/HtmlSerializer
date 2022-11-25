using System.Reflection;
using System.Text.Json;

namespace Html
{
    public static class HtmlSerializer
    {
        public static string[] singleTags = new string[]
        {
            "_text", "area", "base", "br", "col", "command", "embed", "hr", "img", "input", "keygen", "link", "meta", "param", "source", "track", "wbr"
        };

        public static Tag[] Deserialize(string filePath)
        {
            StreamReader htmlFile = new StreamReader(filePath);

            string[] lines = Array.Empty<string>();
            string? line;
            while ((line = htmlFile.ReadLine()) != null)
            {
                string[] newLines = new string[lines.Length + 1];

                for (int i = 0; i < lines.Length; i++)
                {
                    newLines[i] = lines[i];
                }
                newLines[newLines.Length - 1] = line;
                lines = newLines;
            }

            string html = cutSpaces(lines);
            textTag(ref html);

            string json = htmlToJson(html);

            Tag[]? ret = JsonSerializer.Deserialize<Tag[]>(json);
            if(ret != null)
            {
                return ret;
            }
            return Array.Empty<Tag>();
        }

        private static string cutSpaces(string[] lines)
        {
            for (int i = 0; i < lines.Length; i++)
            {
                lines[i] = lines[i].Trim();
            }

            while (lines.Length > 1)
            {
                bool isEmpty = true;
                foreach (char c in lines[0])
                {
                    if (c == '<')
                    {
                        isEmpty = false;
                    }
                }

                string[] newLines;
                if (isEmpty)
                {
                    newLines = new string[lines.Length - 1];

                    for (int i = 0; i < newLines.Length; i++)
                    {
                        newLines[i] = lines[i + 1];
                    }

                    lines = newLines;

                    continue;
                }

                newLines = new string[lines.Length - 1];
                if (lines[0].Length == 0 || lines[1].Length == 0)
                {
                    newLines[0] = lines[0] + lines[1];
                }
                else if (lines[0][lines[0].Length - 1] == '>' || lines[1][0] == '<')
                {
                    newLines[0] = lines[0] + lines[1];
                }
                else
                {
                    newLines[0] = lines[0] + ' ' + lines[1];
                }
                for (int i = 1; i < newLines.Length; i++)
                {
                    newLines[i] = lines[i + 1];
                }
                lines = newLines;
            }


            bool done = false;
            while (!done)
            {
                done = true;
                for (int i = 0; i < lines[0].Length; i++)
                {
                    if (lines[0][i] == '<' && lines[0][i + 1] == ' ')
                    {
                        string newLine = "";
                        for (int j = 0; j <= i; j++)
                        {
                            newLine += lines[0][j];
                        }
                        for (int j = i + 2; j < lines[0].Length; j++)
                        {
                            newLine += lines[0][j];
                        }
                        lines[0] = newLine;
                        done = false;
                    }
                    else if (lines[0][i] == '>' && lines[0][i - 1] == ' ')
                    {
                        string newLine = "";
                        for (int j = 0; j < i - 1; j++)
                        {
                            newLine += lines[0][j];
                        }
                        for (int j = i; j < lines[0].Length; j++)
                        {
                            newLine += lines[0][j];
                        }
                        lines[0] = newLine;
                        done = false;
                    }
                }
            }


            return lines[0];
        }

        private static void textTag(ref string html)
        {
            for (int i = 1; i < html.Length - 1; i++)
            {
                if (html[i] == '>' && html[i + 1] != '<')
                {
                    for (int j = i; j < html.Length; j++)
                    {
                        if (html[j] == '<')
                        {
                            html = html.Insert(j, "\">");
                            break;
                        }
                    }
                    html = html.Insert(i + 1, "<_text text=\"");
                }
            }
        }

        private static string htmlToJson(string html)
        {
            if (html.Length == 0)
            {
                return "[]";
            }

            string json = "[";

            int i = 0;
            while (i != html.Length)
            {
                string tag = getTag(html, i);
                if (tag == "!DOCTYPE")
                {
                    json += "{\"name\":\"DOCTYPE_html\",\"single\":true},";
                    i = "<!DOCTYPE html>".Length;
                    continue;
                }

                string parametrs = getParametrs(html, i);
                if (singleTags.Contains(tag))
                {
                    i = findFirstCloseSymbol(html, i) + 1;

                    json += $"{"{"}\"name\":\"{tag}\",\"parametrs\":{parametrs},\"single\":true{"}"},";
                }
                else
                {
                    int close = findTagClose(html, i);
                    if (close == -1)
                    {
                        throw new Exception("No tag close");
                    }
                    int start = findFirstCloseSymbol(html, i) + 1;
                    if (start == -1)
                    {
                        throw new Exception("No close symbol (>)");
                    }

                    string range = "";
                    for (int j = start; j < close; j++)
                    {
                        range += html[j];
                    }

                    string data = htmlToJson(range);

                    i = findFirstCloseSymbol(html, close) + 1;

                    json += $"{"{"}\"name\":\"{tag}\",\"parametrs\":{parametrs},\"single\":false,\"data\":{data}{"}"},";
                }
            }

            string newJson = "";
            for (int j = 0; j < json.Length - 1; j++)
            {
                newJson += json[j];
            }
            newJson += "]";

            return newJson;
        }

        private static string getTag(string html, int i)
        {
            if (html[i] == '<')
            {
                i++;
            }

            int firstSymbol;
            for (firstSymbol = i; firstSymbol <= html.Length; firstSymbol++)
            {
                if (firstSymbol == html.Length)
                {
                    throw new Exception("No close symbol (>)");
                }
                if (html[firstSymbol] == '<')
                {
                    throw new Exception("No close symbol (>)");
                }
                if (html[firstSymbol] == ' ' || html[firstSymbol] == '>')
                {
                    break;
                }
            }

            string tag = "";
            for (int j = i; j < firstSymbol; j++)
            {
                tag += html[j];
            }

            return tag;
        }

        private static int findTagClose(string html, int i)
        {
            if (html[i] == '<')
            {
                i++;
            }
            string tag = getTag(html, i);

            int[] openTags = Array.Empty<int>();
            int[] closeTags = Array.Empty<int>();

            for (int j = i; j < html.Length; j++)
            {
                if (html[j] == '<')
                {
                    bool openTag = true;
                    bool closeTag = false;
                    for (int k = 0; k < tag.Length; k++)
                    {
                        try
                        {
                            if (tag[k] != html[k + j + 1])
                            {
                                openTag = false;
                            }
                        }
                        catch (IndexOutOfRangeException)
                        {
                            openTag = false;
                        }
                    }
                    try
                    {
                        if (!(html[tag.Length + j + 1] == ' ' || html[tag.Length + j + 1] == '>'))
                        {
                            openTag = false;
                        }
                    }
                    catch (IndexOutOfRangeException)
                    {
                        openTag = false;
                    }

                    if (!openTag && html[j + 1] == '/')
                    {
                        closeTag = true;
                        for (int k = 0; k < tag.Length; k++)
                        {
                            try
                            {
                                if (tag[k] != html[k + j + 2])
                                {
                                    closeTag = false;
                                }
                            }
                            catch (IndexOutOfRangeException)
                            {
                                closeTag = false;
                            }
                        }
                        try
                        {
                            if (!(html[tag.Length + j + 2] == ' ' || html[tag.Length + j + 2] == '>'))
                            {
                                closeTag = false;
                            }
                        }
                        catch (IndexOutOfRangeException)
                        {
                            closeTag = false;
                        }
                    }

                    if (openTag)
                    {
                        int[] newOpen = new int[openTags.Length + 1];
                        for (int k = 0; k < openTags.Length; k++)
                        {
                            newOpen[k] = openTags[k];
                        }
                        newOpen[newOpen.Length - 1] = j;
                        openTags = newOpen;
                    }
                    else if (closeTag)
                    {
                        int[] newClose = new int[closeTags.Length + 1];
                        for (int k = 0; k < closeTags.Length; k++)
                        {
                            newClose[k] = closeTags[k];
                        }
                        newClose[newClose.Length - 1] = j;
                        closeTags = newClose;


                        if (closeTags.Length == openTags.Length + 1)
                        {
                            return closeTags[closeTags.Length - 1];
                        }
                    }
                }
            }

            return -1;
        }

        private static int findFirstCloseSymbol(string html, int i)
        {
            int symbol = -1;
            for (int j = i; j < html.Length; j++)
            {
                if (html[j] == '>')
                {
                    symbol = j;
                    break;
                }
            }
            return symbol;
        }

        private static string getParametrs(string html, int i)
        {
            if (html[i] == '<')
            {
                i++;
            }

            string parametrs = "{";
            string tag = getTag(html, i);
            int tagClose = findFirstCloseSymbol(html, i);

            if (html[i + tag.Length] == '>')
            {
                return "null";
            }

            int firstSymbol;
            while (i < tagClose)
            {
                for (firstSymbol = i; firstSymbol <= tagClose; firstSymbol++)
                {
                    if (html[firstSymbol] != ' ')
                    {
                        break;
                    }
                }
                if (firstSymbol == tagClose)
                {
                    break;
                }


                int endSymbol;
                bool isNoValue = false;
                for (endSymbol = firstSymbol; endSymbol <= tagClose; endSymbol++)
                {
                    if (html[endSymbol] == '=')
                    {
                        break;
                    }
                    if (html[endSymbol] == ' ' || html[endSymbol] == '>')
                    {
                        isNoValue = true;
                        break;
                    }
                }

                string key = "";
                for (int j = firstSymbol; j < endSymbol; j++)
                {
                    key += html[j];
                }

                if (isNoValue)
                {
                    parametrs += $"\"{key}\":\"<noValue>\",";
                    i = endSymbol + 1;
                    continue;
                }

                firstSymbol = endSymbol + 1;
                for (endSymbol = firstSymbol + 1; endSymbol <= tagClose; endSymbol++)
                {
                    if (html[endSymbol] == '\"')
                    {
                        break;
                    }
                }
                if (endSymbol == tagClose)
                {
                    throw new Exception("No parametr value close symbol (\")");
                }
                endSymbol++;

                string value = "";
                for (int j = firstSymbol; j < endSymbol; j++)
                {
                    value += html[j];
                }

                parametrs += $"\"{key}\":{value},";

                i = endSymbol + 1;
            }

            string newPars = "";
            for (int k = 0; k < parametrs.Length - 1; k++)
            {
                newPars += parametrs[k];
            }
            newPars += "}";

            return newPars;
        }




        public static string Serialize(Tag[] value)
        {
            return Serialize(value, 0);
        }

        private static string Serialize(Tag[] value, int tubNumber)
        {
            string html = "";
            string tub = "";
            for(int i = 0; i < tubNumber; i++)
            {
                tub += "\t";
            }

            foreach (Tag tag in value)
            {
                string name = tag.name;
                Tag.Parametrs? pars = tag.parametrs;
                string parametrs = "";
                string data = "";
                if (name == "DOCTYPE_html")
                {
                    html += $"{tub}<!DOCTYPE html>\n";
                    continue;
                }
                if (name == "_text")
                {
                    if (pars == null)
                    {
                        throw new Exception("No text parametr in _text tag");
                    }
                    html += tub + pars.text + '\n';
                    continue;
                }

                if (pars != null)
                {
                    if (pars.text != null)
                    {
                        parametrs += $" text=\"{pars.text}\"";
                    }
                    if (pars.herv != null)
                    {
                        parametrs += $" herv=\"{pars.herv}\"";
                    }
                    if (pars.id != null)
                    {
                        parametrs += $" id=\"{pars.id}\"";
                    }
                    if (pars._class != null)
                    {
                        parametrs += $" class=\"{pars._class}\"";
                    }
                    if (pars.charset != null)
                    {
                        parametrs += $" charset=\"{pars.charset}\"";
                    }
                    if (pars.align != null)
                    {
                        parametrs += $" align=\"{pars.align}\"";
                    }
                    if (pars.title != null)
                    {
                        parametrs += $" title=\"{pars.title}\"";
                    }
                    if (pars.dir != null)
                    {
                        parametrs += $" dir=\"{pars.dir}\"";
                    }
                    if (pars.lang != null)
                    {
                        parametrs += $" lang=\"{pars.lang}\"";
                    }
                    if (pars.valign != null)
                    {
                        parametrs += $" valign=\"{pars.valign}\"";
                    }
                    if (pars.bgcolor != null)
                    {
                        parametrs += $" bgcolor=\"{pars.bgcolor}\"";
                    }
                    if (pars.background != null)
                    {
                        parametrs += $" background=\"{pars.background}\"";
                    }
                    if (pars.width != null)
                    {
                        parametrs += $" width=\"{pars.width}\"";
                    }
                    if (pars.height != null)
                    {
                        parametrs += $" height=\"{pars.height}\"";
                    }
                }

                if (tag.single)
                {
                    html += $"{tub}<{name}{parametrs}>\n";
                }
                else
                {
                    if (tag.data != null)
                    {
                        data = Serialize(tag.data, tubNumber + 1);
                    }

                    html += $"{tub}<{name}{parametrs}>\n{data}{tub}</{name}>\n";
                }
            }



            return html;
        }


        private static int Sum(int a, int b)
        {
            return a + b;
        }
    }

#pragma warning disable CS8618
    public class Tag
    {
        public string name { get; set; }
        public bool single { get; set; }
        public Tag[]? data { get; set; }
        public Parametrs? parametrs { get; set; }

        public class Parametrs
        {
            public string? text { get; set; }
            public string? herv { get; set; }
            public string? id { get; set; }
            public string? _class { get; set; }
            public string? charset { get; set; }
            public string? align { get; set; }
            public string? title { get; set; }
            public string? dir { get; set; }
            public string? lang { get; set; }
            public string? valign { get; set; }
            public string? bgcolor { get; set; }
            public string? background { get; set; }
            public string? width { get; set; }
            public string? height { get; set; }

        }
    }
#pragma warning restore CS8618
}
