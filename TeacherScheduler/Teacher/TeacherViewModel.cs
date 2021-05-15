using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Data;
using System.Xml;

namespace TeacherScheduler
{
    public class TeacherViewModel : ObservableObject, IPageViewModel
    {
        public string Name { get; } = "Teacher Parameters";

        public Teacher TheTeacher { get; private set; }

        public TeacherViewModel()
        {
            TheTeacher = DataBaseManager.Instance.loadTeacher();
        }
    }
}
