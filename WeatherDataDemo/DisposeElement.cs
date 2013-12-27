using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AlteryxGalleryAPIWrapper;

namespace WeatherDataDemo
{
    public class DisposeElement : IDisposable
    {

        Client objClient = new Client("https://devgallery.alteryx.com/api/");
        // private MortageCalculatorCheckSteps ok = new MortageCalculatorCheckSteps();


        public void Dispose()
        {
            objClient.Dispose();

        }

        public void Dispose(string _appid)
        {
            objClient.DeleteApp(_appid);
            objClient.Dispose();

        }
    }
}
