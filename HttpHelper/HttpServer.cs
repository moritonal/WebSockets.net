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
        public static string ServeHttpPage(string file, string mimeType)
        {
            StringBuilder str = new StringBuilder();

            string fileContents = null;

            if (File.Exists(file))
                fileContents = File.ReadAllText(file);

            str.AppendLine("HTTP/1.1 200 OK");
            str.AppendLine("Content-Type: " + mimeType + "; charset=UTF-8");
            str.AppendLine("Connection: close");
            str.AppendLine("Content-Type: " + mimeType);

            if (fileContents != null)
                str.AppendLine("Content-Length:" + fileContents.Length);

            str.AppendLine("");

            if (fileContents != null)
                str.Append(fileContents);

            return str.ToString();
        }
    }
}
