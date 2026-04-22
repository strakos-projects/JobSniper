using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobSniper.Plugins
{
    public interface IWebActionPlugin
    {
        string Name { get; }
        void Execute(string url, string jobText, string companyName, string jobTitle);
    }
}
