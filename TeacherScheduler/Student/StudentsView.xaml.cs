using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml;

namespace TeacherScheduler
{
    /// <summary>
    /// Interaction logic for StudentSchduleView.xaml
    /// </summary>
    public partial class StudentsView : UserControl
    {        
        public StudentsView()
        {
            InitializeComponent();          
        }

        public void test(object sender, RoutedEventArgs e)
        {
            ((StudentsViewModel)DataContext).SelectedStudent = ((StudentsViewModel)DataContext).Students[1];
        }
    }
}
