using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Collections.ObjectModel;

namespace TeacherScheduler
{
    public class StudentsViewModel : IPageViewModel
    {
        public string Name { get; } = "Students Parameters";

        public ObservableCollection<Student> Students { get; private set; }

        Student selectedStudent;
        public Student SelectedStudent { 
            get { return selectedStudent; }
            set { selectedStudent = value; }
        }             

        public IList<School> Schools { get; private set; }        

        public ICommand AddStudentCommand { get; }
        public ICommand RemoveSelectedStudentCommand { get; }             

        //public StudentsView StudentsViewBuffer { get; set; }

        public StudentsViewModel(IList<School> schools)
        {
            Schools = schools;

            AddStudentCommand = new RelayCommand(param => addStudent());
            RemoveSelectedStudentCommand = new RelayCommand(param => removeSelectedStudent((Student)param));
            Students = new ObservableCollection<Student>(DataBaseManager.Instance.loadStudents());
            //StudentsViewBuffer = new StudentsView();
            //StudentsViewBuffer.DataContext = this;
        }

        private void addStudent()
        {
            Student student = new Student();
            Students.Add(student);
            DataBaseManager.Instance.addStudent(student);
        }

        private void removeSelectedStudent(Student student)
        {
            if (student != null)
            {
                Students.Remove(student);
                DataBaseManager.Instance.removeStudent(student.Name);
            }
        }       
    }
}
