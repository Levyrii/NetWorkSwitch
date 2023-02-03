using ManagedNativeWifi;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace WpfNetWork
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly NetList _netWorkList = new();
        private const int INTERNET_CONNECTION_MODEM = 1;
        private const int INTERNET_CONNECTION_LAN = 2;

        [DllImport("winInet.dll")]
        private static extern bool InternetGetConnectedState(ref int dwFlag, int dwReserved);

        public MainWindow()
        {
            InitializeComponent();

            SetNetWorkList();
            Loaded += MainWindow_Loaded;
            MouseMove += MainWindow_MouseMove;
        }

        #region 事件处理
        /// <summary>
        /// 窗口加载处理事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            int wlanStatus = GetNetConStatus("baidu.com");
            if (wlanStatus == 3)
            {
                btnSwitch.Content = "切换到内网";
                await DisableNWI(_netWorkList.EthernetNWIName);
            }
            else
            {
                //如果启动时外网不通一律认为当前时内外（即使不太准确）
                //int lanStatus = GetNetConStatus("10.20.13.100");
                btnSwitch.Content = "切换到外网";
                await DisableNWI(_netWorkList.WirelessNWIName);
            }
        }

        /// <summary>
        /// 网络切换按钮处理事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void BtnSwitch_Click(object sender, RoutedEventArgs e)
        {
            string? btnContent = btnSwitch.Content.ToString();
            if (btnContent != "切换到内网" && btnContent != "切换到外网") return;

            ResetNWIName();
            if (btnSwitch.Content.ToString() == "切换到内网")
            {
                btnSwitch.Content = "切换中...";
                await DisableNWI(_netWorkList.WirelessNWIName);
                await EnableNWI(_netWorkList.EthernetNWIName);
                btnSwitch.Content = "切换到外网";
                return;
            }

            btnSwitch.Content = "切换中...";
            await DisableNWI(_netWorkList.EthernetNWIName);
            await EnableNWI(_netWorkList.WirelessNWIName);
            btnSwitch.Content = "Wifi连接中...";
            await Task.Delay(600);
            bool ret = await ConnectWifiAsync(txtWifi.Text);
            btnSwitch.Content = "切换到内网";
        }

        private void MainWindow_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                var pt = e.GetPosition(this);
                if (pt.X > 0 && pt.X < 200 && pt.Y > 0 && pt.Y < 25)
                {
                    this.DragMove();
                }
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void BtnMin_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }
        #endregion

        #region 方法
        /// <summary>
        /// 设置初始网络集合
        /// </summary>
        private void SetNetWorkList()
        {
            var validNWILst = GetValidNWI();
            var wirelessNWI = validNWILst.FirstOrDefault(
                p => p.NetworkInterfaceType == NetworkInterfaceType.Wireless80211);
            _netWorkList.Add(new NetCls(wirelessNWI == null ? "WLAN" : wirelessNWI.Name, 0));

            var ethernetNWI = validNWILst.FirstOrDefault(
                p => p.NetworkInterfaceType == NetworkInterfaceType.Ethernet);
            string ethernetName = ethernetNWI == null ?
                (string.IsNullOrEmpty(txtEnther.Text) ? "以太网" : txtEnther.Text) : ethernetNWI.Name;

            _netWorkList.Add(new NetCls(ethernetName, 1));
        }

        /// <summary>
        /// 重新设置当前最新的NWI名称
        /// </summary>
        private void ResetNWIName()
        {
            var ethernetNWI = _netWorkList.EthernetNWI;
            if (ethernetNWI != null && !string.IsNullOrEmpty(txtEnther.Text))
            {
                ethernetNWI.Name = txtEnther.Text;
            }
        }

        /// <summary>
        /// 获取有效的网络接口
        /// </summary>
        /// <returns></returns>
        private List<NetworkInterface> GetValidNWI()
        {
            NetworkInterface[] interfaces = NetworkInterface.GetAllNetworkInterfaces();
            var ret = interfaces.Where(p => p.NetworkInterfaceType != NetworkInterfaceType.Loopback &&
            !p.Description.Contains("Virtual") && !p.Description.Contains("VPN"));
            return ret.ToList();
        }
        #endregion

        /// <summary>
        /// 启用网络Interface
        /// </summary>
        /// <param name="interfaceName"></param>
        private Task EnableNWI(string? interfaceName)
        {
            if (string.IsNullOrEmpty(interfaceName)) return Task.CompletedTask;
            ProcessStartInfo psi = new("netsh", $"interface set interface \"{interfaceName}\" admin=enable")
            {
                CreateNoWindow = true
            };
            Process p = new()
            {
                StartInfo = psi
            };
            p.Start();
            return p.WaitForExitAsync();
        }

        /// <summary>
        /// 禁用网络Interface
        /// </summary>
        /// <param name="interfaceName"></param>
        private Task DisableNWI(string? interfaceName)
        {
            if (string.IsNullOrEmpty(interfaceName)) return Task.CompletedTask;
            ProcessStartInfo psi = new("netsh", $"interface set interface \"{interfaceName}\" admin=disable")
            {
                CreateNoWindow = true
            };
            Process p = new()
            {
                StartInfo = psi
            };
            p.Start();
            return p.WaitForExitAsync();
        }

        /// <summary>
        /// 获取网络连接状态
        /// </summary>
        /// <param name="strNetAddress"></param>
        /// <returns></returns>
        private int GetNetConStatus(string strNetAddress)
        {
            int iNetStatus = 0;
            int dwFlag = new();
            if (!InternetGetConnectedState(ref dwFlag, 0))
            {
                //没有能连上互联网
                iNetStatus = 1;
            }
            else if ((dwFlag & INTERNET_CONNECTION_MODEM) != 0)
            {
                //采用调治解调器上网,需要进一步判断能否登录具体网站
                if (PingNetAddress(strNetAddress))
                {
                    //可以ping通给定的网址,网络OK
                    iNetStatus = 2;
                }
                else
                {
                    //不可以ping通给定的网址,网络不OK
                    iNetStatus = 4;
                }
            }
            else if ((dwFlag & INTERNET_CONNECTION_LAN) != 0)
            {
                //采用网卡上网,需要进一步判断能否登录具体网站
                if (PingNetAddress(strNetAddress))
                {
                    //可以ping通给定的网址,网络OK
                    iNetStatus = 3;
                }
                else
                {
                    //不可以ping通给定的网址,网络不OK
                    iNetStatus = 5;
                }
            }
            return iNetStatus;
        }

        /// <summary>
        /// ping 具体的网址看能否ping通
        /// </summary>
        /// <param name="strNetAdd"></param>
        /// <returns></returns>
        private bool PingNetAddress(string strNetAdd)
        {
            try
            {
                Ping ping = new();
                PingReply pr = ping.Send(strNetAdd, 3000);
                return pr.Status == IPStatus.Success;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// wifi连接
        /// </summary>
        /// <param name="sid"></param>
        /// <returns></returns>
        public async Task<bool> ConnectWifiAsync(string sid)
        {
            //NativeWifi.ThrowsOnAnyFailure = true;
            var availableNetwork = await GetAvailableNetworkPacks(sid);
            if (availableNetwork is null) return false;

            return await NativeWifi.ConnectNetworkAsync(
                interfaceId: availableNetwork.Interface.Id,
                profileName: availableNetwork.ProfileName,
                bssType: availableNetwork.BssType,
                timeout: TimeSpan.FromSeconds(10));
        }

        private async Task<AvailableNetworkPack?> GetAvailableNetworkPacks(
            string sid, int curEnlistTimes = 0)
        {
            if (curEnlistTimes >= 10) return null;

            var ret = NativeWifi.EnumerateAvailableNetworks().ToList();
            var availableNetwork = ret
                .Where(x => x.ProfileName == sid)
                .OrderByDescending(x => x.SignalQuality)
                .FirstOrDefault();
            if (availableNetwork != null) return availableNetwork;

            await Task.Delay(200);
            return await GetAvailableNetworkPacks(sid, ++curEnlistTimes);
        }
    }

    public class NetCls
    {
        public string Name { get; set; }

        /// <summary>
        /// 0:无线 1:有线
        /// </summary>
        public int Type { get; set; }

        public NetCls(string name, int type)
        {
            Name = name;
            Type = type;
        }
    }

    public class NetList : List<NetCls>
    {
        public NetCls? WirelessNWI => this.FirstOrDefault(p => p.Type == 0);
        public NetCls? EthernetNWI => this.FirstOrDefault(p => p.Type == 1);

        public string? WirelessNWIName => WirelessNWI?.Name;
        public string? EthernetNWIName => EthernetNWI?.Name;
    }
}