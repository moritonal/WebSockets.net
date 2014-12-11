using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HttpHelper
{
    public class HttpServer
    {
        public static string ServeHttpPage(string file)
        {
            StringBuilder str = new StringBuilder();

            str.AppendLine("HTTP/1.1 200 OK");
            str.AppendLine("Content-Type: text/html; charset=UTF-8");
            str.AppendLine("Connection: close");
            str.AppendLine("");
            if (File.Exists(file))
                str.Append(File.ReadAllText(file));

            return str.ToString();
        }
    }
}
