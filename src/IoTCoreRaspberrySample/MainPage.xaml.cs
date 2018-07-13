using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Devices.Gpio;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.System.Threading;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace IoTCoreRaspberrySample
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        TwitterManager _twitter = new TwitterManager();
        private ThreadPoolTimer _timerTwitter;
        private CoreDispatcher _dispacher;
        private GpioController _gpioController;
        private GpioPin _pinSensor;
        private GpioPin _pinLed;

        public MainPage()
        {
            this.InitializeComponent();
            this.Loaded += MainPage_Loaded;
            _dispacher = this.Dispatcher;
        }

        private async void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            await InitTwitter();
            await InitGpio();
        }

        private async Task InitGpio()
        {
            _gpioController = await GpioController.GetDefaultAsync();
            if (_gpioController == null)
                return;

            _pinLed = _gpioController?.OpenPin(26);
            _pinLed.SetDriveMode(GpioPinDriveMode.Output);

            _pinSensor = _gpioController?.OpenPin(18);
            _pinSensor.DebounceTimeout = TimeSpan.FromMilliseconds(200);
            _pinSensor.SetDriveMode(GpioPinDriveMode.InputPullDown);
            _pinSensor.ValueChanged += _pinSensor_ValueChanged;
        }

        private async Task InitTwitter()
        {
            if (await _twitter.Setup())
            {
                this.txbMessage1.Text = "Twitter conectado!";
                _timerTwitter = ThreadPoolTimer.CreatePeriodicTimer(Twitter_Tick, TimeSpan.FromMilliseconds(5000));
            }
            else
                this.txbMessage1.Text = "Twitter off!";
        }

        private async void _pinSensor_ValueChanged(GpioPin sender, GpioPinValueChangedEventArgs args)
        {
            var text = args.Edge == GpioPinEdge.RisingEdge ? "Tem Gente!" : "ninguém em casa";
            await _twitter.Publish(text);

            await _dispacher.RunAsync(CoreDispatcherPriority.Normal,
                () => this.txbMessage2.Text = text);
        }

        private async void Twitter_Tick(ThreadPoolTimer timer)
        {
            var lastTweet = await _twitter.GetTweet();
            if (lastTweet.user == null)
                return;

            var onOffstr = lastTweet.on ? "ligar" : "desligar";
            _pinLed?.Write(lastTweet.on ? GpioPinValue.High : GpioPinValue.Low);

            await _dispacher.RunAsync(CoreDispatcherPriority.Normal, () =>
                 {
                     this.txbMessage1.Text = $"{lastTweet.user} \nmandou {onOffstr}!";
                     this.imgProfile.Source = new BitmapImage(new Uri(lastTweet.image)); 
                 });
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
