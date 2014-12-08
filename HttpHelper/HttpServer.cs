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
            if (File.Exists(file))
                return File.ReadAllText(file);
            return null;
        }
    }
}
