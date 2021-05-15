using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace TeacherScheduler
{
    public class Student : ObservableObject
    {                
        private string name = "New Tambal";
        private School school = null;
        private int requiredHoursNr = 0;        

        public string Name
        {
            get { return name; }

            set
            {
                if (name != value)
                {
                    DataBaseManager.Instance.updateStudentName(name, value);
                    name = value;

                    OnPropertyChanged("Name");
                }
            }
        }

        public School SchoolAttended
        {
            get { return school; }

            set
            {
                if (value != null && school != value)
                {
                    school = value;
                    DataBaseManager.Instance.updateStudentSchool(name, value);
                    OnPropertyChanged("SchoolAttended");
                }
            }
        }

        public int RequiredHoursNr
        {
            get { return requiredHoursNr; }

            set
            {
                if (requiredHoursNr != value)
                {
                    requiredHoursNr = value;
                    DataBaseManager.Instance.updateStudentRequiredHours(name, value);
                    OnPropertyChanged("RequiredHoursNr");
                }
            }
        }

        public ObservableMatrix<bool> Schedule { get; }
        
        public Student()
        {
            Schedule = new ObservableMatrix<bool>(App.HOURS_NR, App.DAYS_LABELS.Length);
            Schedule.MatrixChanged += (int hourIdx, int dayIdx, bool isAvailable) => DataBaseManager.Instance.updateStudentSchedule(name, hourIdx, dayIdx, isAvailable);
        }

        public Student(string name, School school, int requiredHoursNr) : this()
        {            
            this.name = name;
            this.school = school;
            this.requiredHoursNr = requiredHoursNr;            
        }
    }
}
