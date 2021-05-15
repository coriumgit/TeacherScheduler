using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TeacherScheduler
{
    public class Teacher {
        public ObservableMatrix<bool> Schedule { get; }

        public Teacher()
        {
            Schedule = new ObservableMatrix<bool>(App.HOURS_NR, App.DAYS_LABELS.Length);
            Schedule.MatrixChanged += (int hourIdx, int dayIdx, bool isAvailable) => DataBaseManager.Instance.updateTeacherSchedule(hourIdx, dayIdx, isAvailable);
        }       
    }
}
