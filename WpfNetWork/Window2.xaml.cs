using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace WpfNetWork
{
    /// <summary>
    /// Window2.xaml 的交互逻辑
    /// </summary>
    public partial class Window2 : Window
    {
        public Window2()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            //var testLst = Enumerable
            //    .Range(0, 10000)
            //    .Select(p => new TestCls()
            //    {
            //        IntProp = p,
            //        DecimalProp = p * 1.0101M,
            //        BoolProp = p % 2 == 0,
            //        DateTimeProp = DateTime.Now.AddSeconds(-p),
            //        StrProp1 = $"str1{p}",
            //        StrProp2 = $"str2{p}",
            //        StrProp3 = $"str3{p}str3",
            //        StrProp4 = $"{p}str4"
            //    }).ToList();

            //var str = JsonSerializer.Serialize(testLst, TestContext.Default.ListTestCls);
        }
    }
}
